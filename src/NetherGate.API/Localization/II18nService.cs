namespace NetherGate.API.Localization;

/// <summary>
/// 简单 i18n 接口，用于按 locale 获取多语言文本并做占位符替换
/// </summary>
public interface II18nService
{
    /// <summary>
    /// 当前 Locale（如 "en_us"、"zh_cn"）
    /// </summary>
    string CurrentLocale { get; }

    /// <summary>
    /// 翻译指定 key
    /// </summary>
    /// <param name="key">消息键，如 "nethergate.welcome"</param>
    /// <param name="variables">占位变量，可为空</param>
    /// <param name="locale">可选 locale，为空使用当前</param>
    /// <returns>翻译后的文本；未找到则返回 key 本身</returns>
    string Translate(string key, IDictionary<string, string>? variables = null, string? locale = null);
}


