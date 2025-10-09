namespace NetherGate.Core.Utilities;

/// <summary>
/// 字符串转义工具类
/// 提供 JSON、SNBT 等格式的字符串转义功能
/// </summary>
public static class StringEscapeUtils
{
    /// <summary>
    /// 转义 JSON 文本
    /// 如果输入已经是 JSON 格式（以 { 或 [ 开头），则直接返回
    /// 否则包装为简单的 JSON 文本组件格式：{"text":"..."}
    /// </summary>
    /// <param name="text">要转义的文本</param>
    /// <returns>转义后的 JSON 字符串</returns>
    public static string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "{\"text\":\"\"}";

        // 如果已经是 JSON 格式（以 { 或 [ 开头），直接返回
        var trimmed = text.TrimStart();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            return text;
        }
        
        // 否则包装为简单的 JSON 文本组件
        var escaped = text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
        
        return $"{{\"text\":\"{escaped}\"}}";
    }

    /// <summary>
    /// 转义 SNBT (Stringified NBT) 字符串
    /// 用于 Minecraft 命令中的 NBT 数据
    /// </summary>
    /// <param name="text">要转义的文本</param>
    /// <returns>转义后的 SNBT 字符串</returns>
    public static string EscapeSnbt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")  // 反斜杠
            .Replace("\"", "\\\"")  // 双引号
            .Replace("'", "\\'")    // 单引号
            .Replace("\n", "\\n")   // 换行符
            .Replace("\r", "\\r")   // 回车符
            .Replace("\t", "\\t");  // 制表符
    }

    /// <summary>
    /// 为 SNBT 字符串添加引号并转义
    /// </summary>
    /// <param name="text">要转义的文本</param>
    /// <param name="useDoubleQuotes">是否使用双引号（默认使用单引号）</param>
    /// <returns>带引号的转义字符串</returns>
    public static string QuoteSnbt(string text, bool useDoubleQuotes = false)
    {
        var escaped = EscapeSnbt(text);
        return useDoubleQuotes ? $"\"{escaped}\"" : $"'{escaped}'";
    }

    /// <summary>
    /// 转义 Minecraft 命令中的字符串
    /// </summary>
    /// <param name="text">要转义的文本</param>
    /// <returns>转义后的字符串</returns>
    public static string EscapeCommand(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Minecraft 命令中的特殊字符需要转义
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("'", "\\'");
    }

    /// <summary>
    /// 反转义 JSON 字符串
    /// </summary>
    /// <param name="text">JSON 转义的文本</param>
    /// <returns>反转义后的文本</returns>
    public static string UnescapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t");
    }

    /// <summary>
    /// 反转义 SNBT 字符串
    /// </summary>
    /// <param name="text">SNBT 转义的文本</param>
    /// <returns>反转义后的文本</returns>
    public static string UnescapeSnbt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\'", "'")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t");
    }

    /// <summary>
    /// 检查字符串是否需要引号
    /// 在 SNBT 中，包含空格或特殊字符的字符串需要引号
    /// </summary>
    /// <param name="text">要检查的文本</param>
    /// <returns>如果需要引号则返回 true</returns>
    public static bool NeedsQuotes(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;

        // 检查是否包含空格或特殊字符
        return text.Any(c => char.IsWhiteSpace(c) || 
                            c == '"' || c == '\'' || c == '\\' || 
                            c == '{' || c == '}' || c == '[' || c == ']' ||
                            c == ':' || c == ',');
    }

    /// <summary>
    /// 安全地为字符串添加引号（如果需要）
    /// </summary>
    /// <param name="text">要处理的文本</param>
    /// <param name="useDoubleQuotes">是否使用双引号</param>
    /// <returns>处理后的字符串</returns>
    public static string QuoteIfNeeded(string text, bool useDoubleQuotes = false)
    {
        if (NeedsQuotes(text))
            return QuoteSnbt(text, useDoubleQuotes);
        
        return text;
    }
}

