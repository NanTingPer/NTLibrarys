using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AgentNexus.Core.Models;

/// <summary>
/// Ds方法调用的Json
/// </summary>
public class DsFunctionToolsSchema
{
    /// <summary>
    /// 使用<see cref="MethodXMLNote"/>创建函数说明
    /// </summary>
    /// <param name="methodXMLnode">方法的文档说明</param>
    public DsFunctionToolsSchema(MethodXMLNote methodXMLnode)
    {
        Function = new DsFunctionSchema(methodXMLnode);
    }

    /// <summary>
    /// 自行创建<see cref="DsFunctionSchema"/>
    /// </summary>
    /// <param name="function">函数说明</param>
    public DsFunctionToolsSchema(DsFunctionSchema function)
    {
        Function = function;
    }

    /// <summary>
    /// 工具类型，目前只支持函数调用
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// 函数
    /// </summary>
    [JsonPropertyName("function")]
    public DsFunctionSchema Function { get; set; }
}

/// <summary>
/// Ds方法调用的单个方法
/// </summary>
public class DsFunctionSchema
{
    /// <summary>
    /// 自己传递<see cref="DsFunctionParametersSchema"/>
    /// </summary>
    /// <param name="functionParameter">方法参数描述</param>
    /// <param name="name">方法名称</param>
    public DsFunctionSchema(DsFunctionParametersSchema functionParameter, string name)
    {
        Parameters = functionParameter;
        Name = name;
    }

    /// <summary>
    /// 利用<see cref="MethodXMLNote"/>生成<see cref="DsFunctionSchema"/>
    /// </summary>
    /// <param name="methodXMLNote"></param>
    public DsFunctionSchema(MethodXMLNote methodXMLNote)
    {
        Name = methodXMLNote.Name;
        Description = methodXMLNote.Summary;
        Parameters = DsFunctionParametersSchema.Parser(methodXMLNote);
    }

    /// <summary>
    /// 此方法的描述
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 此方法的名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 此方法的参数描述
    /// </summary>
    [JsonPropertyName("parameters")]
    public DsFunctionParametersSchema Parameters { get; set; }
}

/// <summary>
/// Ds方法调用中的方法参数
/// </summary>
public class DsFunctionParametersSchema
{
    /// <summary>
    /// 参数类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    /// <summary>
    /// 参数列表 type, description
    /// </summary>
    [JsonPropertyName("properties")]
    public JsonObject? Properties { get; set; }

    /// <summary>
    /// 必须的给定的参数列表
    /// </summary>
    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }

    /// <summary>
    /// 将<see cref="MethodXMLNote.Params"/> 解析为 <see cref="DsFunctionParametersSchema"/>
    /// </summary>
    /// <param name="methodNote"></param>
    /// <returns></returns>
    public static DsFunctionParametersSchema Parser(MethodXMLNote methodNote)
    {
        //获取参数列表 全部标记为必须
        var functionParameter = new DsFunctionParametersSchema()
        {
            Required = [],
        };
        methodNote.Params.ForEach(p => {
            functionParameter.Required.Add(p.Name);
        });

        //解析参数列表写入properties
        var propertiesObject = new JsonObject();
        foreach (MethodXMLParam methodXMLParam in methodNote.Params) {
            var paramNode = new JsonObject()
            {
                {"type", GetJsonType(methodXMLParam.TypeType!) },
                {"description", methodXMLParam.Summary }
            };
            propertiesObject.Add(methodXMLParam.Name, paramNode);
        }
        functionParameter.Properties = propertiesObject;
        return functionParameter;
    }

    /// <summary>
    /// 获取给定类型指定的Json类型
    /// </summary>
    /// <param name="type"> 给定类型 </param>
    /// <returns></returns>
    public static string GetJsonType(Type type)
    {
        return type switch
        {
            // null 或 void 视为 "null"
            null => "null",
            _ when type == typeof(void) => "null",

            // boolean
            _ when type == typeof(bool) || type == typeof(bool?) => "boolean",

            // integer 类型
            _ when type == typeof(sbyte)
                || type == typeof(sbyte?)
                || type == typeof(byte)
                || type == typeof(byte?)
                || type == typeof(short)
                || type == typeof(short?)
                || type == typeof(ushort)
                || type == typeof(ushort?)
                || type == typeof(int)
                || type == typeof(int?)
                || type == typeof(uint)
                || type == typeof(uint?)
                || type == typeof(long)
                || type == typeof(long?)
                || type == typeof(ulong)
                || type == typeof(ulong?) => "integer",

            // number（浮点/decimal）
            _ when type == typeof(float)
                || type == typeof(float?)
                || type == typeof(double)
                || type == typeof(double?)
                || type == typeof(decimal)
                || type == typeof(decimal?) => "number",

            // string
            _ when type == typeof(string) || type == typeof(char) || type == typeof(char?) => "string",

            // array / list / enumerable（非字符串）
            _ when type.IsArray
                || (type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(List<>)
                    || type.GetGenericTypeDefinition() == typeof(IList<>)
                    || type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    || type.GetGenericTypeDefinition() == typeof(ICollection<>))) => "array",

            // object（引用类型，排除已处理的 string 和数组等）
            _ when type.IsClass || type.IsInterface => "object",
            _ when type.IsValueType => "string",
            _ => "object"
        };
    }
}