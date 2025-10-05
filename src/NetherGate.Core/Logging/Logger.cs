using NetherGate.API.Logging;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace NetherGate.Core.Logging;

/// <summary>
/// 日志实现
/// </summary>
public class Logger : ILogger
{
    private readonly string _name;
    private readonly LogLevel _minLevel;
    private readonly List<ILogWriter> _writers;

    public Logger(string name, LogLevel minLevel, List<ILogWriter> writers)
    {
        _name = name;
        _minLevel = minLevel;
        _writers = writers;
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);
    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warning(string message) => Log(LogLevel.Warning, message);
    public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    public void Fatal(string message, Exception? exception = null) => Log(LogLevel.Fatal, message, exception);

    public bool IsEnabled(LogLevel level) => level >= _minLevel;

    private void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (!IsEnabled(level))
            return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            LoggerName = _name,
            Message = message,
            Exception = exception
        };

        foreach (var writer in _writers)
        {
            writer.Write(logEntry);
        }
    }
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string LoggerName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
}

/// <summary>
/// 日志写入器接口
/// </summary>
public interface ILogWriter
{
    void Write(LogEntry entry);
}

/// <summary>
/// 控制台日志写入器
/// </summary>
public class ConsoleLogWriter : ILogWriter
{
    private readonly bool _useColors;

    public ConsoleLogWriter(bool useColors = true)
    {
        _useColors = useColors;
    }

    public void Write(LogEntry entry)
    {
        var levelText = GetLevelText(entry.Level);
        var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

        if (_useColors)
        {
            // 只对日志级别部分上色
            Console.Write($"[{timestamp}] [");
            Console.ForegroundColor = GetLevelColor(entry.Level);
            Console.Write(levelText);
            Console.ResetColor();
            Console.WriteLine($"] [{entry.LoggerName}] {entry.Message}");
        }
        else
        {
            Console.WriteLine($"[{timestamp}] [{levelText}] [{entry.LoggerName}] {entry.Message}");
        }

        if (entry.Exception != null)
        {
            if (_useColors)
                Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"    {entry.Exception}");
            if (_useColors)
                Console.ResetColor();
        }
    }

    private static string GetLevelText(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Info => "INFO ",
        LogLevel.Warning => "WARN ",
        LogLevel.Error => "ERROR",
        LogLevel.Fatal => "FATAL",
        _ => "UNKNW"
    };

    private static ConsoleColor GetLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.Cyan,
        LogLevel.Info => ConsoleColor.Green,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Fatal => ConsoleColor.DarkRed,
        _ => ConsoleColor.White
    };
}

/// <summary>
/// 文件日志写入器
/// </summary>
public class FileLogWriter : ILogWriter, IDisposable
{
    private readonly string _filePath;
    private readonly int _maxSize;
    private readonly int _maxFiles;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private long _currentSize;

    public FileLogWriter(string filePath, int maxSize, int maxFiles)
    {
        _filePath = filePath;
        _maxSize = maxSize;
        _maxFiles = maxFiles;

        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // 归档旧日志
        ArchiveOldLog();

        OpenWriter();
    }

    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            if (_writer == null)
                return;

            var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = entry.Level.ToString().ToUpper().PadRight(7);
            var message = $"[{timestamp}] [{level}] [{entry.LoggerName}] {entry.Message}";

            _writer.WriteLine(message);
            if (entry.Exception != null)
            {
                _writer.WriteLine($"    {entry.Exception}");
            }

            _writer.Flush();
            _currentSize += message.Length + Environment.NewLine.Length;

            // 检查是否需要滚动日志
            if (_currentSize >= _maxSize)
            {
                RollLog();
            }
        }
    }

    private void ArchiveOldLog()
    {
        // 如果日志文件不存在或为空，则无需归档
        if (!File.Exists(_filePath))
            return;

        var fileInfo = new FileInfo(_filePath);
        if (fileInfo.Length == 0)
        {
            File.Delete(_filePath);
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_filePath) ?? ".";
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            
            // 查找今天已经存在的归档数量
            var existingArchives = Directory.GetFiles(directory, $"{today}-*.log.gz");
            var nextNumber = existingArchives.Length + 1;
            
            // 生成归档文件名: yyyy-MM-dd-N.log.gz
            var archiveFileName = $"{today}-{nextNumber}.log.gz";
            var archivePath = Path.Combine(directory, archiveFileName);
            
            // 压缩日志文件
            using (var sourceStream = File.OpenRead(_filePath))
            using (var targetStream = File.Create(archivePath))
            using (var compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
            {
                sourceStream.CopyTo(compressionStream);
            }
            
            // 删除原日志文件
            File.Delete(_filePath);
        }
        catch (Exception)
        {
            // 如果归档失败，继续使用现有日志文件
            // 不抛出异常，避免影响程序启动
        }
    }

    private void OpenWriter()
    {
        _writer = new StreamWriter(_filePath, true, System.Text.Encoding.UTF8);
        _currentSize = new FileInfo(_filePath).Exists ? new FileInfo(_filePath).Length : 0;
    }

    private void RollLog()
    {
        _writer?.Dispose();
        _writer = null;

        // 删除最旧的日志文件
        var oldestFile = $"{_filePath}.{_maxFiles - 1}";
        if (File.Exists(oldestFile))
            File.Delete(oldestFile);

        // 重命名现有日志文件
        for (int i = _maxFiles - 2; i >= 0; i--)
        {
            var sourceFile = i == 0 ? _filePath : $"{_filePath}.{i}";
            var targetFile = $"{_filePath}.{i + 1}";

            if (File.Exists(sourceFile))
                File.Move(sourceFile, targetFile);
        }

        OpenWriter();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}

/// <summary>
/// 日志工厂实现
/// </summary>
public class LoggerFactory : ILoggerFactory
{
    private readonly LogLevel _minLevel;
    private readonly List<ILogWriter> _writers;
    private readonly ConcurrentDictionary<string, ILogger> _loggers;

    public LoggerFactory(LogLevel minLevel, List<ILogWriter> writers)
    {
        _minLevel = minLevel;
        _writers = writers;
        _loggers = new ConcurrentDictionary<string, ILogger>();
    }

    public ILogger CreateLogger(string name)
    {
        return _loggers.GetOrAdd(name, n => new Logger(n, _minLevel, _writers));
    }

    public ILogger CreateLogger<T>()
    {
        return CreateLogger(typeof(T).Name);
    }
}

