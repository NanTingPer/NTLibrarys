using System.Reflection;
using System.Reflection.Emit;

namespace NTLibray.Reflect.ConsoleRun;
internal class NTLibrayReflectEmitTest
{
    public static void Run()
    {
        AssemblyBuilder? asmBuilder = DynamicTypeHelper.CreateDynamicAssembly();
        var moduleBuilder = asmBuilder.CreateDynamicModule();
        var typeBuilder = moduleBuilder.DefineType(
            "AbsImplType",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(AbsNoImplType));
        var methodBuilder = typeBuilder.DefineMethod("noimplmethod", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
        var generator = methodBuilder.GetILGenerator();

        generator
                .EmitStaticMethod(AbsNoImplType.PrintHello)
                .Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, typeof(AbsNoImplType).GetMethod(nameof(AbsNoImplType.RunImplMethod), BindingFlags.Instance | BindingFlags.Public)!);

        var type = typeBuilder.CreateType();
        var o = (AbsNoImplType)Activator.CreateInstance(type)!;
        o.RunImplMethod();
    }
}

public abstract class AbsNoImplType
{
    public abstract void RunImplMethod();
    public static void PrintHello()
    {
        System.Console.Write("Hello World");
    }
}