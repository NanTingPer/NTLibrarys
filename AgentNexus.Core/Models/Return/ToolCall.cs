using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models.Return;

/// <summary>
/// 当Ds要调用方法时，会返回这个
/// <para> choices.message.tool_calls </para>
/// </summary>
public class ToolCall
{
    /// <summary>
    /// 索引
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;

    /// <summary>
    /// tool调用的ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 函数类型 只支持function
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// 要被调用的方法信息
    /// </summary>
    [JsonPropertyName("function")]
    public ToolCallFunction? Function { get; set; }

    private static JsonSerializerOptions options = new JsonSerializerOptions()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    /// <summary>
    /// 使用Json序列化
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, options);
    }
}
