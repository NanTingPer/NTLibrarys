using System.Text;
using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models;

/// <summary>
/// AI返回的内容
/// </summary>
public record class ReturnValue
{
    /// <summary>
    /// 此类型的JsonSchema
    /// </summary>
    public const string ReturnJsonSchema = """
        {
          "$schema": "http://json-schema.org/draft-07/schema#",
          "type": "object",
          "properties": {
            "Value": {
              "type": "string",
              "default": "",
              "description": "当为流式返回时，这个是返回的内容；当为非流式返回时，这个是返回的内容。"
            },
            "AllValue": {
              "type": "string",
              "default": "",
              "description": "当为流式返回时，这个是全部内容；当为非流式返回时，这个是返回的内容。"
            },
            "IsStream": {
              "type": "boolean",
              "default": false,
              "description": "当前请求是否是流式请求。"
            },
            "IsFunctionCall": {
              "type": "boolean",
              "default": false,
              "description": "当前请求的返回是否是方法调用。"
            },
            "IsEnd": {
              "type": "boolean",
              "default": false,
              "description": "当请求模式为流式时，这次返回是否是结束。"
            },
            "Function": {
              "anyOf": [
                {
                  "type": "object",
                  "properties": {
                    "FunctionName": {
                      "type": "string",
                      "default": "",
                      "description": "被调用的函数名称。"
                    },
                    "FunctionParmas": {
                      "type": "array",
                      "items": {
                        "type": "string"
                      },
                      "default": [],
                      "description": "传递给函数的参数列表（字符串数组）。注意：字段名拼写为 'FunctionParmas'（缺少 'e'），与代码一致。"
                    }
                  },
                  "required": ["FunctionName", "FunctionParmas"],
                  "additionalProperties": false
                },
                {
                  "type": "null"
                }
              ],
              "description": "当 IsFunctionCall 为 true 时，此字段包含具体的函数调用信息；否则为 null（序列化时将被省略）。"
            }
          },
          "required": ["Value", "AllValue", "IsStream", "IsFunctionCall", "IsEnd"],
          "additionalProperties": false
        }
        """;

    /// <summary>
    /// <see cref="Function"/>的JsonSchema
    /// </summary>
    public const string FunctionCallSchema = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "properties": {
            "Name": {
              "type": "string"
            },
            "Parmas": {
              "type": "array",
              "items": {
                "type": "string"
              }
            }
          },
          "required": ["Name", "Parmas"]
        }
        """;

    /// <summary>
    /// 当为流式返回时，这个是返回的内容
    /// <para> 当为非流式返回时，这个是返回的内容 </para>
    /// </summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// 当为流式返回时，这个是全部内容
    /// <para> 当为非流式返回时，这个是返回的内容 </para>
    /// </summary>
    public string AllValue { get; set; } = string.Empty;
    /// <summary>
    /// 当前请求是否是流式请求
    /// </summary>
    public bool IsStream { get; set; } = false;
    /// <summary>
    /// 当前请求的返回是否是方法调用
    /// </summary>
    public bool IsFunctionCall { get; set; } = false;
    /// <summary>
    /// 当请求模式为流式时，这次返回是否是结束
    /// </summary>
    public bool IsEnd { get; set; } = false;

    /// <summary>
    /// 要被调用的方法信息
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Function? Function { get; set; }
}

/// <summary>
/// <see cref="ReturnValue"/>的Function
/// </summary>
public class Function
{
    /// <summary>
    /// 方法名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 方法参数列表
    /// </summary>
    public string[] Parmas { get; set; } = [];

    /// <summary>
    /// 重写ToString方法
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < Parmas.Length; i++) {
            stringBuilder.Append($"{Parmas[i]}, ");
        }
        return $"{Name}: {stringBuilder.ToString()}";
    }
}
