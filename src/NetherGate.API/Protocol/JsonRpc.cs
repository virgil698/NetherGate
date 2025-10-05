using System.Text.Json.Serialization;

namespace NetherGate.API.Protocol;

/// <summary>
/// JSON-RPC 2.0 请求
/// </summary>
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 响应
/// </summary>
public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    [JsonIgnore]
    public bool IsSuccess => Error == null;
}

/// <summary>
/// JSON-RPC 2.0 错误
/// </summary>
public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 通知（无需响应）
/// </summary>
public class JsonRpcNotification
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

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
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;
}

