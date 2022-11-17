using System.Diagnostics;
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
            foreach (var param in allBenchmarkParams)
            {
                var ps = param.GetCustomAttributes(true).OfType<ParamsAttribute>().ToArray();
                if (ps.Length != 1) throw new Exception("Params not allow AllowMultiple");
                var paramsAttribute = ps[0]; //不会重复
                foreach (var paramsValue in paramsAttribute.Values)
                {
                    param.SetValue(obj, paramsValue);
                    CallAllMethod(obj, allBenchmarkMethods);
                }
            }
        }
        else
        {
            CallAllMethod(obj, allBenchmarkMethods);
        }

        //再调用Cleanup
        foreach (var method in allBenchmarkCleanMethods)
        {
            method.Invoke(obj, null);
        }
    }

    static void CallAllMethod<T>(T obj, MethodInfo[] allBenchmarkMethods) where T : class
    {
        foreach (var method in allBenchmarkMethods)
        {
            var paramsArray = method.GetCustomAttributes(true).OfType<ArgumentsAttribute>().ToArray();
            if (paramsArray.Length > 0)
            {
                foreach (var argumentsAttribute in paramsArray)
                {
                    RunSingleMethod(obj, method, argumentsAttribute.Values);
                }
            }
            else
            {
                RunSingleMethod(obj, method, null);
            }
        }
    }

    static void RunSingleMethod<T>(T obj, MethodInfo method, object[]? values) where T : class
    {
        var start = Stopwatch.StartNew();
        while (true)
        {
            method.Invoke(obj, values);
            //执行5分钟这个方法
            if (start.ElapsedMilliseconds >= 300_000) break;
        }
        
    }
}