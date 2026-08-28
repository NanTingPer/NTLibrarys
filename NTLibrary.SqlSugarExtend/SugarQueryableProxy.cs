using NTLibrary.SqlSugarExtend.ExpressionReplacers;
using SqlSugar;
using System.Linq.Expressions;
using System.Reflection;

namespace NTLibrary.SqlSugarExtend;

public class SugarQueryableProxy
{
    private ISugarQueryable<object> origQuery;
    private Type queryCurrentType = null!;
    private SugarQueryableProxy(ISugarQueryable<object> origQuery) 
    {
        this.origQuery = origQuery;
    }

    public static SugarQueryableProxy Create<TResult>(ISugarQueryable<TResult> query)
    {
        var queryProxy = new SugarQueryableProxy(query.Select<object>(a => a)) { queryCurrentType = typeof(TResult) } ;
        return queryProxy;
    }

    // 表达式内部的一切都需要是object的，例如构建MembingExpression时，需要使用 Expression.Convert(par, Type)，因为参数是object 不能直接访问属性。
    // 核心设计还是在SelectAllExpressionMethod中，其他如参数替换为object类型并处理访问时的类型转换。均由其他子方法完成。

    //public SugarQueryableProxy Select<TShape>(Expression<Func<TShape, object>> overrExpression)
    //{
    //    origQuery.Select(ExpressionExtend.SelectAllExpression<object,>(overrExpression));
    //}

    public SugarQueryableProxy OrderBy<TShape>(Expression<Func<TShape, object>> orderBy_expression)
    {
        var body = orderBy_expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } convertExpression) {
            body = convertExpression.Operand;
        }
        // 这里要构建参数树，参数树的类型可以用 ISugarQueryable<object> 中object的具体类型
        // Type.GetTypeFromHandle(origQuery.GetType().GetGenericArguments()[0].TypeHandle); origQuery是 ISugarQueryable<object> 的实例
        // queryCurrentType 上面那样只能取到object，还是需要追踪类型变化
        var parameterExpression = Expression.Parameter(queryCurrentType, "a");
        var replacer = new ParameterReplacers(parameterExpression);
        var propertyReplacer = new PropertyInfoReplacer(parameterExpression, queryCurrentType.GetProperties());

        if (body is MemberExpression memberExpression) {
            // 对于基础的属性访问，只需要将 参数 TShape 替换为上面构建的参数树
            //memberExpression.Member.Name; 用于排序的属性名
            if (memberExpression.Member.MemberType != MemberTypes.Property) {
                throw new Exception("只能使用属性成员访问");
            }
            var targetMemberExpression = replacer.Visit(memberExpression) as MemberExpression;
            var fMemberExpression = propertyReplacer.Visit(targetMemberExpression) as MemberExpression;
            var fLambda = Expression.Lambda<Func<object, object>>(fMemberExpression, parameterExpression);
            origQuery = origQuery.OrderBy(fLambda);
            return this;
        }

        if (body is NewExpression newExpression) {
            // 对于new构建的多字段排序，只需要将new中的参数替换为上面构建的参数树 并将属性访问的 Member 替换为参数类型对应名字的Member
            // 即: 原本new属性取的是 TShape 替换后 取的是查询目前实际的类型
            //newExpression.Members; 用于排序的多个属性
        }

        // 构建完Order表达式后，将其应用到 origQuery 使用 Expression<Func<object, object>> 重载。
        // 可能需要构建类型转换树 Expression.Convert(属性访问树 / new树, typeof(object));
        // origQuery.OrderBy(最终树);
        return this;
    }
}
