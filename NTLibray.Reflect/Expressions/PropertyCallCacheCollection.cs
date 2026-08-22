using System.Collections.Concurrent;
using System.Reflection;
//using System.Runtime.CompilerServices;

namespace NTLibray.Reflect.Expressions;

/// <summary>
/// 使用表达式树构建的属性调用缓存
/// </summary>
/// <typeparam name="TFunc"> 如果是静态的，则使用<see cref="Func{Object}"/>, 如果是实例的，则使用<see cref="Func{Object, Object}"/> </typeparam>
internal class PropertyCallCacheCollection<TFunc>
    where TFunc : Delegate
{
    //private readonly ConditionalWeakTable<Type, ConcurrentDictionary<string, TFunc>> _cache = [];
    private readonly ConcurrentDictionary<PropertyInfo, TFunc> _cache = [];

    public void Add(PropertyInfo propertyInfo, TFunc func)
    {
        _cache.AddOrUpdate(propertyInfo, func, (_, _) => func);

        //var type = propertyInfo.DeclaringType!;
        //var value = _cache.GetOrAdd(type, key => {
        //    var dic = new ConcurrentDictionary<string, TFunc>();
        //    return dic;
        //});
        //// 替换
        //value.AddOrUpdate(propertyInfo.Name, p => func, (_, _) => func);
    }

    public TFunc? Get(PropertyInfo propertyInfo)
    {
        _cache.TryGetValue(propertyInfo, out var value);
        return value;
        //var type = propertyInfo.DeclaringType!;
        //if (_cache.TryGetValue(type, out var pcaches)) {
        //    if (pcaches.TryGetValue(propertyInfo.Name, out var func)) {
        //        return func;
        //    }
        //}
        //return null;
    }

    public TFunc? Remove(PropertyInfo propertyInfo)
    {
        _cache.Remove(propertyInfo, out var value);
        return value;
        //var type = propertyInfo.DeclaringType!;
        //if (_cache.TryGetValue(type, out var pcaches)) {
        //    pcaches.TryRemove(propertyInfo.Name, out TFunc? removeFunc);
        //    return removeFunc;
        //}
        //return null;
    }
}