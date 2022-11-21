using System;
using System.Net;
using MongoBenchmark;
using Prometheus;

namespace MongoIncUpdate.Benchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        Env.Init(args);
        RunBenchmark();
        Env.Stop();
    }

    private static void RunBenchmark()
    {
        Console.WriteLine("---Benchmark---");
        BenchmarkRunner.Run<MongoBenchmark.MongoBenchmark>();
        Console.WriteLine("---Benchmark完成---");
    }
}