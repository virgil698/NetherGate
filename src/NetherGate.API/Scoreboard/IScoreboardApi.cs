namespace NetherGate.API.Scoreboard;

/// <summary>
/// 计分板系统 API
/// <para>计分板（Scoreboard）用于在游戏中显示分数、排行榜和统计信息。</para>
/// <para>完全基于 RCON /scoreboard 命令实现，无需文件系统支持。</para>
/// </summary>
/// <remarks>
/// 支持的功能：
/// <list type="bullet">
/// <item>目标管理（objectives）- 创建、删除、显示位置</item>
/// <item>分数操作（players）- 设置、增加、减少、获取、重置</item>
/// <item>队伍管理（teams）- 创建、删除、成员管理、选项设置</item>
/// </list>
/// </remarks>
public interface IScoreboardApi
{
    // ==================== 目标管理 ====================
    
    /// <summary>
    /// 创建计分板目标
    /// </summary>
    /// <param name="name">目标名称（不能超过 16 个字符）</param>
    /// <param name="criteria">标准类型（如 dummy、health、playerKillCount）</param>
    /// <param name="displayName">显示名称（可选，支持 JSON 格式）</param>
    /// <returns>是否创建成功</returns>
    Task<bool> CreateObjectiveAsync(string name, ScoreboardCriteria criteria = ScoreboardCriteria.Dummy, string? displayName = null);
    
    /// <summary>
    /// 删除计分板目标
    /// </summary>
    /// <param name="name">目标名称</param>
    /// <returns>是否删除成功</returns>
    Task<bool> RemoveObjectiveAsync(string name);
    
    /// <summary>
    /// 设置计分板显示位置
    /// </summary>
    /// <param name="slot">显示位置（侧边栏、玩家列表、名字下方）</param>
    /// <param name="objectiveName">目标名称</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetDisplaySlotAsync(DisplaySlot slot, string objectiveName);
    
    /// <summary>
    /// 清除计分板显示位置
    /// </summary>
    /// <param name="slot">显示位置</param>
    /// <returns>是否清除成功</returns>
    Task<bool> ClearDisplaySlotAsync(DisplaySlot slot);
    
    /// <summary>
    /// 修改目标显示名称
    /// </summary>
    /// <param name="objectiveName">目标名称</param>
    /// <param name="displayName">新的显示名称（支持 JSON 格式）</param>
    /// <returns>是否修改成功</returns>
    Task<bool> ModifyObjectiveDisplayNameAsync(string objectiveName, string displayName);
    
    /// <summary>
    /// 修改目标渲染类型
    /// </summary>
    /// <param name="objectiveName">目标名称</param>
    /// <param name="renderType">渲染类型（整数或红心）</param>
    /// <returns>是否修改成功</returns>
    Task<bool> ModifyObjectiveRenderTypeAsync(string objectiveName, RenderType renderType);
    
    // ==================== 分数操作 ====================
    
    /// <summary>
    /// 设置分数
    /// </summary>
    /// <param name="holder">分数持有者（玩家名或任意字符串）</param>
    /// <param name="objectiveName">目标名称</param>
    /// <param name="score">分数值</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetScoreAsync(string holder, string objectiveName, int score);
    
    /// <summary>
    /// 增加分数
    /// </summary>
    /// <param name="holder">分数持有者</param>
    /// <param name="objectiveName">目标名称</param>
    /// <param name="amount">增加的数量</param>
    /// <returns>是否增加成功</returns>
    Task<bool> AddScoreAsync(string holder, string objectiveName, int amount);
    
    /// <summary>
    /// 减少分数
    /// </summary>
    /// <param name="holder">分数持有者</param>
    /// <param name="objectiveName">目标名称</param>
    /// <param name="amount">减少的数量</param>
    /// <returns>是否减少成功</returns>
    Task<bool> RemoveScoreAsync(string holder, string objectiveName, int amount);
    
    /// <summary>
    /// 获取分数
    /// </summary>
    /// <param name="holder">分数持有者</param>
    /// <param name="objectiveName">目标名称</param>
    /// <returns>分数值，如果不存在则返回 null</returns>
    Task<int?> GetScoreAsync(string holder, string objectiveName);
    
    /// <summary>
    /// 重置分数
    /// </summary>
    /// <param name="holder">分数持有者</param>
    /// <param name="objectiveName">目标名称（可选，如为 null 则重置所有目标的分数）</param>
    /// <returns>是否重置成功</returns>
    Task<bool> ResetScoreAsync(string holder, string? objectiveName = null);
    
