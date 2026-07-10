using AgentNexus.Core.Models;
using AgentNexus.Core.Models.Return;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentNexus.Tooling;

/// <summary>
/// 用于AI调用的方法
/// </summary>
public static class FunctionCallExecute
{
    /// <summary>
    /// 方法调用的委托类型
    /// </summary>
    /// <param name="parms">方法的传入参数</param>
    /// <returns></returns>
    public delegate object? FunctionCallDelegate(params object[] parms);

    /// <summary>
    /// <para> 全局方法表 </para>
    /// 全部方法调用的实例，可以直接调用
    /// </summary>
    public readonly static Dictionary<MethodInfo, FunctionCallDelegate> Instance = [];

    private static volatile bool _isInit = false;

    /// <summary>
    /// 初始话全局方法表
    /// </summary>
    public static void InitFunctionCalls(Assembly? assembly = null)
    {
        if(_isInit == true) {
            return;
        }
        _isInit = true;

        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var filename = GetAssembly(assembly).GetName().Name;
        var dsAgentXmlDocmentFile = Path.Combine(assemblyDirectory!, filename + ".xml");
        var methodXML = new MethodXML(dsAgentXmlDocmentFile);

        IEnumerable<Type> autoRegionFunctionTypes = GetAssembly(assembly).GetTypes()
                .Where(type => type.GetInterface(nameof(IAutoRegionFunction)) != null);

        var functionCallMethodInfos = autoRegionFunctionTypes.Select(type => {
            return type.GetMethods().Where(methodinfo => {
                // 异步方法状态机
                var compilerGenerated = methodinfo.IsDefined(typeof(CompilerGeneratedAttribute), false);
                if (compilerGenerated) {
                    return false;
                }

                var functionCallAttribute = methodinfo.GetCustomAttribute<FunctionCallAttribute>();
                if (functionCallAttribute == null || !methodinfo.IsStatic) {
                    return false;
                }

                return true;
            });
        })
                .SelectMany(methodInfos => methodInfos)
            ;

        Tools = [.. functionCallMethodInfos.Select(methodXML.GetMethodXMLNotes)];

        //如果使用CreateDelegate，那么他的第一个参数是委托类型
        foreach (var @delegate in functionCallMethodInfos.Select(m => (m, CreateAdapter(m)))) {
            Instance[@delegate.m] = @delegate.Item2;
        }
    }

    /// <summary>
    /// 创建适配器
    /// </summary>
    /// <param name="methodInfo">要被适配的方法引用</param>
    /// <returns></returns>
    public static FunctionCallDelegate CreateAdapter(MethodInfo methodInfo)
    {
        var methodParameters = methodInfo.GetParameters();

        // parms 创建参数树
        var parms = Expression.Parameter(typeof(object[]), "parms");

        List<Expression> args = [];
        for (int i = 0; i < methodParameters.Length; i++) {
            // parms[i]
            var iExpression = Expression.Constant(i);
            var argExpression = Expression.ArrayIndex(parms, iExpression);

            // (type)parms[i]
            var convertExpression = Expression.Convert(argExpression, methodParameters[i].ParameterType);
            args.Add(convertExpression);
        }

        //methodInfo.Invok(null, parms);
        var callExpression = Expression.Call(methodInfo, args);

        Expression bodyExpression;

        if (methodInfo.ReturnType == typeof(void)) {
            // methodInfo.Invok();
            // return null;
            // Block => 返回最后一个表达式树的结果
            bodyExpression = Expression.Block(callExpression, Expression.Constant(null));
        } else {
            //(object)methodInfo.Invok;
            bodyExpression = Expression.Convert(callExpression, typeof(object));
        }

        //(parms) => (object)methodInfo.Invok(args)
        var lambda = Expression.Lambda<FunctionCallDelegate>(bodyExpression, parms);

        return lambda.Compile();
    }

    /// <summary>
    /// 创建Task.Result的Func
    /// </summary>
    /// <typeparam name="T"> Task的泛型类型 </typeparam>
    /// <returns></returns>
    private static Func<Task<T>, T> CreateTaskResult<T>()
    {
        var taskT = typeof(Task<>).MakeGenericType(typeof(T));
        //task => task.Result
        var parameter = Expression.Parameter(taskT, "task");
        var property = Expression.Property(parameter, nameof(Task<int>.Result));
        var body = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<Task<T>, T>>(body, parameter).Compile();
    }

    private readonly static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 全部工具的方法文档
    /// </summary>
    public static List<MethodXMLNote> Tools { get; private set; } = [];

