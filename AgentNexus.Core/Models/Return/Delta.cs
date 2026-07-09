using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models.Return;

public class Delta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// R1的推理内容
    /// </summary>
    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    /// <summary>
    /// 当此次停止为工具调用时，这个就不是空了
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; set; }
}
