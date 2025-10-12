using NetherGate.API.Logging;
using NetherGate.API.Plugins;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 服务器处理器注册表实现
/// </summary>
public class ServerHandlerRegistry : IServerHandlerRegistry
{
    private readonly ILogger _logger;
    private readonly List<RegisteredHandler<IServerOutputHandler>> _outputHandlers = new();
    private readonly List<RegisteredHandler<IServerMessageProcessor>> _messageProcessors = new();
    private readonly object _lock = new();

    public ServerHandlerRegistry(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void RegisterOutputHandler(IServerOutputHandler handler, string pluginId)
    {
        lock (_lock)
        {
            // 检查是否已存在
            if (_outputHandlers.Any(h => h.Handler.Name == handler.Name && h.PluginId == pluginId))
            {
                _logger.Warning($"输出处理器 '{handler.Name}' 已被插件 '{pluginId}' 注册，将被覆盖");
                UnregisterOutputHandler(handler.Name, pluginId);
            }

            var registered = new RegisteredHandler<IServerOutputHandler>
            {
                Handler = handler,
                PluginId = pluginId,
                RegisteredAt = DateTime.UtcNow,
                IsEnabled = true
            };

            _outputHandlers.Add(registered);

            // 按优先级排序（降序）
            _outputHandlers.Sort((a, b) => b.Handler.Priority.CompareTo(a.Handler.Priority));

            _logger.Info($"[{pluginId}] 注册输出处理器: {handler.Name} (优先级: {handler.Priority})");
        }
    }

    /// <inheritdoc/>
    public void UnregisterOutputHandler(string handlerName, string pluginId)
    {
        lock (_lock)
        {
            var removed = _outputHandlers.RemoveAll(h => h.Handler.Name == handlerName && h.PluginId == pluginId);
            if (removed > 0)
            {
                _logger.Info($"[{pluginId}] 注销输出处理器: {handlerName}");
            }
        }
    }

    /// <inheritdoc/>
    public async Task RegisterMessageProcessorAsync(IServerMessageProcessor processor, string pluginId)
    {
        lock (_lock)
        {
            // 检查是否已存在
            if (_messageProcessors.Any(p => p.Handler.Name == processor.Name && p.PluginId == pluginId))
            {
                _logger.Warning($"消息处理器 '{processor.Name}' 已被插件 '{pluginId}' 注册，将被覆盖");
                _ = UnregisterMessageProcessorAsync(processor.Name, pluginId);
            }

            var registered = new RegisteredHandler<IServerMessageProcessor>
            {
                Handler = processor,
                PluginId = pluginId,
                RegisteredAt = DateTime.UtcNow,
                IsEnabled = true
            };

            _messageProcessors.Add(registered);

            // 按优先级排序（降序）
            _messageProcessors.Sort((a, b) => b.Handler.Priority.CompareTo(a.Handler.Priority));

            _logger.Info($"[{pluginId}] 注册消息处理器: {processor.Name} (优先级: {processor.Priority})");
        }

        // 初始化处理器
        try
        {
            await processor.InitializeAsync();
            _logger.Debug($"[{pluginId}] 消息处理器 '{processor.Name}' 初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error($"[{pluginId}] 消息处理器 '{processor.Name}' 初始化失败", ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UnregisterMessageProcessorAsync(string processorName, string pluginId)
    {
        IServerMessageProcessor? processor = null;

        lock (_lock)
        {
            var registered = _messageProcessors.FirstOrDefault(p => p.Handler.Name == processorName && p.PluginId == pluginId);
            if (registered != null)
            {
                processor = registered.Handler;
                _messageProcessors.Remove(registered);
                _logger.Info($"[{pluginId}] 注销消息处理器: {processorName}");
            }
        }

        // 关闭处理器
        if (processor != null)
        {
            try
            {
                await processor.ShutdownAsync();
                _logger.Debug($"[{pluginId}] 消息处理器 '{processorName}' 已关闭");
            }
            catch (Exception ex)
            {
                _logger.Error($"[{pluginId}] 消息处理器 '{processorName}' 关闭失败", ex);
            }
        }
    }

    /// <inheritdoc/>
    public async Task UnregisterAllHandlersAsync(string pluginId)
    {
        List<IServerMessageProcessor> processorsToShutdown;

        lock (_lock)
        {
            // 移除输出处理器
            var outputCount = _outputHandlers.RemoveAll(h => h.PluginId == pluginId);

            // 移除消息处理器
            processorsToShutdown = _messageProcessors
                .Where(p => p.PluginId == pluginId)
                .Select(p => p.Handler)
                .ToList();

            _messageProcessors.RemoveAll(p => p.PluginId == pluginId);

            if (outputCount > 0 || processorsToShutdown.Count > 0)
            {
                _logger.Info($"[{pluginId}] 注销所有处理器: {outputCount} 个输出处理器, {processorsToShutdown.Count} 个消息处理器");
            }
        }

        // 关闭消息处理器
        foreach (var processor in processorsToShutdown)
        {
            try
            {
                await processor.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"[{pluginId}] 关闭消息处理器 '{processor.Name}' 失败", ex);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<RegisteredHandler<IServerOutputHandler>> GetOutputHandlers()
    {
        lock (_lock)
        {
            return _outputHandlers.Where(h => h.IsEnabled).ToList();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<RegisteredHandler<IServerMessageProcessor>> GetMessageProcessors()
    {
        lock (_lock)
        {
            return _messageProcessors.Where(p => p.IsEnabled).ToList();
        }
    }

    /// <inheritdoc/>
    public HandlerSummary GetPluginHandlers(string pluginId)
    {
        lock (_lock)
        {
            var outputHandlers = _outputHandlers.Where(h => h.PluginId == pluginId && h.IsEnabled).ToList();
            var messageProcessors = _messageProcessors.Where(p => p.PluginId == pluginId && p.IsEnabled).ToList();

            return new HandlerSummary
            {
                OutputHandlerCount = outputHandlers.Count,
                MessageProcessorCount = messageProcessors.Count,
                OutputHandlerNames = outputHandlers.Select(h => h.Handler.Name).ToList(),
                MessageProcessorNames = messageProcessors.Select(p => p.Handler.Name).ToList()
            };
        }
    }

    /// <summary>
    /// 处理服务器输出（内部方法，由 ServerProcessManager 调用）
    /// </summary>
    internal async Task ProcessOutputAsync(ServerOutputContext context)
    {
        var handlers = GetOutputHandlers();

        foreach (var registered in handlers)
        {
            // 跳过已处理且优先级较低的
            if (context.IsHandled && registered.Handler.Priority < 50)
                continue;

            // 快速过滤
            if (!registered.Handler.ShouldHandle(context.RawLine))
                continue;

            try
            {
                var result = await registered.Handler.HandleAsync(context);

                if (!result.Success)
                {
                    _logger.Warning($"处理器 '{registered.Handler.Name}' 执行失败: {result.Message}");
                }

                if (result.StopPropagation)
                {
                    _logger.Trace($"处理器 '{registered.Handler.Name}' 停止后续处理");
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"处理器 '{registered.Handler.Name}' 执行异常", ex);
            }
        }
    }

    /// <summary>
    /// 处理服务器消息（内部方法）
    /// </summary>
    internal async Task ProcessMessageAsync(ServerMessage message)
    {
        var processors = GetMessageProcessors();

        foreach (var registered in processors)
        {
            if (!registered.Handler.ShouldProcess(message))
                continue;

            try
            {
                await registered.Handler.ProcessMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.Error($"消息处理器 '{registered.Handler.Name}' 执行异常", ex);
            }
        }
    }

    /// <summary>
    /// 批量处理服务器消息（内部方法）
    /// </summary>
    internal async Task ProcessBatchAsync(IReadOnlyList<ServerMessage> messages)
    {
        if (messages.Count == 0)
            return;

        var processors = GetMessageProcessors();

        foreach (var registered in processors)
        {
            var messagesToProcess = messages.Where(m => registered.Handler.ShouldProcess(m)).ToList();
            if (messagesToProcess.Count == 0)
                continue;

            try
            {
                await registered.Handler.ProcessBatchAsync(messagesToProcess);
            }
            catch (Exception ex)
            {
                _logger.Error($"消息处理器 '{registered.Handler.Name}' 批量处理异常", ex);
            }
        }
    }
}

