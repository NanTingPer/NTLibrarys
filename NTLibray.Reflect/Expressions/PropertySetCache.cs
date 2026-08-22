using System.Reflection;

namespace NTLibray.Reflect.Expressions;

public class StaticPropertySetCache : PropertyCacheAbstract<Action<object>>
{
    protected override Action<object> FuncFactory(PropertyInfo property)
    {
        return property.CreateStaticSetValueExpression().Compile();
    }
}

public class InstancePropertySetCache : PropertyCacheAbstract<Action<object, object>>
{
    protected override Action<object, object> FuncFactory(PropertyInfo property)
    {
        return property.CreateInstanceSetValueExpression().Compile();
    }
}
