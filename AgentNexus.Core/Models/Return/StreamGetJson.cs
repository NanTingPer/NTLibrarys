using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models.Return;

public class StreamGetJson
{
    /// <summary>
    /// 对话唯一标识符
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// 存放消息
    /// </summary>
    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }
    /// <summary>
    /// 此消息创建的时间戳
    /// </summary>
    [JsonPropertyName("created")]
    public double Created { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// 后端配置
    /// </summary>
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; set; }

    /// <summary>
    /// 对象类型 chat.completion.chunk
    /// </summary>
    [JsonPropertyName("object")]
    public string? Object { get; set; }
}
