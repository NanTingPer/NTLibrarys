using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models.Return;

public class Choice
{
    /// <summary>
    /// 内容 流式返回的一个 completion 增量。
    /// </summary>
    [JsonPropertyName("delta")]
    public Delta? Delta { get; set; }

    /// <summary>
    /// 如果非流式返回 使用这个
    /// </summary>
    [JsonPropertyName("message")]
    public Delta? Message { get; set; }

    /// <summary>
    /// 终止原因
    /// <para> Possible values: [stop, length, content_filter, tool_calls, insufficient_system_resource]</para>
    /// <para> 模型停止生成 token 的原因。</para>
    /// <para> stop：模型自然停止生成，或遇到 stop 序列中列出的字符串。</para>
    /// <para> length ：输出长度达到了模型上下文长度限制，或达到了 max_tokens 的限制。</para>
    /// <para> content_filter：输出内容因触发过滤策略而被过滤。</para>
    /// <para> insufficient_system_resource: 由于后端推理资源受限，请求被打断。</para>
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    /// <summary>
    /// 该 completion 在模型生成的 completion 的选择列表中的索引。
    /// </summary>
    [JsonPropertyName("index")]
    public long Index { get; set; }
}
