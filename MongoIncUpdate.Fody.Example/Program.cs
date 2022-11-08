using BenchmarkDotNet.Running;

namespace MongoIncUpdate.Fody.Example;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("---TestBenchmarkIncUpdate---");
        var v = BenchmarkRunner.Run<IncUpdateBenchmark>(); 
        // var v = new IncUpdateBenchmark();
        // v.BenchmarkIncUpdate();  
        Console.WriteLine("---TestBenchmarkIncUpdate完成---"); 
        Console.WriteLine(v); 
    }
}