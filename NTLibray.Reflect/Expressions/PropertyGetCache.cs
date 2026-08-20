using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NTLibray.Reflect.Expressions;

public class StaticPropertyGetCache : PropertyCacheAbstract<Func<object>>
{
    protected override Func<object> FuncFactory(PropertyInfo property)
    {
        return property.CreateStaticGetValueExpression().Compile();
    }
}

public class InstancePropertyGetCache : PropertyCacheAbstract<Func<object, object>>
{
    protected override Func<object, object> FuncFactory(PropertyInfo property)
    {
        return property.CreateInstanceGetValueExpression().Compile();
    }
}
