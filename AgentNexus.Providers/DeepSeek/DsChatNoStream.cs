using AgentNexus.Core.Models;
using AgentNexus.Core.Models.Return;
using AgentNexus.Tooling;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static AgentNexus.Core.Models.RequestJson;
namespace AgentNexus.Providers.DeepSeek;
/// <summary>
/// 构建非流式请求的对话
/// </summary>
public class DsChatNoStream : DsChat
{
    /// <summary>
    /// 此对话的名称，默认为<see cref="string.Empty"/>
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 此对话的基础配置
    /// </summary>
    public ChatOptions Options { get; set; }
    /// <summary>
    /// 使用给定的Key构建非流式请求的对话
    /// </summary>
    /// <param name="apiKey">apiKey</param>
    /// <param name="options">当前对话的基础配置，默认为<see cref="Nullable"/></param>
    public DsChatNoStream(string apiKey, ChatOptions? options = null)
        : this("", apiKey, options)
    {
    }
    /// <summary>
    /// 使用给定的提示词和API密钥构建对话
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="apiKey">密钥</param>
    /// <param name="options">当前对话的基础配置，默认为<see cref="Nullable"/></param>
    public DsChatNoStream(string prompt, string apiKey, ChatOptions? options = null)
        : this(prompt, apiKey, new HttpClient(), options)
    {
    }
    /// <summary>
    /// 使用给定的提示词、API密码、<see cref="HttpClient"/> 构建对话
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="apiKey">密钥</param>
    /// <param name="client">HttpClient</param>
    /// <param name="options">当前对话的基础配置，默认为<see cref="Nullable"/></param>
    public DsChatNoStream(string prompt, string apiKey, HttpClient client, ChatOptions? options = null)
        : base(prompt, apiKey, client)
    {
        if (options == null) {
            var opt = new ChatOptions()
            {
                EnableContext = true,
                EnableFunctionCall = true,
            };
            Options = opt;
        } else {
            Options = options;
        }
    }
    //工具调用的完整流程
    // 1 => 用户拉起对话
    // 2 => AI表示需要调用工具 进行第一次返回 此次返回的Message将为空
    // 3 => Tools解析并调用工具 然后以Tools的角色返回(调用Chat)还要将ToolCallId返回
    // 4 => AI理解工具返回的内容给出反馈
    // 5 => 对话结束
    /// <summary>
    /// 非流式请求
    /// </summary>
    /// <param name="content"> 此次对话的内容 </param>
    /// <param name="tool_call_id"> 如果为工具调用 则请传入调用id </param>
    /// <param name="dsroleType"> 如果为工具调用 则请传入工具角色 </param>
    /// <returns></returns>
    public async Task<ReturnValue> Chat(string content, string? tool_call_id, DsRoleType dsroleType = DsRoleType.user)
    {
        var message = new Message(content, dsroleType);
        if (dsroleType == DsRoleType.tool) {
            message.ToolCallId = tool_call_id;
        }
        //用户对话的消息
        messages.Add(message);
        var origValue = new ReturnValue()
        {
            AllValue = "",
            Value = "",
            IsEnd = true,
            IsFunctionCall = false,
            IsStream = false,
            Function = null
        };
        var reMessage = GetRequestMessage(
            stream: false,
            functionCall: Options.EnableFunctionCall
        );
        var responseMessage = await client.SendAsync(reMessage, CancellationToken.None);
        if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK) {
            string v = await responseMessage.Content.ReadAsStringAsync();
            v = responseMessage.StatusCode + v;
            var badRet = origValue with
            {
                AllValue = v, //失败
                Value = v,
            };
            return badRet;
        }
        var responseValue = await responseMessage.Content.ReadAsStringAsync();
        var jsonObject = JsonSerializer.Deserialize<StreamGetJson>(responseValue);
        var oneChoices = jsonObject?.Choices![0];
        //如果是因为工具调用而停止
        if (oneChoices?.FinishReason == "tool_calls") {
            //那么他的Message就含有ToolsCall
            var tool = oneChoices!.Message!.ToolCalls![0]!;
            var value = await FunctionCallExecute.DynamicInvoke(tool);
            return await Chat(value?.ToString() ?? "调用完成了, 你可以进行回复或者继续调用下一个方法。", tool.Id, DsRoleType.tool);
        }
        var requValue = oneChoices?.Message?.Content ?? "无内容";
        //ds回复的消息
        messages.Add(new RequestJson.Message(requValue, DsRoleType.assistant));
        var retValue = origValue with
        {
            AllValue = requValue,
            Value = requValue,
        };

        #region 没启用上下文
        if (Options.EnableContext == false) {
            messages.Clear();
            var systemPrompt = new Message(Prompt, DsRoleType.system);
            messages.Add(systemPrompt);
        }
        #endregion
        return retValue;
    }
}