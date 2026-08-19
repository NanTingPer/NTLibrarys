using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace NTLibray.Reflect;

public static class DynamicTypeHelper
{
    /// <summary>
    /// 创建动态程序集，如果<paramref name="name"/>为空，则默认使用调用方法所属的<see cref="AssemblyName"/>定义
    /// </summary>
    public static AssemblyBuilder CreateDynamicAssembly(
        AssemblyName? name = null,
        AssemblyBuilderAccess access = AssemblyBuilderAccess.RunAndCollect,
        IEnumerable<CustomAttributeBuilder>? attributBuilder = null
    )
    {
        if (name == null) {
            var stack = new StackTrace();
            var stackFrame = stack.GetFrame(1);
            name = stackFrame!.GetMethod()!.DeclaringType!.Assembly.GetName()!; // 声明类型
        }
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(name, access, attributBuilder);
        return asmBuilder;
    }

    /// <summary>
    /// 定义模块，如果模块名称为空，则使用调用此方法所在的模块名称
    /// </summary>
    public static ModuleBuilder CreateDynamicModule(this AssemblyBuilder asmBuilder, string? moduleName = null, int stackDepth = 1)
    {
        if (moduleName == null) {
            var stack = new StackTrace();
            var stackFrame = stack.GetFrame(stackDepth);
            moduleName = stackFrame!.GetMethod()!.DeclaringType!.Assembly.ManifestModule.Name!; // 声明类型
        }
        var module = asmBuilder.DefineDynamicModule(moduleName);
        return module;
    }

    /// <summary>
    /// 此方法只是为了简化调用，只能传入静态委托
    /// </summary>
    /// <param name="ilg"></param>
    /// <param name="o"></param>
    /// <returns></returns>
    public static ILGenerator EmitStaticMethod<TDelegate>(this ILGenerator ilg, TDelegate o/*, Action<ILGenerator>? emitParms = null*/)
        where TDelegate : Delegate
    {
        if (o.Target == null) {
            ilg.Emit(OpCodes.Call, o.Method);
        } else {
            // 如果你的委托捕获了实例对象的属性 / 字段那么需要压入this <br />
            // 如果你的委托捕获了方法体中的局部成员 那么需要压入this
            //ilg.Emit(OpCodes.Ldarg_0);
            //emitParms?.Invoke(ilg);
            //ilg.Emit(OpCodes.Callvirt, o.Method);
            throw new InvalidOperationException("只支持静态委托！");
        }
        return ilg;
    }
}
