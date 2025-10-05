namespace NetherGate.API.Protocol;

/// <summary>
/// 服务端管理协议 (SMP) API 接口
/// </summary>
public interface ISmpApi
{
    // ========== 白名单管理 ==========
    
    /// <summary>
    /// 获取白名单
    /// </summary>
    Task<List<PlayerDto>> GetAllowlistAsync();

    /// <summary>
    /// 设置白名单（替换整个列表）
    /// </summary>
    Task SetAllowlistAsync(List<PlayerDto> players);

    /// <summary>
    /// 添加玩家到白名单
    /// </summary>
    Task AddToAllowlistAsync(PlayerDto player);

    /// <summary>
    /// 从白名单移除玩家
    /// </summary>
    Task RemoveFromAllowlistAsync(PlayerDto player);

    /// <summary>
    /// 清空白名单
    /// </summary>
    Task ClearAllowlistAsync();

    // ========== 封禁管理 ==========
    
    /// <summary>
    /// 获取玩家封禁列表
    /// </summary>
    Task<List<UserBanDto>> GetBansAsync();

    /// <summary>
    /// 设置玩家封禁列表（替换整个列表）
    /// </summary>
    Task SetBansAsync(List<UserBanDto> bans);

    /// <summary>
    /// 封禁玩家
    /// </summary>
    Task AddBanAsync(UserBanDto ban);

    /// <summary>
    /// 解除玩家封禁
    /// </summary>
    Task RemoveBanAsync(PlayerDto player);

    /// <summary>
    /// 清空玩家封禁列表
    /// </summary>
    Task ClearBansAsync();

    // ========== IP 封禁管理 ==========
    
    /// <summary>
    /// 获取 IP 封禁列表
    /// </summary>
    Task<List<IpBanDto>> GetIpBansAsync();

    /// <summary>
    /// 设置 IP 封禁列表（替换整个列表）
    /// </summary>
    Task SetIpBansAsync(List<IpBanDto> bans);

    /// <summary>
    /// 封禁 IP
    /// </summary>
    Task AddIpBanAsync(IpBanDto ban);

    /// <summary>
    /// 解除 IP 封禁
    /// </summary>
    Task RemoveIpBanAsync(string ip);

    /// <summary>
    /// 清空 IP 封禁列表
    /// </summary>
    Task ClearIpBansAsync();

    // ========== 玩家管理 ==========
    
    /// <summary>
    /// 获取在线玩家列表
    /// </summary>
    Task<List<PlayerDto>> GetPlayersAsync();

    /// <summary>
    /// 踢出玩家
    /// </summary>
    Task KickPlayerAsync(string playerName, string? reason = null);

    // ========== 管理员管理 ==========
    
    /// <summary>
    /// 获取管理员列表
    /// </summary>
    Task<List<OperatorDto>> GetOperatorsAsync();

    /// <summary>
    /// 设置管理员列表（替换整个列表）
    /// </summary>
    Task SetOperatorsAsync(List<OperatorDto> operators);

    /// <summary>
    /// 添加管理员
    /// </summary>
    Task AddOperatorAsync(OperatorDto op);

    /// <summary>
    /// 移除管理员
    /// </summary>
    Task RemoveOperatorAsync(PlayerDto player);

    /// <summary>
    /// 清空管理员列表
    /// </summary>
    Task ClearOperatorsAsync();

    // ========== 服务器管理 ==========
    
    /// <summary>
    /// 获取服务器状态
    /// </summary>
    Task<ServerState> GetServerStatusAsync();

    /// <summary>
    /// 保存世界
    /// </summary>
    Task SaveWorldAsync();

    /// <summary>
    /// 停止服务器
    /// </summary>
    Task StopServerAsync();

    /// <summary>
    /// 发送系统消息
    /// </summary>
    Task SendSystemMessageAsync(string message);

    // ========== 游戏规则管理 ==========
    
    /// <summary>
    /// 获取所有游戏规则
    /// </summary>
    Task<Dictionary<string, TypedRule>> GetGameRulesAsync();

    /// <summary>
    /// 更新游戏规则
    /// </summary>
    Task UpdateGameRuleAsync(string rule, object value);

    // ========== 服务器设置 ==========
    
    /// <summary>
    /// 获取服务器设置
    /// </summary>
    Task<Dictionary<string, object?>> GetServerSettingsAsync();

    /// <summary>
    /// 获取指定服务器设置
    /// </summary>
    Task<object?> GetServerSettingAsync(string key);

    /// <summary>
    /// 设置服务器设置
    /// </summary>
    Task SetServerSettingAsync(string key, object value);
}

