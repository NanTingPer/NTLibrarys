using AgentNexus.Core.Models;
using System.Threading.Tasks;

namespace AgentNexus.Providers.DeepSeek;
/// <summary>
/// 适用于ds的Agent工作流
/// </summary>
public class DsAgent
{
    /// <summary>
    /// 此工作流的名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 当对话被返回单个Token时调用
    /// </summary>
    public event DsAgentTokenEvent? TokenEvent;
    /// <summary>
    /// 当单次对话结束时，触发此事件
    /// </summary>
    public event DsAgentTokenEvent? EndEvent;
    /// <summary>
    /// 此工作流全部的对话
    /// </summary>
    public List<DsChatStream> Chats { get; private set; } = [];
    /// <summary>
    /// 构建工作流，至少需要一个对话
    /// </summary>
    public DsAgent(params DsChatStream[] dschats)
    {
        if (dschats.Length < 1) {
            throw new Exception("构建工作流至少需要一个对话");
        }
        Chats.AddRange(dschats);
    }
    /// <summary>
    /// 使用工作流
    /// </summary>
    /// <param name="content">对话内容</param>
    /// <returns></returns>
    public async Task<ReturnValue> Chat(string content)
    {
        ReturnValue? lastChatReturn = null;
        foreach (var chat in Chats) {
            string input = lastChatReturn?.AllValue ?? content;
            await foreach (var item in chat.Chat(input, null)) {
                if (item.IsEnd == false &&
                    !string.IsNullOrWhiteSpace(item.Value) &&
                    TokenEvent != null) {
                    var args = new DsAgentEventArgs()
                    {
                        ChatName = chat.Name,
                        Token = item.Value
                    };
                    TokenEvent.Invoke(this, args);
                }
                if (item.IsEnd) {
                    lastChatReturn = item;
                    var args = new DsAgentEventArgs()
                    {
                        Token = item.AllValue,
                        ChatName = chat.Name
                    };
                    EndEvent?.Invoke(this, args);
                }
            }
            ;
        }
        return lastChatReturn!;
    }
}

/// <summary>
/// <see cref="DsAgent"/>中事件的传入结果
/// </summary>
public class DsAgentEventArgs
{
    /// <summary>
    /// 触发事件的<see cref="DsChatStream"/>的名称
    /// </summary>
    public string ChatName { get; set; } = string.Empty;
    /// <summary>
    /// 因为是流式对话，因此返回单个Token，如果是End则返回全部内容
    /// </summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// <see cref="DsAgent"/>返回单个Token时触发事件的委托
/// </summary>
/// <param name="orig"> 触发事件的对象 </param>
/// <param name="args"> 触发事件所传递的参数 </param>
public delegate void DsAgentTokenEvent(DsAgent orig, DsAgentEventArgs args);