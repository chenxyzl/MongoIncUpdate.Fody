using System.Net;
using BenchmarkDotNet.Running;
using MongoBenchmark;
using Prometheus;

namespace MongoIncUpdate.Benchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        
        Console.ReadLine();

        server.Stop();
    }

    private static void RunBenchmark(string[] args)
    {
        Console.WriteLine("---Benchmark---");
        var v = BenchmarkRunner.Run<MongoBenchmark.MongoBenchmark>();
        Console.WriteLine("---Benchmark完成---");
    }
}