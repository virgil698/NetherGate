using System.Text.Json.Serialization;

namespace NetherGate.API.Protocol;

/// <summary>
/// JSON-RPC 2.0 请求
/// </summary>
public class JsonRpcRequest
{
    /// <summary>
    /// JSON-RPC 协议版本
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// 请求 ID
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// 方法名
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 参数
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 响应
/// </summary>
public class JsonRpcResponse
{
    /// <summary>
    /// JSON-RPC 协议版本
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// 请求 ID
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// 结果
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Error == null;
}

/// <summary>
/// JSON-RPC 2.0 错误
/// </summary>
public class JsonRpcError
{
    /// <summary>
    /// 错误代码
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 附加数据
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 通知（无需响应）
/// </summary>
public class JsonRpcNotification
{
    /// <summary>
    /// JSON-RPC 协议版本
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// 方法名
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 参数
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; set; }

    /// <summary>
    /// 获取类型化的参数
    /// </summary>
    public T? GetParams<T>() where T : class
    {
        if (Params == null)
            return null;

        try
        {
            // 如果已经是目标类型，直接返回
            if (Params is T typedParams)
                return typedParams;

            // 否则尝试通过 JSON 序列化/反序列化进行转换
            var json = System.Text.Json.JsonSerializer.Serialize(Params);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// JSON-RPC 错误代码
/// </summary>
public static class JsonRpcErrorCodes
{
    /// <summary>
    /// 解析错误
    /// </summary>
    public const int ParseError = -32700;
    
    /// <summary>
    /// 无效请求
    /// </summary>
    public const int InvalidRequest = -32600;
    
    /// <summary>
    /// 方法未找到
    /// </summary>
    public const int MethodNotFound = -32601;
    
    /// <summary>
    /// 无效参数
    /// </summary>
    public const int InvalidParams = -32602;
    
    /// <summary>
    /// 内部错误
    /// </summary>
    public const int InternalError = -32603;
}

