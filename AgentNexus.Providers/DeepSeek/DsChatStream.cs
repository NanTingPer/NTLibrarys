using AgentNexus.Core.Models;
using AgentNexus.Core.Models.Return;
using AgentNexus.Tooling;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using static AgentNexus.Core.Models.RequestJson;
namespace AgentNexus.Providers.DeepSeek;
/// <summary>
/// 流式对话
/// </summary>
public class DsChatStream : DsChat
{
    /// <summary>
    /// 此对话的名称，默认为<see cref="string.Empty"/>
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 当对话结束时，触发此事件，传入对话完整内容
    /// </summary>
    public event Action<string>? EndEvent;
    /// <summary>
    /// 配置项
    /// </summary>
    public ChatOptions Options { get; private set; }
    #region ctor
    /// <summary>
    /// 使用给定的Key构建流式请求的对话
    /// </summary>
    /// <param name="apiKey">apiKey</param>
    /// <param name="options"> 配置项 </param>
    public DsChatStream(string apiKey, ChatOptions? options = null)
        : this("", apiKey, options)
    {
    }
    /// <summary>
    /// 使用给定的提示词和API密钥构建对话
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="apiKey">密钥</param>
    /// <param name="options"> 配置项 </param>
    public DsChatStream(string prompt, string apiKey, ChatOptions? options = null)
        : this(prompt, apiKey, new HttpClient(), options)
    {
    }
    /// <summary>
    /// 使用给定的提示词、API密码、<see cref="HttpClient"/> 构建对话
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="apiKey">密钥</param>
    /// <param name="client">HttpClient</param>
    /// <param name="options"> 配置项 </param>
    public DsChatStream(string prompt, string apiKey, HttpClient client, ChatOptions? options = null)
        : base(prompt, apiKey, client)
    {
        Options = options ?? new ChatOptions()
        {
            EnableContext = true,
            EnableFunctionCall = true
        };
    }
    #endregion
    /// <summary>
    /// 对话，<paramref name="content"/>是此次发送的内容
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="tool_call_id"> 方法调用的ID 非方法调用留空 </param>
    /// <param name="dsroleType"> 此次对话的主体 </param>
    /// <param name="token"> 取消令牌 </param>
    /// <returns></returns>
    public async IAsyncEnumerable<ReturnValue> Chat(string content, string? tool_call_id = null, DsRoleType dsroleType = DsRoleType.user, CancellationToken? token = null)
    {
        #region 将对话的消息加入到消息上下文
        var message = new Message(content, dsroleType);
        if (dsroleType == DsRoleType.tool) {
            message.ToolCallId = tool_call_id;
        }
        messages.Add(message);
        #endregion
        #region 初始化ReturenValue
        var origValue = new ReturnValue()
        {
            AllValue = "",
            Value = "",
            IsEnd = false,
            IsFunctionCall = false,
            IsStream = true,
            Function = null
        };
        #endregion
        var reMessage = GetRequestMessage(
            stream: true,
            functionCall: Options.EnableFunctionCall
        );
        //对于流请求，只读头就行
        var response = await client.SendAsync(
            request: reMessage,
            completionOption: HttpCompletionOption.ResponseHeadersRead,
            cancellationToken: token ?? CancellationToken.None
        );
        if (IfErrTry(() => response.EnsureSuccessStatusCode(), out _) || response.StatusCode != System.Net.HttpStatusCode.OK) {
            var badReason = await response.Content.ReadAsStringAsync();
            var badValue = origValue with { AllValue = badReason, Value = badReason, IsEnd = true };
            yield return badValue;
            yield break; //break后整个方法体结束
        }
        bool isFunctionCall = false;
        bool isInitFunctionCall = false;
        bool isDsFunctionCall = false; //是否是来自Ds官方文档的函数调用
        #region FunctionCall的参数 他连参数的传递都是流的
        ToolCall? toolCall = null;
        #endregion
        StringBuilder msgValue = new();
        await foreach (var item in StreamChatHandel(response)) {
            #region 为ToolCall赋值 因为方法参数等都是流式传输的 需要进行拼接
            if (item.json != null && Options.EnableFunctionCall) {
                if (item.json.Choices != null &&
                    item.json.Choices.Count > 0 &&
                    item.json.Choices[0].Delta != null &&
                    item.json.Choices[0].Delta!.ToolCalls != null &&
                    item.json.Choices[0].Delta!.ToolCalls!.Count > 0) {
                    var oneChoice = item.json.Choices[0];
                    var toolCallChip = item.json.Choices[0].Delta!.ToolCalls![0];
                    if (toolCall == null) {
                        toolCall = toolCallChip;
                    } else {
                        toolCall.Function!.Arguments += toolCallChip.Function!.Arguments!;
                    }
                    var chipName = toolCallChip.Function?.Name;
                    if (toolCall?.Function!.Name == null && chipName != null) {
                        toolCall!.Function!.Name = chipName;
                    }
                    if (toolCall!.Id == null || string.IsNullOrEmpty(toolCall.Id)) {
                        toolCall.Id = toolCallChip.Id;
                    }
                }
                if (item.json?.Choices?[0]?.FinishReason == "tool_calls") {
                    isDsFunctionCall = true;
                }
            }
            #endregion
            if (string.IsNullOrWhiteSpace(item.oneChar) || string.IsNullOrEmpty(item.oneChar)) {
                yield return origValue with
                {
                    AllValue = item.oneChar,
                    Value = item.oneChar,
                    IsFunctionCall = false,
                    Function = null
                };
            }
            msgValue.Append(item.oneChar);
            //判断是不是FunctionCall, 提示词严格要求，如果是FunctionCall要返回Json
            #region Bool FunctionCall 已过时
            if (isInitFunctionCall == false && Options.EnableFunctionCall) {
                if (item.oneChar.StartsWith('{')) {
                    isFunctionCall = true;
                }
                isInitFunctionCall = true;
            }
            #endregion
            //方法调用 不应该返回内容
            //所以不会进入此if语句内
            if (!isFunctionCall && !isDsFunctionCall) {
                var centerRetValue = origValue with
                {
                    AllValue = item.oneChar,
                    Value = item.oneChar,
                    IsFunctionCall = false,
                    Function = null
                };
                yield return centerRetValue;
            }
        }
        var endValue = msgValue.ToString();
        if (!isFunctionCall && !isDsFunctionCall) {//方法调用 不应该返回内容
            var retValue = origValue with
            {
                AllValue = endValue,
                Value = endValue,
                IsEnd = true,
                IsFunctionCall = false,
                Function = null
            };
            yield return retValue;
        }
        #region 方法调用
        if (isDsFunctionCall && Options.EnableFunctionCall) {
            await foreach (var item in FunctionCall(toolCall!)) {
                yield return item;
            }
            ;
        }
        #endregion
        if (Options.EnableContext) {
            messages.Add(new Message(endValue, DsRoleType.assistant));
        }
        EndEvent?.Invoke(endValue); //调用结束事件
        if (Options.EnableContext == false) {
            messages.Clear();
            messages.Add(new RequestJson.Message(Prompt, DsRoleType.system));
        }
        yield break;
    }

    /// <summary>
    /// 方法调用
    /// </summary>
    /// <param name="toolCall"></param>
    /// <returns></returns>
    protected virtual async IAsyncEnumerable<ReturnValue> FunctionCall(ToolCall toolCall)
    {
        var value = await FunctionCallExecute.DynamicInvoke(toolCall!);
        var toolValue = value?.ToString() ?? "已经调用完成";
        messages.Add(new Message(toolCall!));
        // Tool的消息，要求传入Toolid
        await foreach (var item in Chat(toolValue, toolCall!.Id, DsRoleType.tool)) {
            yield return item;
        }
    }
}