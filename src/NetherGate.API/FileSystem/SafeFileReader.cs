using System.Text;
using System.Text.Json;

namespace NetherGate.API.FileSystem;

/// <summary>
/// 安全文件读取器 - 优化的文件读取，避免锁定文件
/// 参考自 AATool 的最佳实践
/// </summary>
public static class SafeFileReader
{
    /// <summary>
    /// 安全地读取文件内容（文本）
    /// 使用只读权限和共享读写删除，避免锁定文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="encoding">编码（默认 UTF-8）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容</returns>
    public static async Task<string> ReadTextAsync(
        string path, 
        Encoding? encoding = null, 
        CancellationToken cancellationToken = default)
    {
        encoding ??= Encoding.UTF8;

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, // 允许其他进程读写和删除
            bufferSize: 4096,
            useAsync: true
        );

        using var reader = new StreamReader(stream, encoding);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// 安全地读取 JSON 文件并反序列化
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="path">文件路径</param>
    /// <param name="options">JSON 序列化选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>反序列化的对象</returns>
    public static async Task<T?> ReadJsonAsync<T>(
        string path,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true
        );

        return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
    }

    /// <summary>
    /// 安全地读取二进制文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件字节数组</returns>
    public static async Task<byte[]> ReadBytesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true
        );

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// 安全地读取文件的部分内容
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="offset">偏移量</param>
    /// <param name="count">读取字节数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取的字节数组</returns>
    public static async Task<byte[]> ReadPartialAsync(
        string path,
        long offset,
        int count,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true
        );

        stream.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[count];
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
        
        if (bytesRead < count)
        {
            Array.Resize(ref buffer, bytesRead);
        }

        return buffer;
    }

    /// <summary>
    /// 检查文件是否存在且可读
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>是否可读</returns>
    public static bool CanRead(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete
            );
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 等待文件可用（当文件被锁定时）
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="pollInterval">轮询间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件是否可用</returns>
    public static async Task<bool> WaitForFileAsync(
        string path,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        var endTime = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < endTime)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            if (CanRead(path))
                return true;

            await Task.Delay(pollInterval.Value, cancellationToken);
        }

        return false;
    }
}