    /// <summary>
    /// 全部工具的文档字符串
    /// </summary>
    public static string ToolsString => JsonSerializer.Serialize(Tools, jsonSerializerOptions);

    /// <summary>
    /// Ds API调用标准中的Tools传递参数
    /// </summary>
    public static List<DsFunctionToolsSchema> DsToolsString => Tools.Select(x => new DsFunctionToolsSchema(x)).ToList();

    /// <summary>
    /// 使用给定参数调用给定方法表中的给定方法
    /// </summary>
    /// <param name="tools">  </param>
    /// <param name="methodName"> 全局方法名称 </param>
    /// <param name="params"> 参数列表 </param>
    /// <returns></returns>
    private static async Task<object?> DynamicInvoke(Dictionary<MethodInfo, FunctionCallDelegate> tools, MethodInfo method, params object[] @params)
    {
        //if (!Instance.TryGetValue(methodName, out var value)) {
        if (!tools.TryGetValue(method, out var value)) {
            return "不存在的方法, 请先使用工具列表获取工具";
        }

        var result = value.Invoke(@params);
        if (result == null || result is not Task task) {
            return result;
        }

        await task;
        //泛型Task需要获取类型
        var resultType = result.GetType();

        var baseType = resultType.BaseType;
        bool baseTypeIsNull = !(baseType == null);
        bool baseTypeIsNullTwo = !(baseType == null);
        //判断基类型的类型是不是Task<>
        //如果方法返回的是 非 async Task<> 需要这里
        if (baseTypeIsNull == false) {
            try {
                baseTypeIsNull = baseTypeIsNull && (resultType.BaseType!.GetGenericTypeDefinition() == typeof(Task<>));
            } catch {
                baseTypeIsNull = false;
            }
        }

        //判断类型本身是否是是泛型，如果是那么判断泛型类型是不是 Task<>
        //如果方法返回的是 async Task<> 走这里
        if (baseTypeIsNullTwo == false && resultType.IsGenericType) {
            baseTypeIsNullTwo = baseTypeIsNullTwo && (resultType.GetGenericTypeDefinition() == typeof(Task<>));
        }

        //当异步方法的返回值为async Task<> 时候会被封装成状态机
        //当异步方法的返回值为Task<> 时，不会被封装成状态机
        if (resultType.IsGenericType &&
            //得到的是Task<>被编译器封装后的状态机类型
            //状态机的基类是Task<> 所以可以使用resultType.BaseType判断
            (baseTypeIsNull || baseTypeIsNullTwo)) {
            return resultType.GetProperty(nameof(Task<int>.Result))!.GetValue(task);
        }

        return null;
    }

    /// <summary>
    /// 使用<see cref="ToolCall"/> 调用全局方法表中的给定方法
    /// </summary>
    /// <param name="call"> 给定方法 </param>
    /// <returns></returns>
    public static async Task<object> DynamicInvoke(ToolCall call)
        => await DynamicInvoke(Instance, call);

    /// <summary>
    /// 调用给定方法表中的给定方法
    /// </summary>
    /// <param name="tools"> 方法表 </param>
    /// <param name="call"> 要被调用的方法 </param>
    /// <returns></returns>
    public static async Task<object> DynamicInvoke(Dictionary<MethodInfo, FunctionCallDelegate> tools, ToolCall call)
    {
        var function = call.Function;
        if (function == null) {
            return "工具调用中，函数为空";
        }

        List<object> parms = [];
        var functionName = function.Name;
        MethodInfo? origInfo = null;

        foreach (var item in tools) {
            if (item.Key.Name == functionName) {
                origInfo = item.Key;
                break;
            }
        }

        if (origInfo == null) {
            return "工具调用中，无效工具名";
        }

        var methodParameterInfos = origInfo!.GetParameters();

        for (int i = 0; i < function.ArgumentsJsonObject.Count; i++) {
            parms.Add(function.ArgumentsJsonObject[i].Deserialize(methodParameterInfos[i].ParameterType)!);
        }

        object? callResult;
        try {
            callResult = await DynamicInvoke(tools, origInfo, [.. parms]);
        } catch (Exception exception) {
            return exception.Message;
        }

        return callResult ?? "工具调用完成了!";
    }

    /// <summary>
    /// 如果给定的程序集为null，那么返回当前调用方的程序集
    /// </summary>
    /// <returns></returns>
    private static Assembly GetAssembly(Assembly? assembly = null)
    {
        if (assembly == null) {
            return Assembly.GetCallingAssembly();
        } else {
            return assembly;
        }
    }
}