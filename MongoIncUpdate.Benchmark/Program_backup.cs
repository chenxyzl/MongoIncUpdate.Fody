// using BenchmarkDotNet.Running;
// using MongoBenchmark;
//
// namespace MongoIncUpdate.Benchmark;
//
// public static class Program
// {
//     public static Task Main(string[] args)
//     {
//         Env.Init(args);
//         // RunBenchmark(args);
//
//         _ = Task.Run(async delegate
//         {
//             while (true)
//             {
//                 // Pretend to process a record approximately every second, just for changing sample data.
//                 Env.GetMetric("BenchmarkInsert").Inc(1);
//                 await Task.Delay(TimeSpan.FromSeconds(1));
//             }
//         });
//         Console.WriteLine("Press enter to exit.");
//         Console.ReadLine();
//         Env.Stop();
//         return Task.CompletedTask;
//     }
//
//     private static void RunBenchmark(string[] args)
//     {
//         Console.WriteLine("---Benchmark---");
//         var v = BenchmarkRunner.Run<MongoBenchmark.MongoBenchmark>();
//         Console.WriteLine("---Benchmark完成---");
//     }
// }