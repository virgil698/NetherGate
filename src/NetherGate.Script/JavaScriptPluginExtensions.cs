using Microsoft.Extensions.DependencyInjection;
using NetherGate.API.Logging;

namespace NetherGate.Script;

/// <summary>
/// JavaScript 插件扩展方法
/// </summary>
public static class JavaScriptPluginExtensions
{
    /// <summary>
    /// 添加 JavaScript 插件支持
    /// </summary>
    public static IServiceCollection AddJavaScriptPluginSupport(
        this IServiceCollection services,
        string? pluginsDirectory = null)
    {
        // 注册 JavaScriptRuntime 为单例
        services.AddSingleton<JavaScriptRuntime>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var runtime = new JavaScriptRuntime(logger);
            
            // 初始化引擎
            runtime.Initialize();
            
            return runtime;
        });

        // 注册 JavaScriptPluginLoader
        services.AddSingleton<JavaScriptPluginLoader>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            var runtime = sp.GetRequiredService<JavaScriptRuntime>();
            var pluginsDir = pluginsDirectory ?? Path.Combine(AppContext.BaseDirectory, "plugins");
            
            return new JavaScriptPluginLoader(logger, sp, runtime, pluginsDir);
        });

        return services;
    }
}

