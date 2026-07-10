using AgentNexus.Tooling;

namespace AgentNexus.Examples;

/// <summary>
/// </summary>
public class Functions : IAutoRegionFunction
{
    /// <summary>
    /// 获取给定城市的天气
    /// </summary>
    /// <param name="city">城市名称</param>
    [FunctionCall]
    public static string GetWeather(string city)
    {
        return $"{city}今天温度38摄氏度.";
    }
}
