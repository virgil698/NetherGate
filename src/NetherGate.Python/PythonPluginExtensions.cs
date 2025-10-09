using Microsoft.Extensions.DependencyInjection;
using NetherGate.API.Logging;

namespace NetherGate.Python;

/// <summary>
/// Python 插件扩展方法
/// </summary>
public static class PythonPluginExtensions
{
    /// <summary>
    /// 添加 Python 插件支持
    /// </summary>
    public static IServiceCollection AddPythonPluginSupport(this IServiceCollection services)
    {
        // 注册 Python 运行时
        services.AddSingleton<PythonRuntime>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var runtime = new PythonRuntime(logger);
            runtime.Initialize();
            return runtime;
        });

        // 注册 Python 插件加载器
        services.AddSingleton<PythonPluginLoader>();

        return services;
    }
}

