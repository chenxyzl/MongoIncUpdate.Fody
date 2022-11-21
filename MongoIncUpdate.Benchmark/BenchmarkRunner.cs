using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MongoIncUpdate.Benchmark;

public sealed class BenchmarkRunner
{
    public static void Run<T>() where T : class
    {
        //todo 获取类型符合的属性--自用,就不检查合法性了
        var allBenchmarkParams = typeof(T).GetProperties()
            .Where(typ => typ.GetCustomAttributes(true).OfType<ParamsAttribute>().Any()).ToArray();
        //todo 获取类型符合的方法,以及方法的参数--自用,就不检查合法性了
        var allBenchmarkMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any()).ToArray();
        //todo 获取类型符合的方法,以及方法的参数--自用,就不检查合法性了
        var allBenchmarkSetupMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<GlobalSetupAttribute>().Any()).ToArray();
        //todo 获取类型符合的方法,以及方法的参数--自用,就不检查合法性了
        var allBenchmarkCleanMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<GlobalCleanupAttribute>().Any()).ToArray();

        var caseSetupMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<CaseSetupAttribute>().Any()).ToArray();
        var caseCleanMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<CaseCleanupAttribute>().Any()).ToArray();

        //构造对象
        var obj = Activator.CreateInstance<T>();

        //先调用Setup方法
        foreach (var method in allBenchmarkSetupMethods)
        {
            method.Invoke(obj, null);
        }

        if (allBenchmarkParams.Length > 0)
        {
            //第一次循环赋初始值
            foreach (var param in allBenchmarkParams)
            {
                var ps = param.GetCustomAttributes(true).OfType<ParamsAttribute>().ToArray();
                if (ps.Length != 1) throw new Exception("Params not allow AllowMultiple");
                var paramsAttribute = ps[0]; //不会重复
                foreach (var paramsValue in paramsAttribute.Values)
                {
                    param.SetValue(obj, paramsValue);
                    break;
                }
            }

            //第二次循环开始执行
            List<string> record = new List<string>();
            var i = -1;
            foreach (var param in allBenchmarkParams)
            {
                i++;
                record.Add(param.Name);
                var ps = param.GetCustomAttributes(true).OfType<ParamsAttribute>().ToArray();
                if (ps.Length != 1) throw new Exception("Params not allow AllowMultiple");
                var paramsAttribute = ps[0]; //不会重复
                var j = -1;
                foreach (var paramsValue in paramsAttribute.Values)
                {
                    j++;
                    param.SetValue(obj, paramsValue);
                    record[i] = $"{param.Name}:{paramsValue}";
                    CallAllMethod(obj, allBenchmarkMethods, caseSetupMethods, caseCleanMethods,
                        $"[{string.Join("-", record.ToArray())}]");
                }
            }
        }
        else
        {
            CallAllMethod(obj, allBenchmarkMethods, caseSetupMethods, caseCleanMethods, "[]");
        }

        //再调用Cleanup
        foreach (var method in allBenchmarkCleanMethods)
        {
            method.Invoke(obj, null);
        }
    }

    static void CallAllMethod<T>(T obj, MethodInfo[] allBenchmarkMethods, MethodInfo[] caseSetupMethods,
        MethodInfo[] caseCleanMethods, string paramPrefix) where T : class
    {
        foreach (var method in allBenchmarkMethods)
        {
            var paramsArray = method.GetCustomAttributes(true).OfType<ArgumentsAttribute>().ToArray();
            if (paramsArray.Length > 0)
            {
                foreach (var argumentsAttribute in paramsArray)
                {
                    RunSingleMethod(obj, method, argumentsAttribute.Values, caseSetupMethods, caseCleanMethods,
                        paramPrefix);
                }
            }
            else
            {
                RunSingleMethod(obj, method, new object[] { }, caseSetupMethods, caseCleanMethods, paramPrefix);
            }
        }
    }

    static void RunSingleMethod<T>(T obj, MethodInfo method, object[] values, MethodInfo[] caseSetupMethod,
        MethodInfo[] caseCleanMethod, string paramPrefix) where T : class
    {
        var str = $"paramPrefix:{paramPrefix}:{method.Name}:{string.Join("-", values.ToArray())}";
        foreach (var methodInfo in caseSetupMethod)
        {
            methodInfo.Invoke(obj, new object[] { method.Name });
        }
        
        Console.WriteLine($"{str}: 开始运行...");
        var start = Stopwatch.StartNew();
        while (true)
        {
            method.Invoke(obj, values);
            //执行5分钟这个方法
            if (start.ElapsedMilliseconds >= 1_000) break;
        }

        foreach (var methodInfo in caseCleanMethod)
        {
            methodInfo.Invoke(obj, new object[] { method.Name });
        }

        Console.WriteLine($"{str}: 结束运行...");
    }
}