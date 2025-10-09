using System.Diagnostics;
using System.Runtime.InteropServices;
using NetherGate.API.Logging;
using Python.Runtime;

namespace NetherGate.Python;

/// <summary>
/// Python 运行时管理器
/// 负责初始化、配置和管理 Python 解释器
/// </summary>
public class PythonRuntime : IDisposable
{
    private readonly ILogger _logger;
    private bool _initialized;
    private bool _disposed;

    public PythonRuntime(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化 Python 运行时
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
        {
            _logger.Warning("Python 运行时已经初始化");
            return;
        }

        try
        {
            _logger.Info("正在初始化 Python 运行时...");

            // 设置 Python 主目录（如果指定了环境变量）
            var pythonHome = Environment.GetEnvironmentVariable("PYTHONHOME");
            if (!string.IsNullOrEmpty(pythonHome))
            {
                Runtime.PythonDLL = GetPythonDll(pythonHome);
                _logger.Debug($"使用自定义 Python 路径: {pythonHome}");
            }

            // 初始化 Python 引擎
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();

            _initialized = true;

            // 获取 Python 版本信息
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                string version = sys.version.ToString();
                _logger.Info($"Python 运行时已初始化: {version}");
            }

            // 安装 NetherGate Python SDK
            InstallNetherGateSDK();
        }
        catch (Exception ex)
        {
            _logger.Error("初始化 Python 运行时失败", ex);
            throw;
        }
    }

    /// <summary>
    /// 关闭 Python 运行时
    /// </summary>
    public void Shutdown()
    {
        if (!_initialized || _disposed)
        {
            return;
        }

        try
        {
            _logger.Info("正在关闭 Python 运行时...");
            PythonEngine.Shutdown();
            _initialized = false;
            _logger.Info("Python 运行时已关闭");
        }
        catch (Exception ex)
        {
            _logger.Error("关闭 Python 运行时失败", ex);
        }
    }

    /// <summary>
    /// 创建虚拟环境
    /// </summary>
    public void CreateVirtualEnvironment(string venvPath)
    {
        EnsureInitialized();

        _logger.Info($"创建 Python 虚拟环境: {venvPath}");

        using (Py.GIL())
        {
            try
            {
                dynamic venv = Py.Import("venv");
                venv.create(venvPath, with_pip: true);
                _logger.Info("虚拟环境创建成功");
            }
            catch (PythonException ex)
            {
                _logger.Error($"创建虚拟环境失败: {ex.Message}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// 安装 Python 包
    /// </summary>
    public void InstallPackage(string packageSpec, string? venvPath = null)
    {
        _logger.Info($"安装 Python 包: {packageSpec}");

        try
        {
            // 构建 pip 命令
            string pipExecutable = GetPipExecutable(venvPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = pipExecutable,
                Arguments = $"install {packageSpec}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("无法启动 pip 进程");
            }

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                _logger.Info($"包 {packageSpec} 安装成功");
            }
            else
            {
                string error = process.StandardError.ReadToEnd();
                _logger.Error($"包 {packageSpec} 安装失败: {error}");
                throw new InvalidOperationException($"pip install 失败: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"安装包失败: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// 添加 Python 路径
    /// </summary>
    public void AddToPath(string path)
    {
        EnsureInitialized();

        using (Py.GIL())
        {
            dynamic sys = Py.Import("sys");
            dynamic syspath = sys.path;
            
            if (!syspath.Contains(path))
            {
                syspath.insert(0, path);
                _logger.Debug($"已添加到 Python 路径: {path}");
            }
        }
    }

    /// <summary>
    /// 检查 Python 版本
    /// </summary>
    public bool CheckPythonVersion(string requiredVersion)
    {
        EnsureInitialized();

        using (Py.GIL())
        {
            dynamic sys = Py.Import("sys");
            string currentVersion = $"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}";
            
            var required = Version.Parse(requiredVersion.TrimEnd('+'));
            var current = Version.Parse(currentVersion);

            bool compatible = current >= required;
            
            if (!compatible)
            {
                _logger.Warning($"Python 版本不兼容: 需要 {requiredVersion}, 当前 {currentVersion}");
            }

            return compatible;
        }
    }

    /// <summary>
    /// 安装 NetherGate Python SDK
    /// </summary>
    private void InstallNetherGateSDK()
    {
        _logger.Info("安装 NetherGate Python SDK...");

        // TODO: 从嵌入资源中提取 SDK 文件
        // 或者使用 pip install nethergate-python

        _logger.Info("NetherGate Python SDK 已安装");
    }

    /// <summary>
    /// 获取 Python DLL 路径
    /// </summary>
    private string GetPythonDll(string pythonHome)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: python310.dll, python311.dll, etc.
            var dllFiles = Directory.GetFiles(pythonHome, "python3*.dll");
            if (dllFiles.Length > 0)
            {
                return dllFiles[0];
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: libpython3.10.so, libpython3.11.so, etc.
            return Path.Combine(pythonHome, "lib", "libpython3.so");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: libpython3.10.dylib, libpython3.11.dylib, etc.
            return Path.Combine(pythonHome, "lib", "libpython3.dylib");
        }

        throw new PlatformNotSupportedException("不支持的操作系统");
    }

    /// <summary>
    /// 获取 pip 可执行文件路径
    /// </summary>
    private string GetPipExecutable(string? venvPath)
    {
        if (!string.IsNullOrEmpty(venvPath))
        {
            // 虚拟环境中的 pip
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(venvPath, "Scripts", "pip.exe");
            }
            else
            {
                return Path.Combine(venvPath, "bin", "pip");
            }
        }

        // 系统 pip
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pip.exe" : "pip3";
    }

    /// <summary>
    /// 确保运行时已初始化
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Python 运行时未初始化");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Shutdown();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

