using System.Linq.Expressions;

namespace NTLibrary.SqlSugarExtend.ExpressionReplacers;

/// <summary>
/// 参数替换，传入参数<see cref="Type"/>不要相同，因为是使用参数<see cref="Type"/>返回参数，只有匹配了才会返回
/// </summary>
public class ParameterReplacers : ExpressionVisitor
{
    private readonly ParameterExpression[] _params = [];
    public ParameterReplacers(params ParameterExpression[] par)
    {
        _params = par;
    }
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return _params.FirstOrDefault(f => f.Type == node.Type) ?? node;
    }
}
