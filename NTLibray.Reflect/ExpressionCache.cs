using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace NTLibray.Reflect;

public abstract class ExpressionCacheAbstract<TFunc>
    where TFunc : Delegate
{
    private readonly static Cache<TFunc> _cache = [];
    protected abstract TFunc FuncFactory(PropertyInfo property);
    public void Add(PropertyInfo property, TFunc func)
    {
        _cache.Add(property, func);
    }
    /// <summary>
    /// 获取或创建，会存入缓存
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

public class StaticCache : ExpressionCacheAbstract<Func<object>>
{
    protected override Func<object> FuncFactory(PropertyInfo property)
    {
        return property.CreateStaticGetValueExpression().Compile();
    }
}
public class InstanceCache : ExpressionCacheAbstract<Func<object, object>>
{
    protected override Func<object, object> FuncFactory(PropertyInfo property)
    {
        return property.CreateInstanceGetValueExpression().Compile();
    }
}

internal class Cache<TFunc> : IDictionary<Type, Dictionary<PropertyInfo, TFunc>>
    where TFunc : Delegate
{
    private readonly Dictionary<Type, Dictionary<PropertyInfo, TFunc>> _cache = [];
    private IDictionary<Type, Dictionary<PropertyInfo, TFunc>> Ocache => _cache;

    public void Add(PropertyInfo propertyInfo, TFunc func)
    {
        var type = propertyInfo.DeclaringType!;
        if (!_cache.TryGetValue(type, out var value)) {
            var dic = new Dictionary<PropertyInfo, TFunc>();
            dic[propertyInfo] = func;
            _cache[type] = dic;
            return;
        }
        if (!value.TryGetValue(propertyInfo, out var _)) {
            value[propertyInfo] = func;
        }
    }

    public TFunc? Get(PropertyInfo propertyInfo)
    {
        var type = propertyInfo.DeclaringType!;
        if (_cache.TryGetValue(type, out var pcaches)) {
            if (pcaches.TryGetValue(propertyInfo, out var func)) {
                return func;
            }
        }
        return null;
    }

    public Dictionary<PropertyInfo, TFunc> this[Type key] 
    { 
        get => _cache[key]; 
        set => _cache[key] = value; 
    }

    public ICollection<Type> Keys => _cache.Keys;

    public ICollection<Dictionary<PropertyInfo, TFunc>> Values => _cache.Values;

    public int Count => _cache.Count;

    public bool IsReadOnly => Ocache.IsReadOnly;

    public void Add(Type key, Dictionary<PropertyInfo, TFunc> value)
        => _cache.Add(key, value);

    public void Add(KeyValuePair<Type, Dictionary<PropertyInfo, TFunc>> item)
        => _cache.Add(item.Key, item.Value);

    public void Clear()
    {
        foreach (var item in _cache.Values) {
            item.Clear();
        }
        _cache.Clear();
    }

    public bool Contains(KeyValuePair<Type, Dictionary<PropertyInfo, TFunc>> item)
        => Ocache.Contains(item);

    public bool ContainsKey(Type key)
        => _cache.ContainsKey(key);

    public void CopyTo(KeyValuePair<Type, Dictionary<PropertyInfo, TFunc>>[] array, int arrayIndex) =>
        Ocache.CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<Type, Dictionary<PropertyInfo, TFunc>>> GetEnumerator()
        => _cache.GetEnumerator();

    public bool Remove(Type key)
        => _cache.Remove(key);

    public bool Remove(KeyValuePair<Type, Dictionary<PropertyInfo, TFunc>> item)
        => Ocache.Remove(item);

    public bool TryGetValue(Type key, [MaybeNullWhen(false)] out Dictionary<PropertyInfo, TFunc> value)
        => _cache.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}