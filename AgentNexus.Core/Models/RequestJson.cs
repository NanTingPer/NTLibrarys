using AgentNexus.Core.AIChats.Models;
using AgentNexus.Core.Models.Return;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models;
/// <summary>
/// 请求Json, 此类型为DeepSeekAPI需求的Json格式类型
/// </summary>
public class RequestJson
{
    /// <summary>
    /// 使用给定上下文构建请求Json
    /// </summary>
    /// <param name="messages"> 上下文 </param>
    public RequestJson(List<Message> messages)
    {
        Messages = messages;
    }
    /// <summary>
    /// 构建没有任何上下文的请求Json
    /// </summary>
    public RequestJson()
    {
        Messages = [];
    }
    /// <summary>
    /// 构建单次对话的请求Json
    /// </summary>
    /// <param name="systemContent">系统提示词</param>
    /// <param name="userContent">用户消息</param>
    public RequestJson(string systemContent, string userContent)
    {
        Messages = new List<Message>()
        {
            new Message(systemContent, "system"),
            new Message(userContent, "user")
        };
    }

    /// <summary>
    /// 对话消息列表
    /// </summary>
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } //对话的消息列表

    /// <summary>
    /// 对话模型
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "deepseek-chat"; //对话模型

    /// <summary>
    /// 惩罚度
    /// </summary>

    [JsonPropertyName("frequency_penalty")]
    public int FrequencyPenalty { get; set; } = 0;

    /// <summary>
    /// 此次对话最大Token消耗
    /// </summary>

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2500;

    /// <summary>
    /// 模型输出格式
    /// </summary>
    [JsonPropertyName("response_format")]
    public ResponseFormat Response_Format { get; set; } = new ResponseFormat();

    /// <summary>
    /// 铭感词
    /// </summary>
    [JsonPropertyName("stop")]
    public string? Stop { get; set; } = null;

    /// <summary>
    /// 是否使用流式传输
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    /// <summary>
    /// 流式传输的配置
    /// </summary>

    [JsonPropertyName("stream_options")]
    public string? StreamOptions { get; set; } = null;

    /// <summary>
    /// 采样温度
    /// </summary>
    [JsonPropertyName("temperature")]
    public int Temperature { get; set; } = 1;


    /// <summary>
    /// 与温度一致，但不建议一起设置
    /// </summary>
    [JsonPropertyName("top_p")]
    public int Top_p { get; set; } = 1;

    //[Obsolete]
    /// <summary>
    /// ToolCall (不使用)
    /// </summary>
    [JsonPropertyName("tools")]
    public List<DsFunctionToolsSchema>? Tools { get; set; } = null;

    //[Obsolete]
    /// <summary> 
    /// 控制Tool调用 (不使用)
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public string Tool_Choice { get; set; } = "none";

    /// <summary>
    /// 是否返回Token概率
    /// </summary>
    [JsonPropertyName("logprobs")]
    public bool Logprobs { get; set; } = false;

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("top_logprobs")]
    public string? TopLogprobs { get; set; } = null;

    /// <summary>
    /// 对话中的一条消息
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 如果对话中 AI 调用了工具，请将此次调用加入到上下文
        /// </summary>
        /// <param name="toolCall">工具调用信息</param>
        public Message(ToolCall toolCall)
        {
            Role = DsRoleType.assistant.ToString();
            ToolCalls = [toolCall];
            ToolCallId = toolCall.Id;
        }
        /// <summary>
        /// 构建常规消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="role">发送此消息的角色</param>
        public Message(string content, DsRoleType role)
        {
            Content = content;
            Role = role.ToString();
        }
        /// <summary>
        /// 构建常规消息
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="role">发送此消息的角色</param>
        public Message(string content, string role)
        {
            Content = content;
            Role = role;
        }
        /// <summary>
        /// 此消息的内容，如果为工具调用，那么可以为null
        /// </summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        /// <summary>
        /// 拥有此消息的角色，使用枚举<see cref="DsRoleType"/>，或自定义
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }

        /// <summary>
        /// 如果是工具调用的结果 需要将携带此属性的消息压入上下文
        /// </summary>
        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }

        /// <summary>
        /// 如果是工具调用 需要将携带此属性的消息压入上下文
        /// </summary>
        [JsonPropertyName("tool_calls")]
        public List<ToolCall>? ToolCalls { get; set; }
    }
    /// <summary>
    /// API返回结果的数据类型
    /// </summary>
    public class ResponseFormat
    {
        /// <summary>
        /// 构建返回结果数据类型对象
        /// </summary>
        /// <param name="type">数据类型，默认为<see cref="ResponseFormats.text"/></param>
        public ResponseFormat(ResponseFormats type = ResponseFormats.text)
        {
            Type = type.ToString();
        }
        /// <summary>
        /// 实际的类型，默认为<see cref="ResponseFormats.text"/>，如果要赋值 请使用<see cref="Enum.ToString()"/>
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
