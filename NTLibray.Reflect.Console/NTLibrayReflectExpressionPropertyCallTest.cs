using NTLibray.Reflect.Expressions;
using System.Diagnostics;
using System.Text;

namespace NTLibray.Reflect.ConsoleRun;

public static class NTLibrayReflectExpressionPropertyCallTest
{
    public static async Task Run()
    {
        //  Exp反射访问
        //  耗时: 10800274300 ns
        //  其实是直接调用的反射，但是在千万次的调用下，方法栈帧切换时间也达到了高额时间

        //  反射访问
        //  耗时: 10366590400 ns
            
        //  表达式树 缓存访问
        //  耗时: 7466653900 ns
            
        //  表达式树 强类型访问
        //  耗时: 3221050500 ns
            
        //  直接访问
        //  耗时: 1478667600 ns


        var props = typeof(TestClass).GetProperties();
        var obj = new TestClass();
        const int count = 10_000_000;
        StringBuilder strb = new StringBuilder();

        Parallel.For(1, count / 1000, new ParallelOptions() { MaxDegreeOfParallelism = 100000 }, (_, _) => {
            Console.WriteLine("预热");
            Console.Clear();
        });

        #region Get
        CountTime(() => {
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < props.Length; i++) {
                    strb.Append(props[i].ExpGetValue(obj));
                }
            }
        }, "Exp反射访问");
        strb.Clear();

        CountTime(() => {
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < props.Length; i++) {
                    strb.Append(props[i].GetValue(obj));
                }
            }
        }, "反射访问");
        strb.Clear();

        CountTime(() => {
            var propExps = props.Select(p => (Func<object, object>)p.AccessGetDelegateByCache()).ToArray();
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < propExps.Length; i++) {
                    strb.Append(propExps[i](obj));
                }
            }
        }, "表达式树 缓存访问");
        strb.Clear();

        CountTime(() => {
            var propExps = props.Where(p => p.PropertyType == typeof(string)).Select(p => p.CreateGetValueExpression<Func<TestClass, string>>().Compile()).ToArray();
            var propExps2 = props.Where(p => p.PropertyType == typeof(int)).Select(p => p.CreateGetValueExpression<Func<TestClass, int>>().Compile()).ToArray();

            for (int b = 0; b < count; b++) {
                for (int i = 0; i < propExps.Length; i++) {
                    strb.Append(propExps[i](obj));
                }

                for (int i = 0; i < propExps2.Length; i++) {
                    strb.Append(propExps2[i](obj));
                }
            }
        }, "表达式树 强类型访问");
        strb.Clear();

        Console.WriteLine();
        TestClass[] objs = [new(), new(), new(), new(), new(), new(), new(), new()];
        CountTime(() => {
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < objs.Length; i++) {
                    strb.Append(objs[i].Value1);
                }
            }
        }, "直接访问");
        strb.Clear();
        #endregion

        CountTime(() => {
            for (int j = 0; j < count; j++) {
                for (int i = 0; i < props.Length; i++) {
                    var prop = props[i];
                    prop.ExpSetValue(obj, prop.PropertyType == typeof(string) ? "1" : 0);
                }
            }
        }, "ExpSet设置属性值");

        CountTime(() => {
            for (int j = 0; j < count; j++) {
                for (int i = 0; i < props.Length; i++) {
                    var prop = props[i];
                    prop.SetValue(obj, prop.PropertyType == typeof(string) ? "1" : 0);
                }
            }
        }, "反射设置属性值");

        CountTime(() => {
            var str = props.Where(p => p.PropertyType == typeof(string)).Select(p => p.CreateSetValueExpression<Action<TestClass, string>>().Compile()).ToArray();
            var intp = props.Where(p => p.PropertyType == typeof(int)).Select(p => p.CreateSetValueExpression<Action<TestClass, int>>().Compile()).ToArray();
            for (int j = 0; j < count; j++) {
                for (int i = 0; i < str.Length; i++) {
                    str[i](obj, "1");
                }
                for (int i = 0; i < intp.Length; i++) {
                    intp[i](obj, 0);
                }
            }
        }, "强类型表达式树设置值(编译后缓存)");

        CountTime(() => {
            for (int j = 0; j < count; j++) {
                obj.Value1 = "1";
                obj.Value2 = "1";
                obj.Value3 = 0;
                obj.Value4 = 0;
                obj.Value5 = 0;
                obj.Value6 = 0;
                obj.Value7 = "1";
                obj.Value8 = "1";
            }
        }, "直接设置值");

    }

    private static void CountTime(Action action, string? msg = null)
    {
        Console.WriteLine();
        Console.WriteLine(msg);
        long start = Stopwatch.GetTimestamp();
        action.Invoke();
        long end = Stopwatch.GetTimestamp();
        double elapsedNanoseconds = (end - start) * 1_000_000_000.0 / Stopwatch.Frequency;
        Console.WriteLine($"耗时: {elapsedNanoseconds:F0} ns");
        GC.Collect(3, GCCollectionMode.Forced, true);
    }

    public class TestClass
    {
        public string Value1 { get; set; } = "v1";
        public string Value2 { get; set; } = "v2";
        public int Value3 { get; set; } = 999;
        public int Value4 { get; set; } = 999;
        public int Value5 { get; set; } = 999;
        public int Value6 { get; set; } = 999;
        public string Value7 { get; set; } = "v7";
        public string Value8 { get; set; } = "v8";
    }
}
