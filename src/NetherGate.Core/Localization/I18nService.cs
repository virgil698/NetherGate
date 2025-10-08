using System.Globalization;
using System.Text.Json;
using NetherGate.API.Localization;
using NetherGate.API.Logging;

namespace NetherGate.Core.Localization;

/// <summary>
/// 简单 JSON 驱动的 i18n 实现，加载 lang/{locale}.json
/// 支持点号路径与占位符替换
/// </summary>
internal class I18nService : II18nService
{
	private readonly string _langDirectory;
	private readonly ILogger _logger;
	private readonly Dictionary<string, object> _cache = new();

	public string CurrentLocale { get; private set; }

	public I18nService(string langDirectory, string? locale, ILogger logger)
	{
		_langDirectory = langDirectory;
		_logger = logger;
		CurrentLocale = NormalizeLocale(locale) ?? NormalizeLocale(GetSystemLocale()) ?? "en_us";
		LoadLocale(CurrentLocale);
	}

	public string Translate(string key, IDictionary<string, string>? variables = null, string? locale = null)
	{
		if (!string.IsNullOrEmpty(locale) && !string.Equals(NormalizeLocale(locale), CurrentLocale, StringComparison.OrdinalIgnoreCase))
		{
			CurrentLocale = NormalizeLocale(locale)!;
			_cache.Clear();
			LoadLocale(CurrentLocale);
		}

		var message = GetNested(key) ?? key;
		if (variables != null && variables.Count > 0)
		{
			foreach (var (k, v) in variables)
			{
				message = message.Replace("{" + k + "}", v);
			}
		}
		return message;
	}

	private void LoadLocale(string locale)
	{
		try
		{
			if (!Directory.Exists(_langDirectory)) return;
			var path = Path.Combine(_langDirectory, $"{locale}.json");
			if (!File.Exists(path)) return;
			var json = File.ReadAllText(path);
			var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
			if (data != null)
			{
				_cache.Clear();
				foreach (var kv in data)
				{
					_cache[kv.Key] = kv.Value!;
				}
			}
		}
		catch (Exception ex)
		{
			_logger.Debug($"加载语言文件失败: {ex.Message}");
		}
	}

	private string? GetNested(string key)
	{
		var parts = key.Split('.');
		object? current = _cache;
		foreach (var part in parts)
		{
			if (current is Dictionary<string, object> dict)
			{
				if (!dict.TryGetValue(part, out var next)) return null;
				current = next;
			}
			else
			{
				return current?.ToString();
			}
		}
		return current?.ToString();
	}

	private static string GetSystemLocale()
	{
		var env = Environment.GetEnvironmentVariable("NETHERGATE_LOCALE");
		if (!string.IsNullOrWhiteSpace(env)) return env!;
		return CultureInfo.CurrentUICulture.Name;
	}

	private static string? NormalizeLocale(string? locale)
	{
		if (string.IsNullOrWhiteSpace(locale)) return null;
		return locale!.Replace('-', '_').ToLowerInvariant();
	}
}


