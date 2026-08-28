using System.Linq.Expressions;
using System.Reflection;

namespace NTLibrary.SqlSugarExtend.ExpressionReplacers;

/// <summary>
/// 将参数树的属性访问替换为给定的属性数组中的属性信息 <br />
/// 注: 属性名称不应该一样，因为是根据名称在数组里面找的。 <br />
/// 返回的是 <see cref="MemberExpression"/>
/// <code>
/// Expression.Property(parameterExpression, target);
/// </code>
/// </summary>
/// <param name="parameterExpression"> 属性访问参数树 </param>
/// <param name="propertyMemberInfo"></param>
public class PropertyInfoReplacer(ParameterExpression parameterExpression, params PropertyInfo[] propertyMemberInfo) : ExpressionVisitor
{
    private PropertyInfo[] Infos { get; } = propertyMemberInfo;

    protected override Expression VisitMember(MemberExpression node)
    {
        var name = node.Member.Name;
        var target = Infos.FirstOrDefault(m => m.Name == name);
        if (target == null) return node;
        return Expression.Property(parameterExpression, target);
    }
}
