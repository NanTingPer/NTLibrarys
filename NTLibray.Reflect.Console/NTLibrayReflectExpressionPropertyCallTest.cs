using NTLibray.Reflect.Expressions;
using System.Diagnostics;
using System.Text;

namespace NTLibray.Reflect.ConsoleRun;

public static class NTLibrayReflectExpressionPropertyCallTest
{
    public static void Run()
    {
        //  Exp反射访问
        //  耗时: 10800274300 ns
        // 其实是直接调用的反射，但是在千万次的调用下，方法栈帧切换时间也达到了高额时间

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
        const int count = 80_000_000;
        StringBuilder strb = new StringBuilder();
        Console.WriteLine("Exp反射访问");
        CountTime(() => {
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < props.Length; i++) {
                    strb.Append(props[i].ExpGetValue(obj));
                }
            }
        });
        strb.Clear();

        Console.WriteLine();
        Console.WriteLine("反射访问");
        CountTime(() => {
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < props.Length; i++) {
                    strb.Append(props[i].GetValue(obj));
                }
            }
        });
        strb.Clear();

        Console.WriteLine();
        Console.WriteLine("表达式树 缓存访问");
        CountTime(() => {
            var propExps = props.Select(p => (Func<object, object>)p.AccessDelegateByCache()).ToArray();
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < propExps.Length; i++) {
                    strb.Append(propExps[i](obj));
                }
            }
        });
        strb.Clear();

        Console.WriteLine();
        Console.WriteLine("表达式树 强类型访问");
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
        });
        strb.Clear();

        Console.WriteLine();
        Console.WriteLine("直接访问");
        TestClass[] objs = [new(), new(), new(), new(), new(), new(), new(), new()];
        CountTime(() => {
            for (int b = 0; b < count; b++) {
                for (int i = 0; i < objs.Length; i++) {
                    strb.Append(objs[i].Value1);
                }
            }
        });
        strb.Clear();
    }

    private static void CountTime(Action action)
    {
        long start = Stopwatch.GetTimestamp();
        action.Invoke();
        long end = Stopwatch.GetTimestamp();
        double elapsedNanoseconds = (end - start) * 1_000_000_000.0 / Stopwatch.Frequency;
        Console.WriteLine($"耗时: {elapsedNanoseconds:F0} ns");
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
