namespace AgentNexus.Core.Models;
/// <summary>
/// 对话时的角色类型
/// </summary>
public enum DsRoleType
{
    /// <summary>
    /// 用户
    /// </summary>
    user,
    /// <summary>
    /// 系统
    /// </summary>
    system,
    /// <summary>
    /// DeepSeek自己
    /// </summary>
    assistant,
    /// <summary>
    /// 工具调用的返回结果
    /// </summary>
    tool
}
