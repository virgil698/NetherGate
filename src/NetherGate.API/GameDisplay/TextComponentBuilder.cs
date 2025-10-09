namespace NetherGate.API.GameDisplay;

/// <summary>
/// 文本组件构建器
/// 用于构建复杂的 Minecraft 文本组件（Minecraft 1.21.9+）
/// </summary>
public class TextComponentBuilder
{
    private readonly List<object> _components = new();

    /// <summary>
    /// 添加普通文本
    /// </summary>
    public TextComponentBuilder Text(string text, string? color = null, bool? bold = null, bool? italic = null)
    {
        var component = new Dictionary<string, object> { ["text"] = text };
        
        if (color != null) component["color"] = color;
        if (bold != null) component["bold"] = bold.Value;
        if (italic != null) component["italic"] = italic.Value;
        
        _components.Add(component);
        return this;
    }

    /// <summary>
    /// 添加精灵图对象（Minecraft 1.21.9+）
    /// </summary>
    /// <param name="type">对象类型：atlas 或 sprite</param>
    /// <param name="value">
    /// 当 type 为 "atlas" 时，值为图集命名空间ID（如 "minecraft:blocks"）
    /// 当 type 为 "sprite" 时，值为精灵图命名空间ID（如 "item/porkchop"）
    /// </param>
    public TextComponentBuilder Sprite(string type, string value)
    {
        var component = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["object"] = new Dictionary<string, string>
            {
                [type] = value
            }
        };
        
        _components.Add(component);
        return this;
    }

    /// <summary>
    /// 添加玩家头像对象（Minecraft 1.21.9+）
    /// </summary>
    /// <param name="playerName">玩家名称</param>
    /// <param name="renderHat">是否渲染帽子部分</param>
    public TextComponentBuilder PlayerHead(string playerName, bool renderHat = true)
    {
        var component = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["object"] = new Dictionary<string, object>
            {
                ["player"] = playerName,
                ["hat"] = renderHat
            }
        };
        
        _components.Add(component);
        return this;
    }

    /// <summary>
    /// 添加玩家头像对象（使用档案数据）
    /// </summary>
    /// <param name="profile">玩家档案</param>
    /// <param name="renderHat">是否渲染帽子部分</param>
    public TextComponentBuilder PlayerHead(Data.PlayerProfile profile, bool renderHat = true)
    {
        var profileData = new Dictionary<string, object>
        {
            ["name"] = profile.Name,
            ["id"] = profile.Uuid.ToString()
        };

        // 添加纹理属性
        if (profile.Properties.Count > 0)
        {
            var properties = new List<Dictionary<string, object>>();
            foreach (var prop in profile.Properties)
            {
                var propDict = new Dictionary<string, object>
                {
                    ["name"] = prop.Name,
                    ["value"] = prop.Value
                };
                if (!string.IsNullOrEmpty(prop.Signature))
                {
                    propDict["signature"] = prop.Signature;
                }
                properties.Add(propDict);
            }
            profileData["properties"] = properties;
        }

        var component = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["object"] = new Dictionary<string, object>
            {
                ["player"] = profileData,
                ["hat"] = renderHat
            }
        };
        
        _components.Add(component);
        return this;
    }

    /// <summary>
    /// 添加可点击的文本
    /// </summary>
    public TextComponentBuilder Clickable(string text, ClickAction action, string value, string? color = null)
    {
        var component = new Dictionary<string, object>
        {
            ["text"] = text,
            ["clickEvent"] = new Dictionary<string, string>
            {
                ["action"] = action.ToString().ToLower(),
                ["value"] = value
            }
        };
        
        if (color != null) component["color"] = color;
        
        _components.Add(component);
        return this;
    }

    /// <summary>
    /// 添加悬停提示文本
    /// </summary>
    public TextComponentBuilder Hover(string text, string hoverText, string? color = null)
    {
        var component = new Dictionary<string, object>
        {
            ["text"] = text,
            ["hoverEvent"] = new Dictionary<string, object>
            {
                ["action"] = "show_text",
                ["contents"] = hoverText
            }
        };
        
        if (color != null) component["color"] = color;
        
        _components.Add(component);
        return this;
    }

    /// <summary>
    /// 添加换行符
    /// </summary>
    public TextComponentBuilder NewLine()
    {
        _components.Add(new Dictionary<string, object> { ["text"] = "\n" });
        return this;
    }

    /// <summary>
    /// 构建最终的 JSON 文本组件
    /// </summary>
    public string Build()
    {
        if (_components.Count == 0)
            return "\"\"";

        if (_components.Count == 1)
            return System.Text.Json.JsonSerializer.Serialize(_components[0]);

        // 多个组件合并
        var result = new Dictionary<string, object>
        {
            ["text"] = "",
            ["extra"] = _components
        };

        return System.Text.Json.JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// 清空所有组件
    /// </summary>
    public TextComponentBuilder Clear()
    {
        _components.Clear();
        return this;
    }
}

/// <summary>
/// 点击动作类型
/// </summary>
public enum ClickAction
{
    /// <summary>
    /// 打开 URL
    /// </summary>
    OpenUrl,

    /// <summary>
    /// 执行命令
    /// </summary>
    RunCommand,

    /// <summary>
    /// 建议命令（填充到聊天框）
    /// </summary>
    SuggestCommand,

    /// <summary>
    /// 复制到剪贴板
    /// </summary>
    CopyToClipboard
}

