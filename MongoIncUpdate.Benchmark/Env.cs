using CommandLine;
using Prometheus;

namespace MongoBenchmark;

public static class Env
{
    private class Options
    {
        [Option('c', "ConcurrentCount", Required = true, HelpText = "并发数")]
        public int ConcurrentCount { get; set; }


        [Option('m', "Model", Required = false, HelpText = "模式")]
        public bool Model { get; set; } = false;
    }

    private static bool _init;
    private static readonly object LockObj = new();

    public static void Init(string[] args)
    {
        lock (LockObj)
        {
            if (_init) return;
            _init = true;
            InitSomeThings(args);
        }
    }

    static void InitParams(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
        {
            if (o.ConcurrentCount > 0)
            {
                Console.WriteLine($"Current ConcurrentCount: -c {o.ConcurrentCount}");
            }
            else
            {
                throw new Exception("ConcurrentCount must bigger than 0; please run with params[-c $num]");
            }
        });
    }

    private static MetricPusher? _pusher;
    private static string _jobNameAdjust = "";
    private static readonly Dictionary<string, ICounter> MetricsDic = new();
    private static MetricFactory? _factory;
    static void InitMetric()
    {
        _jobNameAdjust = "MongoBenchmark";
        var registry = Metrics.NewCustomRegistry();
        _factory = Metrics.WithCustomRegistry(registry);

        _pusher = new MetricPusher(new MetricPusherOptions
        {
            Endpoint = "http://localhost:1235/metrics",
            Job = $"MongoBenchmarkJob",
            OnError = e => { Console.Error.WriteLine($"ABC-ABC: {e}"); },
            // Instance = "MongoBenchmarkInstance",
            Registry = registry,
            IntervalMilliseconds = 30,
        });
        _pusher.Start();
    }

    static void RegisterMetrics()
    {
        Console.WriteLine($"metric registered begin");
        
        var counter = _factory.CreateCounter($"BenchmarkInsert", String.Empty);
        
        MetricsDic.Add("BenchmarkInsert", counter);

        foreach (var v in MetricsDic)
        {
            Console.WriteLine($"{v.Key} registered success");
        }

        Console.WriteLine($"metric registered end");
    }

    public static ICounter GetMetric(string name)
    {
        Console.WriteLine($"{MetricsDic.Count} registered aaa");
        MetricsDic.TryGetValue(name, out var gauge);
        if (gauge == null)
        {
            foreach (var v in MetricsDic)
            {
                Console.WriteLine($"{v.Key} registered bbb");
            }

            throw new Exception($"name:{name} not found");
        }

        return gauge;
    }


    static void InitMongo()
    {
    }

    static void InitSomeThings(string[] args)
    {
        InitParams(args);
        InitMetric();
        InitMongo();
        RegisterMetrics();
        //准备db
        //1.链接好db
        //2.创建好集合
        //3.清理老数据
        //4.准备好数据
    }

    public static void Stop()
    {
        _pusher?.Stop();
    }
}