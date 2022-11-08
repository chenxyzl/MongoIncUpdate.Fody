using BenchmarkDotNet.Running;

namespace MongoIncUpdate.Fody.Example;

public class Program
{
    public static void Main(string[] args)
    {
        Benchmark(args);
        Test(args);
    }

    public static void Benchmark(string[] args)
    {
        Console.WriteLine("---Benchmark---");
        var v = BenchmarkRunner.Run<IncUpdateBenchmark>();
        Console.WriteLine("---Benchmark完成---");
        Console.WriteLine(v);
    }

    public static void Test(string[] args)
    {
        Console.WriteLine("---Test---");
        var v = new IncUpdateBenchmark();
        v.BenchmarkIncUpdate();
        Console.WriteLine("---Test完成---");
    }
}