#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
using AgentNexus.Core.Models;
using AgentNexus.Core.Models.Return;
using AgentNexus.Tooling;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
namespace AgentNexus.Providers.DeepSeek;
/// <summary>
/// 与DeepSeek对话
/// </summary>
public abstract class DsChat : IDisposable
{
    /// <summary>
    /// 提示词
    /// </summary>
    public string Prompt { get; set; }
    protected readonly string apiKey;
    protected const string baseUrl = "https://api.deepseek.com/chat/completions";
    protected readonly Uri baseUri = new Uri(baseUrl);
    protected readonly LockList<RequestJson.Message> messages = new LockList<RequestJson.Message>();
    protected readonly HttpClient client;
    public int MaxTokens { get; set; } = 8000;
    /// <summary>
    /// 使用给定的提示词、API密钥、<see cref="HttpClient"/>构建对话
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="apiKey"></param>
    /// <param name="client"></param>
    public DsChat(string prompt, string apiKey, HttpClient client)
    {
        Prompt = prompt;
        this.apiKey = apiKey;
        this.client = client;
        messages.Add(new RequestJson.Message(Prompt, DsRoleType.system));
        this.client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.apiKey);
    }
    /// <summary>
    /// 构建对话
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="apiKey">访问密钥</param>
    public DsChat(string prompt, string apiKey) : this(prompt, apiKey, new HttpClient()) { }
    /// <summary>
    /// 使用默认提示词 "回答" 构建对话
    /// </summary>
    /// <param name="apiKey">访问密钥</param>
    public DsChat(string apiKey) : this("回答", apiKey) { }
    /// <summary>
    /// 发送Post 请求，并返回请求体
    /// </summary>
    /// <param name="stream"> 是否是流式对话 </param>
    /// <param name="functionCall"> 是否传递方法调用参数 </param>
    /// <returns></returns>
    protected virtual HttpRequestMessage GetRequestMessage(
        bool stream,
        bool functionCall = true)
    {
        var reJson = new RequestJson(messages);
        if (functionCall) {
            reJson.Tools = FunctionCallExecute.DsToolsString; //Tools
            reJson.Tool_Choice = "auto";
            reJson.MaxTokens = MaxTokens;
        } else {
            reJson.Tools = null;
            reJson.Tool_Choice = "none";
        }
        reJson.Stream = stream;
        var jsonText = JsonSerializer.Serialize(reJson);
        var reContent = new StringContent(jsonText, Encoding.UTF8, "application/json");
        var reMessage = new HttpRequestMessage(HttpMethod.Post, baseUri)
        {
            Content = reContent
        };
        return reMessage;
    }
    /// <summary>
    /// 流对话处理器
    /// </summary>
    /// <param name="response">http请求的返回体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    protected static async IAsyncEnumerable<(string oneChar, StreamGetJson? json)> StreamChatHandel(HttpResponseMessage response, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken == null) {
            cancellationToken = CancellationToken.None;
        }
        using var streamValue = await response.Content.ReadAsStreamAsync();
        using var sr = new StreamReader(streamValue);
        var dep = sr.ReadLine();
        //Console.WriteLine(dep);
        while (dep != null && !cancellationToken.Value.IsCancellationRequested) {
            StreamGetJson? reqJson = null;
            if (string.IsNullOrEmpty(dep) || dep.Length < "data: ".Length) {
                yield return ("", null);
            } else {
                dep = dep[("data: ".Length - 1)..];
                reqJson = JsonSerializer.Deserialize<StreamGetJson>(dep);
                var value = reqJson?.Choices![0].Delta!.Content ?? "";
                yield return (value, reqJson);
            }
            dep = await sr.ReadLineAsync();
            if (dep != null && dep.Contains("[DONE]")) {
                yield return ("", reqJson);
                yield break;
            }
        }
    }
    /// <summary>
    /// 成功返回True 失败返回False
    /// </summary>
    protected static bool IfErrTry<T>(Func<T> func, out T? value) where T : class
    {
        value = null;
        try {
            value = func.Invoke();
            return false;
        } catch {
            return true;
        }
    }
    private bool isDispose = false;
    public void Dispose()
    {
        if (isDispose == false) {
            client.Dispose();
            isDispose = true;
            GC.SuppressFinalize(this);
        }
    }
}