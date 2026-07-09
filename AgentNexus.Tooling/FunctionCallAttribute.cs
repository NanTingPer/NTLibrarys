using System;

namespace AgentNexus.Tooling;

/// <summary>
/// 将方法标注为AI的方法调用，类型需要实现<see cref="IAutoRegionFunction"/>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FunctionCallAttribute : Attribute { }