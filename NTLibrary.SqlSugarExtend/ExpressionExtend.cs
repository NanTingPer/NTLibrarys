using NTLibrary.SqlSugarExtend.ExpressionReplacers;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NTLibrary.SqlSugarExtend;

public static class ExpressionExtend
{
    //public static Expression<Func<object, object>> SelectObjectExpression<T>(params Expression<Func<T, object>>[] overrProp)
    //{
    //    (NewExpression NewExpression, List<MemberBinding> NewBinding, ParameterExpression[] Parameters) v = ReadParametersUnionToType(overrProp);

    //    Expression.Lambda<Func<object, object>>(v.NewExpression, )
    //}

    public static Expression<TDelegate> SelectAllExpression<TDelegate>(params Expression<TDelegate>[] overrProp)
        where TDelegate : Delegate
    {
        var v = ReadParametersUnionToType(overrProp);
        return Expression.Lambda<TDelegate>(Expression.MemberInit(v.NewExpression, v.NewBinding), v.Parameters);
    }

    /// <summary>
    /// 读取给定委托中的参数，并将参数类型的全部熟悉合并成一个新的动态类型，返回能够构建其New表达式的材料。 <br />
    /// 为什么不直接返回编译树？因为返回材料可以方便的替换MemberInitExpression中的参数属性访问。
    /// <code>
    /// var stuff = ReadParametersUnionToType{Student, Teacher, object}();
    /// var newExpression = Expression.Lambda{Func{Student, Teacher, object}}(stuff.MemberInitExpression, stuff.Parameters);
    /// </code>
    /// </summary>
    /// <returns></returns>
    private static MemberInitExpressionStuff ReadParametersUnionToType<TDelegate>(params Expression<TDelegate>[] overrProp)
        where TDelegate : Delegate
    {
        var method = typeof(TDelegate).GetMethod("Invoke")!;
        var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var paramNames = method.GetParameters().Select(p => p.Name).ToArray(); // t{i}
        var parameterList = paramTypes.Select((t, i) => Expression.Parameter(t, $"t{i}")).ToArray();
        var replace = new ParameterReplacers([.. parameterList]);
        var comparer = new OverridePropertyNameComparer();
        var finallyPropertyInfos = paramTypes.CreateOverridePropertys(comparer);
        var exrteParString = new StringBuilder();
        for (int i = 0; i < overrProp.Length; i++) {
            var useExpression = overrProp[i].Body;
            if (useExpression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
                useExpression = unary.Operand;
            // 非new不管 () => new {}
            if (useExpression is not NewExpression @new || @new.Members == null)
                continue;

            // 要进行参数覆盖，是因为 overrProp传入多个表达式，每个表达式的形式不同。如
            // a => new {}
            // b => new {}
            // 要是这样传进来，那么参数就同步，构建时候只声明了一个参数，那么就炸了
            foreach (var op in @new.CreateOverridePropertys(replace)) {
                finallyPropertyInfos.Remove(op);
                finallyPropertyInfos.Add(op);
                exrteParString.Append(op.Name).Append('_').Append(op.PropertyType.GetHashCode()).Append('_');
            }
        }
        var dtypename = string.Join('_', paramTypes.Select(t => t.FullName!.Replace('.', '_'))) + "Dynamic_" + exrteParString.ToString();
        var dynamicType = AssemblyBuilderCache.GetOrCreate(dtypename, [.. finallyPropertyInfos]);
        var newExp = Expression.New(dynamicType);
        List<MemberBinding> bindings = new List<MemberBinding>();
        foreach (var srcProp in finallyPropertyInfos) {
            // 获取动态类型的属性
            PropertyInfo destProp = dynamicType.GetProperty(srcProp.Name, BindingFlags.Public | BindingFlags.Instance)!;
            Expression member;
            if (srcProp.SourceExpression == null) {
                var parm = parameterList.FirstOrDefault(p => p.Type == srcProp.DeclaringType);
                member = Expression.Property(parm, srcProp);
            } else {
                // 从Obj取具体值
                member = srcProp.SourceExpression;
            }
            bindings.Add(Expression.Bind(destProp, member));
        }
        
        // new DynamicType { Prop1 = a.Prop1 , Prop2 = a.Prop2 ... }
        // MemberInitExpression initExp = Expression.MemberInit(newExp, bindings);
        return new MemberInitExpressionStuff(newExp, bindings, parameterList);
        //var rootExp = Expression.Lambda<TDelegate>(initExp, parameterList);
        //return rootExp;
    }

    public static Expression<Func<T1, T2, object>> SelectAllExpression<T1, T2>(params Expression<Func<T1, T2, object>>[] overrProp)
        => SelectAllExpression<Func<T1, T2, object>>(overrProp);

    public static Expression<Func<T, object>> SelectAllExpression<T>(params Expression<Func<T, object>>[] overrProp)
        => SelectAllExpression<Func<T, object>>(overrProp);

    public static HashSet<OverrideProperty> CreateOverridePropertys(this Type type, OverridePropertyNameComparer? comparer = null)
        => new HashSet<OverrideProperty>(type.GetProperties().Select(p => new OverrideProperty(p, null)), comparer ?? new OverridePropertyNameComparer());
    private static HashSet<OverrideProperty> CreateOverridePropertys(this Type[] types, OverridePropertyNameComparer? comparer = null)
    {
        var set = new HashSet<OverrideProperty>(comparer ?? new OverridePropertyNameComparer());
        for (int i = 0; i < types.Length; i++) {
            var t = types[i];
            var props = t.GetProperties();
            for (int j = 0; j < props.Length; j++) {
                var prop = props[j];
                set.Remove(prop);
                set.Add(prop);
            }
        }
        return set;
    }
    
    /// <summary>
    /// 遍历new表达式的参数列表和成员列表，并将其转换为<see cref="OverrideProperty"/>，如传入了 <paramref name="replacer"/> 那么对象参数还会被进行替换
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="replacer"></param>
    /// <returns></returns>
    private static IEnumerable<OverrideProperty> CreateOverridePropertys(this NewExpression expression, ExpressionVisitor? replacer = null)
    {
        var members = expression.Members!;
        for (int j = 0; j < members.Count; j++) {
            var member = members[j] as PropertyInfo;
            var arg = expression.Arguments[j];
            var srcExp = replacer?.Visit(arg) ?? arg;
            var op = new OverrideProperty(member!, srcExp);
            yield return op;
        }
        yield break;
    }

    public class OverrideProperty(PropertyInfo propertyInfo, Expression? expression = null)
    {
        public Type DeclaringType { get; set; } = propertyInfo.DeclaringType!;
        public PropertyInfo PropertyInfo { get; set; } = propertyInfo;
        public string Name => PropertyInfo.Name;
        public Type PropertyType => PropertyInfo.PropertyType;
        public Expression? SourceExpression { get; } = expression;
        public static implicit operator PropertyInfo(OverrideProperty p) => p.PropertyInfo;
        public static implicit operator OverrideProperty(PropertyInfo p) => new OverrideProperty(p);
    }

    public class OverridePropertyNameComparer : IEqualityComparer<OverrideProperty>
    {
        public bool Equals(OverrideProperty? x, OverrideProperty? y)
        {
            if (x == null || y == null) return false;
            return x.PropertyInfo.Name == y.PropertyInfo.Name;
        }

        public int GetHashCode([DisallowNull] OverrideProperty obj)
        {
            return obj.PropertyInfo.Name.GetHashCode();
        }
    }
}

internal record struct MemberInitExpressionStuff(NewExpression NewExpression, List<MemberBinding> NewBinding, ParameterExpression[] Parameters)
{
    public readonly MemberInitExpression MemberInitExpression => Expression.MemberInit(NewExpression, NewBinding);
}