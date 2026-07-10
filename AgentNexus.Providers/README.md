# AgentNexus.Providers

AI 对话服务提供者库，目前支持 DeepSeek API。

## 功能特性

- 流式对话支持
- 非流式对话支持
- 工具调用支持
- Agent 工作流支持（多对话串联）
- 上下文管理

## 核心类

### DsChatStream

流式对话实现，支持实时接收 AI 响应。

```csharp
var chatClient = new DsChatStream(
    prompt: "回答问题",
    apiKey: "your-api-key",
    options: new ChatOptions {
        EnableContext = true,
        EnableFunctionCall = true
    }
);

await foreach (var ret in chatClient.Chat("你好")) {
    Console.Write(ret.Value);
}
```

### DsChatNoStream

非流式对话实现，等待完整响应后返回。

```csharp
var chatClient = new DsChatNoStream("your-api-key");
var result = await chatClient.Chat("你好");
Console.WriteLine(result.Value);
```

### DsFunctionChat\<TSelf\>

> 需要将xml文档放在程序集目录，或者在构造函数指定路径

支持独立方法表的对话，使用泛型实现类型安全的工具调用。

```csharp
public class MyChat : DsFunctionChat<MyChat>
{
    [FunctionCall]
    public static string GetWeather(string city)
    {
        return $"{city}今天天气晴朗";
    }

    public MyChat(string prompt, string apiKey, HttpClient client)
        : base(prompt, apiKey, client) { }
}
```

### DsAgent

Agent 工作流，支持多个对话串联执行。

```csharp
var agent = new DsAgent(chat1, chat2, chat3);
agent.TokenEvent += (sender, args) => Console.Write(args.Token);
await agent.Chat("开始工作流");
```

## 配置选项

`ChatOptions` 支持以下配置：

- `EnableContext`: 是否启用上下文（默认 `true`）
- `EnableFunctionCall`: 是否启用工具调用（默认 `true`）

## 依赖

- `AgentNexus.Core`
- `AgentNexus.Tooling`
- `.NET Standard 2.1`