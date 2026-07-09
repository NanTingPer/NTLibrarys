namespace AgentNexus.Core.Models;
/// <summary>
/// 流对话配置项
/// </summary>
public class ChatOptions
{
    /// <summary>
    /// 是否启用方法调用
    /// </summary>
    public bool EnableFunctionCall { get; set; } = true;
    /// <summary>
    /// 是否启用上下文
    /// </summary>
    public bool EnableContext { get; set; } = true;
}
