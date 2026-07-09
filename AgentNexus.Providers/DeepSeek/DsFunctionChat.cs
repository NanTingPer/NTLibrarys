using AgentNexus.Core.Models;
using AgentNexus.Core.Models.Return;
using AgentNexus.Tooling;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using static AgentNexus.Core.Models.RequestJson;
using static AgentNexus.Tooling.FunctionCallExecute;

namespace AgentNexus.Providers.DeepSeek;
/// <summary>
/// 拥有独立方法表的对话
/// </summary>
public abstract class DsFunctionChat<TSelf> : DsChatStream
    where TSelf : DsFunctionChat<TSelf>
{
    private static bool _loactionException = false;
    private static MethodXML? _assemblyXML;
    private static List<MethodXMLNote> _tools = [];
    private static List<DsFunctionToolsSchema> _dsToolsString => _tools.Select(x => new DsFunctionToolsSchema(x)).ToList();
    /// <summary>
    /// 全部工具方法
    /// </summary>
    public readonly static Dictionary<string, FunctionCallDelegate> FunctionTools = [];
    /// <summary>
    /// 自动初始化<see cref="MethodXML"/>,会尝试自动获取xml文档
    /// </summary>
    /// <exception cref="Exception"></exception>
    static DsFunctionChat()
    {
        var type = typeof(TSelf);
        var assemblyName = type.Namespace?.Split('.')[0];
        if (assemblyName == null) {
            throw new Exception($"{type.FullName}在初始化时发生错误，其不属于任何程序集");
        }
        //获取程序集所在目录
        var assemblyLocation = type.Assembly.Location;
        if (assemblyLocation == string.Empty) {
            _loactionException = true;
            return;
        }
        //获取程序集的XDoc
        var locationPath = Path.GetDirectoryName(assemblyLocation);
        var assemblyXmlDocmentFile = Path.Combine(locationPath!, assemblyName + ".xml");
        try {
            _assemblyXML = new MethodXML(assemblyXmlDocmentFile);
        } catch {
            _loactionException = true;
            return;
        }

        InitializationDictionary();
    }
    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="prompt"> 提示词 </param>
    /// <param name="apiKey"> 密钥 </param>
    /// <param name="client"> 客户端 </param>
    /// <param name="xmlPath"> xml文件路径 </param>
    /// <param name="options"> 配置项 </param>
    public DsFunctionChat(
        string prompt,
        string apiKey,
        HttpClient client,
        string? xmlPath = null,
        ChatOptions? options = null)
        : base(prompt, apiKey, client, options)
    {
        if (xmlPath != null && _loactionException == true) {
            _assemblyXML ??= new MethodXML(xmlPath);
            InitializationDictionary();
        }

        if (_assemblyXML == null) {
            throw new Exception($"{typeof(TSelf).FullName}所属程序集的XML文档没初始化? 没办法正常获取函数信息。");
        }
    }
    /// <summary>
    /// 构建请求消息
    /// </summary>
    /// <param name="stream">是否是流式对话</param>
    /// <param name="functionCall">是否传递方法表</param>
    /// <returns></returns>
    protected override HttpRequestMessage GetRequestMessage(bool stream, bool functionCall = true)
    {
        var reJson = new RequestJson(messages);
        if (functionCall) {
            reJson.Tools = _dsToolsString; //Tools
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
    /// 初始化工具字典，只会获取实现类型的工具
    /// </summary>
    private static void InitializationDictionary()
    {
        var type = typeof(TSelf);
        var functionMethods = type.GetMethods().Where(methodinfo => {
            // 异步方法状态机
            var compilerGenerated = methodinfo.IsDefined(typeof(CompilerGeneratedAttribute), false);
            if (compilerGenerated) {
                return false;
            }

            var functionCallAttribute = methodinfo.GetCustomAttribute<FunctionCallAttribute>();
            if (functionCallAttribute == null || !methodinfo.IsStatic) {
                return false;
            }

            return true;
        });

        if (_assemblyXML == null)
            return;
        _tools = functionMethods.Select(_assemblyXML.GetMethodXMLNotes).ToList();

        //如果使用CreateDelegate，那么他的第一个参数是委托类型
        foreach (var @delegate in functionMethods.Select(m => (m, CreateAdapter(m)))) {
            FunctionTools[@delegate.m.Name] = @delegate.Item2;
        }
    }

    /// <summary>
    /// 使用自身的方法表进行方法调用
    /// </summary>
    /// <param name="toolCall"> 方法参数等 </param>
    /// <returns></returns>
    protected override async IAsyncEnumerable<ReturnValue> FunctionCall(ToolCall toolCall)
    {
        var value = await DynamicInvoke(FunctionTools, toolCall!);
        var toolValue = value?.ToString() ?? "已经调用完成";
        messages.Add(new Message(toolCall!));
        // Tool的消息，要求传入Toolid
        await foreach (var item in Chat(toolValue, toolCall!.Id, DsRoleType.tool)) {
            yield return item;
        }
    }
}