using System.Linq.Expressions;
using System.Reflection;

namespace NTLibray.Reflect.Expressions;

/// <summary>
/// 表达树构建并缓存辅助方法
/// </summary>
public static class ExpressionHelper
{
    private readonly static StaticPropertyGetCache _staticCache = new ();
    private readonly static InstancePropertyGetCache _instanceCache = new();
    /// <summary>
    /// 静态 <see cref="Func{TResult}"/>，实例 <see cref="Func{T, TResult}"/>
    /// </summary>
    public static object AccessDelegateByCache(this PropertyInfo propertyInfo)
    {
        var get = propertyInfo.GetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        if (get.IsStatic)
            return _staticCache.GetOrCreate(propertyInfo);
        else
            return _instanceCache.GetOrCreate(propertyInfo);
    }

    // /// <summary>
    // /// 利用表达式树构建委托并执行
    // /// </summary>
    // /// <param name="propertyInfo"> 要被取值的属性 </param>
    // /// <param name="obj"> 实例属性需要传递实例对象，静态属性可以不用管 </param>
    // /// <param name="cache"> 是否缓存构建的表达式树。如果你不缓存，建议使用<see cref="CreateStaticGetValueExpression(PropertyInfo)"/> 或者 <see cref="CreateInstanceGetValueExpression(PropertyInfo)"/> </param>
    // /// <returns></returns>
    /// <summary>
    /// 调用反射执行
    /// </summary>
    public static object? ExpGetValue(this PropertyInfo propertyInfo, object? obj = null)
    {
        // 反射性能在需要找表的情况下，比表达式更快。
        return propertyInfo.GetValue(obj);
        //var get = propertyInfo.GetMethod;
        //ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        //if (get.IsStatic) {
        //    if (cache)
        //        return _staticCache.GetOrCreate(propertyInfo)();
        //    else
        //        return propertyInfo.CreateStaticGetValueExpression().Compile()();
        //} else {
        //    ArgumentNullException.ThrowIfNull(obj, "实例属性，但是没给实例对象");
        //    if (cache)
        //        return _instanceCache.GetOrCreate(propertyInfo)(obj);
        //    else
        //        return propertyInfo.CreateInstanceGetValueExpression().Compile()(obj);
        //}
    }

    // /// <summary>
    // /// 利用表达式树构建委托并执行
    // /// </summary>
    // /// <typeparam name="TValue">使用<code> (TValue)value </code>转换 这样你就不用在你那边转换了</typeparam>
    // /// <param name="propertyInfo"> 要被取值的属性 </param>
    // /// <param name="obj"> 实例属性需要传递实例对象，静态属性可以不用管 </param>
    // /// <param name="cache"> 是否缓存构建的表达式树。如果你不缓存，建议使用<see cref="CreateStaticGetValueExpression(PropertyInfo)"/> 或者 <see cref="CreateInstanceGetValueExpression(PropertyInfo)"/> </param>
    // /// <returns></returns>
    /// <summary>
    /// 调用反射执行
    /// </summary>
    public static TValue? ExpGetValue<TValue>(this PropertyInfo propertyInfo, object? obj = null)
        => (TValue?)propertyInfo.ExpGetValue(obj);

    /// <summary>
    /// 弱类型，静态属性访问表达式。不会被缓存。若需要 请自行缓存
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
    /// 弱类型，实例属性访问表达式。不会被缓存。若需要 请自行缓存
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
    /// 强类型表达式树，既可以是静态属性，也可以是实例属性。不会被缓存。若需要 请自行缓存
    /// </summary>
    public static Expression<TDelegate> CreateGetValueExpression<TDelegate>(this PropertyInfo property)
        where TDelegate : Delegate
    {
        var get = property.GetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        
        // 非静态
        if (!get.IsStatic) {
            var a = Expression.Parameter(property.DeclaringType!);
            var getExp = Expression.Property(a, property);
            return Expression.Lambda<TDelegate>(getExp, a);
        } else {
            var getExp = Expression.Property(null, property);
            return Expression.Lambda<TDelegate>(getExp);
        }
    }
}