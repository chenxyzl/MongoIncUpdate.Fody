using BenchmarkDotNet.Running;

namespace MongoIncUpdate.Fody.Example;

public class Program
{
    public static void Main(string[] args)
    {
        // Benchmark(args);
        Test(args);
    }

    //需要性能测试就在Main里面打开
    public static void Benchmark(string[] args)
    {
        Console.WriteLine("---Benchmark---");
        var v = BenchmarkRunner.Run<IncUpdateBenchmark>();
        Console.WriteLine("---Benchmark完成---");
    }

    public static void Test(string[] args)
    {
        Console.WriteLine("---Test---");
        var v = new IncUpdateBenchmark();
        v.BenchmarkIncUpdate();
        Console.WriteLine("---Test完成---");
    }
}