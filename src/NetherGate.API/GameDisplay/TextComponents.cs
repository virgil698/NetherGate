using System.Text.Json;

namespace NetherGate.API.GameDisplay;

/// <summary>
/// 文本样式
/// </summary>
public record TextStyle(
	string? Color = null,
	bool? Bold = null,
	bool? Italic = null,
	bool? Underlined = null,
	bool? Strikethrough = null)
{
	/// <summary>
	/// 转换为 JSON 字典
	/// </summary>
	/// <returns>包含样式属性的字典</returns>
	public IDictionary<string, object> ToJsonDict()
	{
		var dict = new Dictionary<string, object>();
		if (!string.IsNullOrEmpty(Color)) dict["color"] = Color!;
		if (Bold == true) dict["bold"] = true;
		if (Italic == true) dict["italic"] = true;
		if (Underlined == true) dict["underlined"] = true;
		if (Strikethrough == true) dict["strikethrough"] = true;
		return dict;
	}
}

/// <summary>
/// 点击/悬浮事件
/// </summary>
public record TextAction(
	string? ClickAction = null,
	string? ClickValue = null,
	string? HoverAction = null,
	string? HoverValue = null)
{
	/// <summary>
	/// 将事件应用到字典
	/// </summary>
	/// <param name="dict">目标字典</param>
	public void ApplyTo(IDictionary<string, object> dict)
	{
		if (!string.IsNullOrEmpty(ClickAction) && !string.IsNullOrEmpty(ClickValue))
		{
			dict["clickEvent"] = new Dictionary<string, object>
			{
				{"action", ClickAction!},
				{"value", ClickValue!}
			};
		}
		if (!string.IsNullOrEmpty(HoverAction) && !string.IsNullOrEmpty(HoverValue))
		{
			dict["hoverEvent"] = new Dictionary<string, object>
			{
				{"action", HoverAction!},
				{"value", HoverValue!}
			};
		}
	}
}

/// <summary>
/// 文本组件（可嵌套）
/// </summary>
public class TextComponent
{
	/// <summary>
	/// 文本内容
	/// </summary>
	public string? Text { get; init; }
	
	/// <summary>
	/// 文本样式
	/// </summary>
	public TextStyle? Style { get; init; }
	
	/// <summary>
	/// 点击/悬浮事件
	/// </summary>
	public TextAction? Action { get; init; }
	
	/// <summary>
	/// 附加的子组件列表
	/// </summary>
	public List<TextComponent> Extra { get; } = new();

	/// <summary>
	/// 创建纯文本组件
	/// </summary>
	/// <param name="text">文本内容</param>
	/// <param name="style">文本样式</param>
	/// <param name="action">点击/悬浮事件</param>
	/// <returns>文本组件实例</returns>
	public static TextComponent Plain(string text, TextStyle? style = null, TextAction? action = null)
	{
		return new TextComponent { Text = text, Style = style, Action = action };
	}

	/// <summary>
	/// 添加子组件
	/// </summary>
	/// <param name="child">子组件</param>
	/// <returns>当前组件实例（用于链式调用）</returns>
	public TextComponent Append(TextComponent child)
	{
		Extra.Add(child);
		return this;
	}

	/// <summary>
	/// 转换为 JSON 字符串
	/// </summary>
	/// <returns>JSON 字符串</returns>
	public string ToJson()
	{
		var dict = new Dictionary<string, object>();
		if (Text != null) dict["text"] = Text;
		if (Style != null)
		{
			foreach (var (k, v) in Style.ToJsonDict()) dict[k] = v;
		}
		Action?.ApplyTo(dict);
		if (Extra.Count > 0)
		{
			dict["extra"] = Extra.Select(e => e.ToJsonElement()).ToArray();
		}
		return JsonSerializer.Serialize(dict);
	}

	private JsonElement ToJsonElement()
	{
		var json = ToJson();
		using var doc = JsonDocument.Parse(json);
		return doc.RootElement.Clone();
	}
}


