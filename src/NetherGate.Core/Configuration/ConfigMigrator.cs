using NetherGate.API.Configuration;
using NetherGate.API.Logging;

namespace NetherGate.Core.Configuration;

/// <summary>
/// 配置迁移器 - 负责将旧版本配置升级到新版本
/// </summary>
public class ConfigMigrator
{
    private readonly ILogger _logger;
    private const int CurrentVersion = 1;

    public ConfigMigrator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 当前配置版本
    /// </summary>
    public static int LatestVersion => CurrentVersion;

    /// <summary>
    /// 检查配置是否需要迁移
    /// </summary>
    public bool NeedsMigration(NetherGateConfig config)
    {
        return config.ConfigVersion < CurrentVersion;
    }

    /// <summary>
    /// 迁移配置到最新版本
    /// </summary>
    public NetherGateConfig Migrate(NetherGateConfig config)
    {
        if (!NeedsMigration(config))
        {
            _logger.Debug($"配置版本 {config.ConfigVersion} 已是最新，无需迁移");
            return config;
        }

        _logger.Info($"检测到旧版配置 (v{config.ConfigVersion})，开始迁移到 v{CurrentVersion}...");

        var currentVersion = config.ConfigVersion;

        // 依次执行迁移步骤
        while (currentVersion < CurrentVersion)
        {
            _logger.Info($"执行迁移: v{currentVersion} -> v{currentVersion + 1}");
            
            config = currentVersion switch
            {
                0 => MigrateFrom0To1(config),
                // 未来版本迁移在此添加
                // 1 => MigrateFrom1To2(config),
                // 2 => MigrateFrom2To3(config),
                _ => throw new NotSupportedException($"不支持从版本 {currentVersion} 迁移")
            };

            currentVersion++;
        }

        config.ConfigVersion = CurrentVersion;
        _logger.Info($"配置迁移完成: v{CurrentVersion}");

        return config;
    }

    /// <summary>
    /// 从 v0 (无版本号) 迁移到 v1
    /// </summary>
    private NetherGateConfig MigrateFrom0To1(NetherGateConfig config)
    {
        _logger.Info("  添加 config_version 字段");
        
        // v0 到 v1 的变更：
        // - 添加 config_version 字段
        // - 其他配置保持不变
        
        config.ConfigVersion = 1;
        return config;
    }

    // 未来版本迁移示例：
    /*
    /// <summary>
    /// 从 v1 迁移到 v2
    /// </summary>
    private NetherGateConfig MigrateFrom1To2(NetherGateConfig config)
    {
        _logger.Info("  示例迁移: 添加新配置项");
        
        // 示例变更：
        // - 重命名某个字段
        // - 添加新的默认值
        // - 调整结构
        
        config.ConfigVersion = 2;
        return config;
    }
    */
}

