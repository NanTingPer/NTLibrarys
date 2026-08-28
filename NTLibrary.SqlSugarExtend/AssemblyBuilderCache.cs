using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace NTLibrary.SqlSugarExtend;

//public class MemberInitPropVisitReplacer(MemberInitExpression expression, Type targetType) : ExpressionVisitor
//{
//    protected override Expression VisitMemberInit(MemberInitExpression node)
//    {
//        Expression.MemberInit();
//    }
//}

/// <summary>
/// 其实就是一个简单的方法 也可以不用封为类型，这个方法可以根据给定的属性列表，创建一个独立的动态类型。
/// </summary>
public class AssemblyBuilderCache
{
    private static readonly ConcurrentDictionary<string, Type> _dynamicTypes = [];
    public static Type GetOrCreate(string typeName, params PropertyInfo[] propertyinfos)
    {
        return _dynamicTypes.GetOrAdd(typeName, name => {
            var mBuiler = GetBuilder();
            var typeBuiler = mBuiler.DefineType(Guid.NewGuid().ToString().Replace('-', '_'), TypeAttributes.Public | TypeAttributes.Class);
            for (int i = 0; i < propertyinfos.Length; i++) {
                var forPorp = propertyinfos[i];
                var fieldBuilder = typeBuiler.DefineField('_' + forPorp.Name, forPorp.PropertyType, FieldAttributes.Private);
                var getMethodBuiler = typeBuiler.DefineMethod("get_" + forPorp.Name, MethodAttributes.Public, returnType: forPorp.PropertyType, []);
                var ilg = getMethodBuiler.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0); // this
                ilg.Emit(OpCodes.Ldfld, fieldBuilder); // this._name
                ilg.Emit(OpCodes.Ret)
                ;

                var setMethodBuiler = typeBuiler.DefineMethod("set_" + forPorp.Name, MethodAttributes.Public, returnType: typeof(void), [forPorp.PropertyType]);
                ilg = setMethodBuiler.GetILGenerator();
                // this._field = value
                // this -> value -> stdlf
                // https://learn.microsoft.com/zh-cn/dotnet/api/system.reflection.emit.opcodes.stfld?view=net-10.0#system-reflection-emit-opcodes-stfld
                ilg.Emit(OpCodes.Ldarg_0); // this
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Stfld, fieldBuilder);
                ilg.Emit(OpCodes.Ret);

                var propBuilder = typeBuiler.DefineProperty(forPorp.Name, PropertyAttributes.None, forPorp.PropertyType, Type.EmptyTypes);
                propBuilder.SetGetMethod(getMethodBuiler);
                propBuilder.SetSetMethod(setMethodBuiler);
            }
            return typeBuiler.CreateType();
        });
    }

    private static AssemblyBuilder? _asmBuiler;
    private static ModuleBuilder? _moduleBuiler;
    private static ModuleBuilder GetBuilder()
    {
        _asmBuiler ??= AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString().Replace('-', '_')), AssemblyBuilderAccess.Run);
        _moduleBuiler ??= _asmBuiler.DefineDynamicModule("module");
        return _moduleBuiler;
    }
}