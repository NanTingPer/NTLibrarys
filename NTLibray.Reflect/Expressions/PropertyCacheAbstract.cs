using System.Reflection;

namespace NTLibray.Reflect.Expressions;

/// <summary>
/// 属性缓存抽象（Get）。
/// </summary>
public abstract class PropertyCacheAbstract<TFunc>
    where TFunc : Delegate
{
    private readonly static PropertyCallCacheCollection<TFunc> _cache = new();
    protected abstract TFunc FuncFactory(PropertyInfo property);
    public void Add(PropertyInfo property, TFunc func)
    {
        _cache.Add(property, func);
    }

    /// <summary>
    /// 获取或创建，会存入缓存，如果不存在则使用传入的工厂创建
    /// </summary>
    public TFunc GetOrCreate(PropertyInfo property, Func<PropertyInfo, TFunc> factory)
    {
        var func = _cache.Get(property);
        if (func == null) {
            func = factory(property);
            _cache.Add(property, func);
        }
        return func;
    }

    /// <summary>
    /// 获取或创建，创建方式取决于自类实现，会存入缓存
    /// </summary>
    public TFunc GetOrCreate(PropertyInfo property)
    {
        var func = _cache.Get(property);
        if (func == null) {
            func = FuncFactory(property);
            _cache.Add(property, func);
        }
        return func;
    }
}