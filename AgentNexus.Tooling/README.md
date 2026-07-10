# AgentNexus.Tooling

AI 工具调用基础设施库，提供函数注册、调用和文档生成能力。

## 功能特性

- 自动扫描并注册标记的工具方法
- 支持同步和异步方法
- 基于 XML 文档注释生成工具描述
- 类型安全的方法调用
- 全局方法表管理

## 使用方式

### 1. 定义工具类

实现 `IAutoRegionFunction` 接口，并使用 `[FunctionCall]` 标记工具方法：

```csharp
public class WeatherFunctions : IAutoRegionFunction
{
    /// <summary>
    /// 获取给定城市的天气
    /// </summary>
    /// <param name="city">城市名称</param>
    [FunctionCall]
    public static string GetWeather(string city)
    {
        return $"{city}今天温度38摄氏度";
    }
}
```

### 2. 初始化全局方法表

> 需要将目标文档置于程序执行目录

在应用启动时调用：

```csharp
FunctionCallExecute.InitFunctionCalls();
```

### 3. 与 Providers 集成

本库提供的工具调用能力可被 `AgentNexus.Providers` 中的对话类集成使用。通过 `FunctionCallExecute.DsToolsString` 可获取符合 DeepSeek API 格式的工具列表。

## 核心组件

### FunctionCallAttribute

标记方法为 AI 可调用的工具。

### FunctionCallExecute

执行函数调用的核心类，提供：

- `InitFunctionCalls()`: 初始化全局方法表
- `DynamicInvoke()`: 动态调用工具方法
- `Tools`: 获取所有已注册的工具列表
- `ToolsString`: 获取工具列表的 JSON 字符串
- `DsToolsString`: 获取符合 DeepSeek API 格式的工具列表

### IAutoRegionFunction

标记接口，实现此接口的类中的 `[FunctionCall]` 方法会被自动扫描注册。

## XML 文档支持

工具描述和参数说明通过 XML 文档注释生成，确保项目启用文档生成：

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## 依赖

- `AgentNexus.Core`
- `System.Text.Json`
- `.NET Standard 2.1`