    /// <summary>
    /// 列出指定持有者的所有分数
    /// </summary>
    /// <param name="holder">分数持有者</param>
    /// <returns>所有分数列表（目标名称 -> 分数值）</returns>
    Task<Dictionary<string, int>> ListScoresAsync(string holder);
    
    // ==================== 分数操作（高级）====================
    
    /// <summary>
    /// 对分数进行算术运算
    /// </summary>
    /// <param name="targetHolder">目标持有者</param>
    /// <param name="targetObjective">目标对象</param>
    /// <param name="operation">运算符（+=、-=、*=、/=、%=、=、&lt;、&gt;、&gt;&lt;）</param>
    /// <param name="sourceHolder">源持有者</param>
    /// <param name="sourceObjective">源对象</param>
    /// <returns>是否操作成功</returns>
    Task<bool> OperationAsync(string targetHolder, string targetObjective, 
        ScoreboardOperation operation, string sourceHolder, string sourceObjective);
    
    /// <summary>
    /// 启用分数触发器（触发器模式）
    /// </summary>
    /// <param name="holder">持有者（玩家）</param>
    /// <param name="objectiveName">目标名称</param>
    /// <returns>是否启用成功</returns>
    Task<bool> EnableTriggerAsync(string holder, string objectiveName);
    
    // ==================== 队伍管理 ====================
    
    /// <summary>
    /// 创建队伍
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="displayName">显示名称（可选）</param>
    /// <returns>是否创建成功</returns>
    Task<bool> CreateTeamAsync(string teamName, string? displayName = null);
    
    /// <summary>
    /// 删除队伍
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <returns>是否删除成功</returns>
    Task<bool> RemoveTeamAsync(string teamName);
    
    /// <summary>
    /// 清空队伍（移除所有成员但保留队伍）
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <returns>是否清空成功</returns>
    Task<bool> EmptyTeamAsync(string teamName);
    
    /// <summary>
    /// 将实体加入队伍
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="members">成员（支持选择器 @a 或具体名称）</param>
    /// <returns>是否加入成功</returns>
    Task<bool> JoinTeamAsync(string teamName, string members);
    
    /// <summary>
    /// 将实体从队伍移除
    /// </summary>
    /// <param name="members">成员（支持选择器）</param>
    /// <returns>是否移除成功</returns>
    Task<bool> LeaveTeamAsync(string members);
    
    /// <summary>
    /// 修改队伍选项
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="option">选项类型</param>
    /// <param name="value">选项值</param>
    /// <returns>是否修改成功</returns>
    Task<bool> ModifyTeamOptionAsync(string teamName, TeamOption option, string value);
    
    /// <summary>
    /// 设置队伍颜色
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="color">颜色</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetTeamColorAsync(string teamName, TeamColor color);
    
    /// <summary>
    /// 设置队伍友军伤害
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetTeamFriendlyFireAsync(string teamName, bool enabled);
    
    /// <summary>
    /// 设置是否能看见隐身队友
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetTeamSeeFriendlyInvisiblesAsync(string teamName, bool enabled);
    
    /// <summary>
    /// 设置队伍名称标签可见性
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="visibility">可见性规则</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetTeamNameTagVisibilityAsync(string teamName, NameTagVisibility visibility);
    
    /// <summary>
    /// 设置队伍前缀
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="prefix">前缀（支持 JSON 格式）</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetTeamPrefixAsync(string teamName, string prefix);
    
    /// <summary>
    /// 设置队伍后缀
    /// </summary>
    /// <param name="teamName">队伍名称</param>
    /// <param name="suffix">后缀（支持 JSON 格式）</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetTeamSuffixAsync(string teamName, string suffix);
    
    // ==================== 高级功能 ====================
    
    /// <summary>
    /// 获取排行榜前 N 名
    /// </summary>
    /// <param name="objectiveName">目标名称</param>
    /// <param name="limit">数量限制（默认 10）</param>
    /// <param name="descending">是否降序（默认 true）</param>
    /// <returns>排行榜列表</returns>
    Task<List<ScoreEntry>> GetTopScoresAsync(string objectiveName, int limit = 10, bool descending = true);
    
    /// <summary>
    /// 获取玩家排名
    /// </summary>
    /// <param name="holder">持有者</param>
    /// <param name="objectiveName">目标名称</param>
    /// <returns>排名（从 1 开始），如果未上榜则返回 null</returns>
    Task<int?> GetPlayerRankAsync(string holder, string objectiveName);
}

/// <summary>
/// 计分板标准类型
/// </summary>
public enum ScoreboardCriteria
{
    /// <summary>自定义标准（由命令手动修改）</summary>
    Dummy,
    
