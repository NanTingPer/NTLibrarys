using AgentNexus.Core.Models;
using AgentNexus.Providers.DeepSeek;
using AgentNexus.Tooling;

namespace AgentNexus.Examples;

/// <summary>
/// </summary>
public class DsFunctionChatExample(string prompt, string apiKey, HttpClient client, string? xmlPath = null, ChatOptions? options = null) : 
    DsFunctionChat<DsFunctionChatExample>(prompt, apiKey, client, xmlPath, options)
{
    /// <summary>
    /// 获取给定城市的天气信息
    /// </summary>
    /// <param name="city">城市</param>
    /// <param name="level">级别(1为当前，2为明天)</param>
    /// <returns></returns>
    [FunctionCall]
    public static string GetWeather(string city, int level)
    {
        return level switch
        {
            1 => $"{city}当前天气为38摄氏度",
            2 => $"{city}明天天气为39摄氏度",
            _ => "无效参数level",
        };
    }

    /// <summary>
    /// </summary>
    public static async Task Run()
    {
        var chatClient = new DsFunctionChatExample(
            prompt: "调用工具，回答问题",
            apiKey: Environment.GetEnvironmentVariable("DEEPSEEKKEY", EnvironmentVariableTarget.User)!,
            client: new HttpClient()
        );

        await foreach (var ret in chatClient.Chat("福建泉州明天的天气是什么样的")) {
            Console.Write(ret.Value);
        }
    }
}
