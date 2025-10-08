namespace NetherGate.API.GameDisplay;

/// <summary>
/// IGameDisplayApi 的富文本扩展方法
/// </summary>
public static class GameDisplayExtensions
{
	/// <summary>
	/// 发送富文本聊天消息（JSON 文本组件）
	/// </summary>
	public static Task SendRichMessageAsync(this IGameDisplayApi api, string targets, TextComponent component)
	{
		return api.SendFormattedMessageAsync(targets, component.ToJson());
	}

	/// <summary>
	/// 显示富文本标题/副标题
	/// </summary>
	public static Task ShowRichTitleAsync(this IGameDisplayApi api, string targets, TextComponent title, TextComponent? subtitle = null, int fadeIn = 10, int stay = 70, int fadeOut = 20)
	{
		return api.ShowTitleAsync(targets, title.ToJson(), subtitle?.ToJson(), fadeIn, stay, fadeOut);
	}

	/// <summary>
	/// 显示富文本 ActionBar（兼容性考虑，内部使用 tellraw JSON）
	/// </summary>
	public static Task ShowRichActionBarAsync(this IGameDisplayApi api, string targets, TextComponent message)
	{
		// tellraw 更稳定支持 JSON 文本；部分实现也支持 title actionbar JSON
		return api.SendFormattedMessageAsync(targets, message.ToJson());
	}
}


