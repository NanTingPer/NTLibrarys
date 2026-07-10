using AgentNexus.Providers.DeepSeek;
using AgentNexus.Tooling;

namespace AgentNexus.Examples;

/// <summary>
/// </summary>
public static class DsChatStreamToolCallExample
{
    /// <summary>
    /// </summary>
    public static async Task Run()
    {
        FunctionCallExecute.InitFunctionCalls(); // 初始化全局方法表

        var chatOptions = new AgentNexus.Core.Models.ChatOptions()
        {
            EnableContext = true,
            EnableFunctionCall = true
        };

        var chatClient = new DsChatStream(
            prompt: "调用工具，回答问题",
            apiKey: Environment.GetEnvironmentVariable("DEEPSEEKKEY", EnvironmentVariableTarget.User)!,
            options: chatOptions
        );


        await foreach (var retvalue in chatClient.Chat("福建福州今天天气怎么样")) {
            Console.Write(retvalue.Value);
        }
        ;
    }
}