    /// <summary>触发器标准（玩家可触发）</summary>
    Trigger,
    
    /// <summary>死亡次数</summary>
    DeathCount,
    
    /// <summary>玩家击杀数</summary>
    PlayerKillCount,
    
    /// <summary>总击杀数</summary>
    TotalKillCount,
    
    /// <summary>生命值</summary>
    Health,
    
    /// <summary>经验值</summary>
    Xp,
    
    /// <summary>等级</summary>
    Level,
    
    /// <summary>食物等级</summary>
    Food,
    
    /// <summary>空气值</summary>
    Air,
    
    /// <summary>护甲值</summary>
    Armor
}

/// <summary>
/// 显示位置
/// </summary>
public enum DisplaySlot
{
    /// <summary>侧边栏（屏幕右侧）</summary>
    Sidebar,
    
    /// <summary>玩家列表（Tab 键）</summary>
    List,
    
    /// <summary>玩家名字下方</summary>
    BelowName
}

/// <summary>
/// 渲染类型
/// </summary>
public enum RenderType
{
    /// <summary>整数</summary>
    Integer,
    
    /// <summary>红心（生命值样式）</summary>
    Hearts
}

/// <summary>
/// 计分板运算符
/// </summary>
public enum ScoreboardOperation
{
    /// <summary>加法赋值（+=）</summary>
    Add,
    
    /// <summary>减法赋值（-=）</summary>
    Subtract,
    
    /// <summary>乘法赋值（*=）</summary>
    Multiply,
    
    /// <summary>除法赋值（/=）</summary>
    Divide,
    
    /// <summary>取模赋值（%=）</summary>
    Modulo,
    
    /// <summary>赋值（=）</summary>
    Assign,
    
    /// <summary>取最小值（&lt;）</summary>
    Min,
    
    /// <summary>取最大值（&gt;）</summary>
    Max,
    
    /// <summary>交换值（&gt;&lt;）</summary>
    Swap
}

/// <summary>
/// 队伍选项
/// </summary>
public enum TeamOption
{
    /// <summary>颜色</summary>
    Color,
    
    /// <summary>友军伤害</summary>
    FriendlyFire,
    
    /// <summary>看见隐身队友</summary>
    SeeFriendlyInvisibles,
    
    /// <summary>名称标签可见性</summary>
    NameTagVisibility,
    
    /// <summary>死亡消息可见性</summary>
    DeathMessageVisibility,
    
    /// <summary>碰撞规则</summary>
    CollisionRule,
    
    /// <summary>显示名称</summary>
    DisplayName,
    
    /// <summary>前缀</summary>
    Prefix,
    
    /// <summary>后缀</summary>
    Suffix
}

/// <summary>
/// 队伍颜色
/// </summary>
public enum TeamColor
{
    /// <summary>黑色</summary>
    Black,
    
    /// <summary>深蓝色</summary>
    DarkBlue,
    
    /// <summary>深绿色</summary>
    DarkGreen,
    
    /// <summary>深青色</summary>
    DarkAqua,
    
    /// <summary>深红色</summary>
    DarkRed,
    
    /// <summary>深紫色</summary>
    DarkPurple,
    
    /// <summary>金色</summary>
    Gold,
    
    /// <summary>灰色</summary>
    Gray,
    
    /// <summary>深灰色</summary>
    DarkGray,
    
    /// <summary>蓝色</summary>
    Blue,
    
    /// <summary>绿色</summary>
    Green,
    
    /// <summary>青色</summary>
    Aqua,
    
    /// <summary>红色</summary>
    Red,
    
    /// <summary>浅紫色</summary>
    LightPurple,
    
    /// <summary>黄色</summary>
    Yellow,
    
    /// <summary>白色</summary>
    White,
    
    /// <summary>重置（移除颜色）</summary>
    Reset
}

/// <summary>
/// 名称标签可见性
/// </summary>
public enum NameTagVisibility
{
    /// <summary>总是显示</summary>
    Always,
    
    /// <summary>从不显示</summary>
    Never,
    
    /// <summary>对自己队伍隐藏</summary>
    HideForOwnTeam,
    
    /// <summary>对其他队伍隐藏</summary>
    HideForOtherTeams
}

/// <summary>
/// 分数条目
/// </summary>
public class ScoreEntry
{
    /// <summary>
    /// 分数持有者
    /// </summary>
    public string Holder { get; init; } = string.Empty;
    
    /// <summary>
    /// 分数值
    /// </summary>
    public int Score { get; init; }
    
    /// <summary>
    /// 排名（从 1 开始）
    /// </summary>
    public int Rank { get; init; }
}

