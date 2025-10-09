using NetherGate.API.Logging;
using Python.Runtime;

namespace NetherGate.Python.Interop;

/// <summary>
/// 服务桥接类
/// 将 C# 服务导出到 Python 环境
/// </summary>
public static class ServiceBridge
{
    /// <summary>
    /// 导出所有服务到 Python 模块
    /// </summary>
    public static void ExportServices(IServiceProvider serviceProvider, ILogger logger)
    {
        logger.Info("导出 C# 服务到 Python 环境...");

        using (Py.GIL())
        {
            try
            {
                // 创建 nethergate 模块（如果不存在）
                dynamic sys = Py.Import("sys");
                dynamic modules = sys.modules;

                // TODO: 实现服务导出逻辑
                // 这里需要将 C# 服务包装为 Python 可调用的对象

                logger.Debug("C# 服务导出完成");
            }
            catch (PythonException ex)
            {
                logger.Error($"导出服务失败: {ex.Message}", ex);
                throw;
            }
        }
    }

    /// <summary>
    /// 包装 C# 对象为 Python 可调用对象
    /// </summary>
    public static PyObject WrapService(object service)
    {
        return service.ToPython();
    }
}

