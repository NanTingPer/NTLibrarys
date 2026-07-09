using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models.Return;

/// <summary>
/// Ds需要进行工具调用时，用这个表示方法名称和参数
/// </summary>
public class ToolCallFunction
{
    /// <summary>
    /// 方法名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 方法参数列表
    /// </summary>
    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    /// <summary>
    /// 对<see cref="Arguments"/>进行序列化
    /// </summary>
    [JsonIgnore]
    public JsonObject ArgumentsJsonObject
    {
        get
        {
            try {
                var arg = JsonNode.Parse(Arguments ?? "") as JsonObject;
                if (arg == null)
                    return [];
                return arg;
            } catch {
                return [];
            }
        }
    }
}