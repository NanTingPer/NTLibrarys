using System.Linq.Expressions;
using System.Reflection;

namespace NTLibray.Reflect.Expressions;

/// <summary>
/// 表达树构建并缓存辅助方法
/// </summary>
public static class ExpressionHelper
{
    private readonly static StaticPropertyGetCache _staticGetCache = new ();
    private readonly static InstancePropertyGetCache _instanceGetCache = new();
    private readonly static StaticPropertySetCache _staticSetCache = new ();
    private readonly static InstancePropertySetCache _instanceSetCache = new ();
    /// <summary>
    /// 静态 <see cref="Func{TResult}"/>，实例 <see cref="Func{T, TResult}"/>
    /// </summary>
    public static object AccessGetDelegateByCache(this PropertyInfo propertyInfo)
    {
        var get = propertyInfo.GetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Get方法为Null，请确保属性可读");
        if (get.IsStatic)
            return _staticGetCache.GetOrCreate(propertyInfo);
        else
            return _instanceGetCache.GetOrCreate(propertyInfo);
    }

    public static object AccessSetDelegateByCache(this PropertyInfo propertyInfo)
    {
        var get = propertyInfo.SetMethod;
        ArgumentNullException.ThrowIfNull(get, "给定属性的Set方法为Null，请确保属性可写");
        if (get.IsStatic)
            return _staticSetCache.GetOrCreate(propertyInfo);
        else
            return _instanceSetCache.GetOrCreate(propertyInfo);
    }

    public static void ExpSetValue(this PropertyInfo propertyInfo, object? @this, object? value)
    {
        if((propertyInfo.GetMethod?.IsStatic ?? false) || (propertyInfo.SetMethod?.IsStatic ?? false)) {
            _staticSetCache.GetOrCreate(propertyInfo)(value!);
            return;
        } else {
            _instanceSetCache.GetOrCreate(propertyInfo)(@this!, value!);
        }
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
        //        return _staticGetCache.GetOrCreate(propertyInfo)();
        //    else
        //        return propertyInfo.CreateStaticGetValueExpression().Compile()();
        //} else {
        //    ArgumentNullException.ThrowIfNull(obj, "实例属性，但是没给实例对象");
        //    if (cache)
        //        return _instanceGetCache.GetOrCreate(propertyInfo)(obj);
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

    /// <summary>
    /// 属性设置表达式树，委托示例: <br />    
    /// <see cref="Action"/> <see cref="{object}"/> => 代表这个属性类型是object和静态<br />    
    /// <see cref="Action"/> <see cref="{A, B}"/>  => 代表这个属性是实例的，并且属于类型A <br />    
    /// </summary>
    /// <returns></returns>
    public static Expression<TDelegate> CreateSetValueExpression<TDelegate>(this PropertyInfo property)
        where TDelegate : Delegate
    {
        var set = property.SetMethod;
        ArgumentNullException.ThrowIfNull(set, "属性不可写，请确保属性不是只读的");   
        if (set.IsStatic) {
            var par = Expression.Parameter(property.PropertyType);
            var push = Expression.Property(null, set);
            var pop = Expression.Assign(push, par);
            return Expression.Lambda<TDelegate>(pop, par);
        } else {
            var value = Expression.Parameter(property.PropertyType);
            var @this = Expression.Parameter(property.DeclaringType!);
            var push = Expression.Property(@this, set);
            var pop = Expression.Assign(push, value);
            var if_this_not_null_then_set = Expression.IfThen(Expression.IsFalse(
                Expression.Equal(@this, Expression.Constant(null))),pop);
            return Expression.Lambda<TDelegate>(if_this_not_null_then_set, @this, value);
        }
    }

    public static Expression<Action<object>> CreateStaticSetValueExpression(this PropertyInfo property)
    {
        var set = property.SetMethod;
        ArgumentNullException.ThrowIfNull(set, "属性不可写，请确保属性不是只读的");
        if (!set.IsStatic) {
            throw new Exception($"非静态属性{property.Name}不要构建静态赋值表达式");
        }
        var par = Expression.Parameter(typeof(object));
        var covPar = Expression.Convert(par, property.PropertyType);
        var propExp = Expression.Property(null, property);
        var setExp = Expression.Assign(propExp, covPar);
        var body = Expression.Lambda<Action<object>>(setExp, par);
        return body;
    }

    /// <summary>
    /// 第一个参数是this，第二个参数是value
    /// </summary>
    public static Expression<Action<object, object>> CreateInstanceSetValueExpression(this PropertyInfo property)
    {
        var set = property.SetMethod;
        ArgumentNullException.ThrowIfNull(set, "属性不可写，请确保属性不是只读的");
        if (set.IsStatic) {
            throw new Exception($"静态属性{property.Name}不要构建实例赋值表达式");
        }
        var @this = Expression.Parameter(typeof(object));
        var thisCov = Expression.Convert(@this, property.DeclaringType!);

        var value = Expression.Parameter(typeof(object));
        var valueCov = Expression.Convert(value, property.PropertyType);
        var propExp = Expression.Property(thisCov, property);
        var setExp = Expression.Assign(propExp, valueCov);
        var if_this_not_null_then_set = Expression.IfThen(Expression.IsFalse(
            Expression.Equal(@this, Expression.Constant(null))), setExp);
        var body = Expression.Lambda<Action<object, object>>(if_this_not_null_then_set, @this, value);
        return body;
    }
}