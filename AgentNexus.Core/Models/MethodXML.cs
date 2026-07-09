using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace AgentNexus.Core.Models;

/// <summary>
/// 用于解析方法的XML注释
/// </summary>
public class MethodXML
{
    /// <summary>
    /// 此类型处理的文档
    /// </summary>
    private readonly XDocument document;
    /// <summary>
    /// 加载给定路径的xml文档
    /// </summary>
    /// <param name="xmlDocumentPath">文档路径</param>
    /// <exception cref="Exception">路径不存在</exception>
    public MethodXML(string xmlDocumentPath)
    {
        if (!File.Exists(xmlDocumentPath)) {
            throw new Exception($"给定的文件路径{xmlDocumentPath}不存在, 请检查是否启用文档生成");
        }
        document = XDocument.Load(xmlDocumentPath);
    }

    private const string member = "member";
    private const string param = "param";
    private const string summary = "summary";

    /// <summary>
    /// 获取给定<see cref="MethodInfo"/>的注释文档
    /// </summary>
    public MethodXMLNote GetMethodXMLNotes(MethodInfo methodInfo)
    {
        var methodXmlNode = new MethodXMLNote(methodInfo.Name)
        {
            Summary = "无说明，请根据方法名称猜测"
        };

        var methodInfoFullName = methodInfo.ReflectedType!.FullName + "." + methodInfo.Name;
        var xmlMembers = document.Descendants(member); //获取全部成员

        //获取此方法的文档注释信息，用于生成工具文档
        var methodInfoXmlElement = xmlMembers.FirstOrDefault(xelement => {
            if (xelement.LastAttribute == null) //LastAttribut 是文档标签的实际内容
                return false;
            var methodXmlFullName = xelement.LastAttribute.Value;
            return methodXmlFullName.Contains($"M:{methodInfoFullName}");
        });
        if (methodInfoXmlElement == null)
            return methodXmlNode;

        var methodSummary = methodInfoXmlElement.Element(summary);
        var methodParams = methodInfoXmlElement.Elements(param).ToList();

        if (methodSummary != null) {
            methodXmlNode.Summary = methodSummary.Value.Trim();
        }

        int paramCount = 0;
        _ = methodParams.Select(paramXML => {
            var name = paramXML.FirstAttribute?.Value ?? "param";
            var summary = paramXML.Value;
            //var index = methodParams.FindIndex(param => param.Name == name);
            var type = methodInfo.GetParameters()[paramCount].ParameterType;
            paramCount++;
            var xmlParam = new MethodXMLParam(name, type.Name)
            {
                Summary = summary.Trim(),
                TypeType = type
            };
            methodXmlNode.Params.Add(xmlParam);
            return "null";
        }).ToArray();

        return methodXmlNode;
    }
}

/// <summary>
/// 方法的文档注释
/// </summary>
public class MethodXMLNote(string name)
{
    /// <summary>
    /// 方法名称
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// 方法说明
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 方法参数列表
    /// </summary>
    public List<MethodXMLParam> Params { get; set; } = [];
}

/// <summary>
/// 方法文档注释参数
/// </summary>
public class MethodXMLParam(string name, string type)
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// 参数类型
    /// </summary>
    public string Type { get; set; } = type;

    /// <summary>
    /// 参数说明
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 类型
    /// </summary>
    [JsonIgnore]
    public Type? TypeType { get; set; }
}