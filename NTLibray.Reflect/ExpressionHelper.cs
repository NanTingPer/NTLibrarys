using System.Linq.Expressions;
using System.Reflection;

namespace NTLibray.Reflect;

/// <summary>
/// 表达树构建并缓存辅助方法
/// </summary>
public static class ExpressionHelper
{
    private readonly static StaticCache _staticCache = new ();
    private readonly static InstanceCache _instanceCache = new();

    public static TValue GetValue<TValue>(this PropertyInfo propertyInfo, object? obj = null, bool cache = true)
    {
        var get = propertyInfo.GetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        for (; get.IsStatic; ) {
            if (cache)
                return (TValue)_staticCache.GetOrCreate(propertyInfo)();
            else
                return (TValue)propertyInfo.CreateStaticGetValueExpression().Compile()();
        }

        for (; !get.IsStatic; ) {
            ArgumentNullException.ThrowIfNull(obj, "获取实例属性值，但是没传入目标对象");
            if (cache)
                return (TValue)_instanceCache.GetOrCreate(propertyInfo)(obj!);
            else
                return (TValue)propertyInfo.CreateInstanceGetValueExpression().Compile()(obj);

        }
        throw new Exception("不应该发生这种情况");
    }

    /// <summary>
    /// 弱类型，静态属性访问表达式
    /// </summary>
    public static Expression<Func<object>> CreateStaticGetValueExpression(this PropertyInfo property)
    {
        var get = property.GetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        if (!get.IsStatic) {
            throw new Exception($"调用{nameof(CreateStaticGetValueExpression)}却使用了实例属性 {property.DeclaringType?.FullName ?? ""} {property.Name}");
        }

        var getExp = Expression.Property(null, property);
        var covToObj = Expression.Convert(getExp, typeof(object));
        return Expression.Lambda<Func<object>>(covToObj);
    }

    /// <summary>
    /// 弱类型，实例属性访问表达式
    /// </summary>
    public static Expression<Func<object, object>> CreateInstanceGetValueExpression(this PropertyInfo property)
    {
        var get = property.GetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        if (get.IsStatic) {
            throw new Exception($"调用{nameof(CreateInstanceGetValueExpression)}却使用了静态属性 {property.DeclaringType?.FullName ?? ""} {property.Name}");
        }
        var p = Expression.Parameter(typeof(object));
        var pToDtype = Expression.Convert(p, property.DeclaringType!); // obj to dtype
        var getExp = Expression.Property(pToDtype, property);
        var valueToObj = Expression.Convert(getExp, typeof(object));
        return Expression.Lambda<Func<object, object>>(valueToObj, p);
    }


    /// <summary>
    /// 强类型表达式树，既可以是静态属性，也可以是实例属性
    /// </summary>
    public static Expression<TDelegate> CreateGetValueExpression<TDelegate>(this PropertyInfo property)
        where TDelegate : Delegate
    {
        var get = property.GetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        
        // 非静态
        for (; !get.IsStatic;) {
            var a = Expression.Parameter(property.DeclaringType!);
            var getExp = Expression.Property(a, property);
            return Expression.Lambda<TDelegate>(getExp, a);
        }

        for (; get.IsStatic;) {
            var getExp = Expression.Property(null, property);
            return Expression.Lambda<TDelegate>(getExp);
        }

        throw new Exception("不应该发生这个错误");
    }
}