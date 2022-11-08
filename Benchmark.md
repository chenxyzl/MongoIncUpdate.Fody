## Mongo增量更新(fody)性能测试

## 性能对比
### 单个集合10项数据(_stateMapCount=10)
|                             Method |     Mean |    Error |   StdDev | Rank |    Gen0 |   Gen1 | Allocated |
|----------------------------------- |---------:|---------:|---------:|-----:|--------:|-------:|----------:|
|             BenchmarkIncUpdate增量更新 | 695.5 μs | 13.89 μs | 12.99 μs |    4 | 10.7422 | 0.9766 | 189.14 KB |
|  BenchmarkTotalSave0使用update方式更新部分 | 330.8 μs |  1.94 μs |  1.72 μs |    2 |  1.4648 | 0.4883 |  28.36 KB |
| BenchmarkTotalSave1使用replace方式整体更新 | 326.6 μs |  6.39 μs |  5.98 μs |    2 |  0.9766 | 0.4883 |  21.49 KB |
|         BenchmarkTotalSave2先序列化再存储 | 350.4 μs |  6.91 μs |  7.39 μs |    3 |  1.4648 |      - |   27.2 KB |
|        BenchmarkTotalSave3跳过序列化只存储 | 298.1 μs |  4.27 μs |  4.00 μs |    1 |  0.9766 |      - |  19.71 KB |


### 单个集合100项数据(_stateMapCount=100)
|                             Method |       Mean |    Error |    StdDev |     Median | Rank |    Gen0 | Allocated |
|----------------------------------- |-----------:|---------:|----------:|-----------:|-----:|--------:|----------:|
|             BenchmarkIncUpdate增量更新 |   728.8 μs | 13.47 μs |  19.75 μs |   722.9 μs |    1 | 11.7188 | 196.25 KB |
|  BenchmarkTotalSave0使用update方式更新部分 | 1,411.5 μs | 44.85 μs | 132.25 μs | 1,394.6 μs |    4 |  3.9063 |  94.31 KB |
| BenchmarkTotalSave1使用replace方式整体更新 | 1,183.1 μs | 43.61 μs | 128.58 μs | 1,177.2 μs |    3 |  1.9531 |  44.08 KB |
|         BenchmarkTotalSave2先序列化再存储 | 1,444.9 μs | 41.02 μs | 120.30 μs | 1,431.4 μs |    5 |  3.9063 |  93.09 KB |
|        BenchmarkTotalSave3跳过序列化只存储 | 1,033.9 μs | 36.21 μs | 105.63 μs | 1,062.3 μs |    2 |       - |  28.88 KB |

### 单个集合1000项数据(_stateMapCount=1000)
|                             Method |        Mean |       Error |      StdDev | Rank |    Gen0 |    Gen1 | Allocated |
|----------------------------------- |------------:|------------:|------------:|-----:|--------:|--------:|----------:|
|             BenchmarkIncUpdate增量更新 |    992.4 μs |    10.98 μs |    10.27 μs |    1 | 15.6250 |       - | 273.87 KB |
|  BenchmarkTotalSave0使用update方式更新部分 |  5,117.8 μs |   608.35 μs | 1,793.74 μs |    3 | 46.8750 | 15.6250 | 832.49 KB |
| BenchmarkTotalSave1使用replace方式整体更新 |  4,556.0 μs |   617.51 μs | 1,820.75 μs |    2 | 15.6250 |       - | 269.13 KB |
|         BenchmarkTotalSave2先序列化再存储 |  5,766.5 μs |   460.33 μs | 1,342.82 μs |    4 | 46.8750 | 15.6250 | 831.35 KB |
|        BenchmarkTotalSave3跳过序列化只存储 | 10,057.6 μs | 1,882.64 μs | 5,551.02 μs |    5 |       - |       - | 120.32 KB |

## 名词解释
  Mean      : Arithmetic mean of all measurements  
  Error     : Half of 99.9% confidence interval  
  StdDev    : Standard deviation of all measurements  
  Median    : Value separating the higher half of all measurements (50th percentile)  
  Rank      : Relative position of current benchmark mean among all benchmarks (Arabic style)  
  Gen0      : GC Generation 0 collects per 1000 operations  
  Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)  
  1 μs      : 1 Microsecond (0.000001 sec)  

## 结论
随着玩家的单条数据大小(10,100,1000)增加,增量更新对比全量更新的性能效果越好(增量更新因为脏标记原因初始会稍高,但很稳定,几乎不会受数据增大而影响)。

## 压测代码
  ```C#
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace MongoIncUpdate.Fody.Test;

[MongoIncUpdate]
public class DirtyItem
{
    public int Int { get; set; } //0
    public float Flo { get; set; } //1
    public double? Dou { get; set; } //2 
    public string? Str { get; set; } //3
}

[MongoIncUpdate]
public class DirtyNestItem
{
    [BsonId] public int Id { get; set; }

    public DirtyItem Item { get; set; } //0

    [BsonSerializer(typeof(StateMapSerializer<int, DirtyItem>))]
    public StateMap<int, DirtyItem> StateMap { get; set; } //1
}

public class OldItem
{
    public int Int { get; set; } //0
    public float Flo { get; set; } //1
    public double? Dou { get; set; } //2 
    public string? Str { get; set; } //3
}

public class NestOldItem
{
    [BsonId] public int Id { get; set; }

    public OldItem Item { get; set; } //0

    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
    public Dictionary<int, OldItem> StateMap { get; set; } //1
}

[MemoryDiagnoser, RankColumn]
public class IncUpdateBenchmark
{
    private static DirtyNestItem _benchmarkIncUpdateData;
    private static NestOldItem _oldOldData;
    private static IMongoDatabase _db;
    private static ITestOutputHelper _output;
    private static IMongoCollection<DirtyNestItem> _cc;
    private static IMongoCollection<NestOldItem> _cc1;
    private static IMongoCollection<BsonDocument> _cc2;
    private static BsonDocument test2;
    private static Random _r;
    private const int _stateMapCount = 100;

    private static bool _init;

    public IncUpdateBenchmark()
    {
        lock (this)
        {
            if (_init) return;
            _init = true;
            //创建mongo链接
            var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
            var mongoClient = new MongoClient(connectionString);
            _db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);
            _cc = _db.GetCollection<DirtyNestItem>(nameof(DirtyNestItem));
            _cc1 = _db.GetCollection<NestOldItem>(nameof(NestOldItem));
            _cc2 = _db.GetCollection<BsonDocument>(("xxx"));

            //初始化数据
            _benchmarkIncUpdateData = new DirtyNestItem
            {
                Id = 1,
                Item = new DirtyItem { Int = 1, Str = "1", Flo = 1.0f, Dou = 1.0 },
                StateMap = new()
            };
            _r = new Random();
            for (int i = 0; i < _stateMapCount; i++)
            {
                var n = _r.Next() % 1000;
                _benchmarkIncUpdateData.StateMap.Add(i, new DirtyItem
                {
                    Int = n,
                    Flo = n,
                    Dou = n,
                    Str = n.ToString()
                });
            }

            test2 = _benchmarkIncUpdateData.ToBsonDocument();

            //初始化数据1
            _oldOldData = new NestOldItem
            {
                Id = 1,
                Item = new OldItem { Int = 1, Str = "1", Flo = 1.0f, Dou = 1.0 },
                StateMap = new()
            };
            _r = new Random();
            for (int i = 0; i < _stateMapCount; i++)
            {
                var n = _r.Next() % 1000;
                _oldOldData.StateMap.Add(i, new OldItem
                {
                    Int = n,
                    Flo = n,
                    Dou = n,
                    Str = n.ToString()
                });
            }
        }
    }

    [Benchmark]
    public void BenchmarkIncUpdate增量更新()
    {
        var n = _r.Next() % _stateMapCount;
        _benchmarkIncUpdateData.StateMap[n]!.Int = n;
        _benchmarkIncUpdateData.StateMap[n]!.Flo = n;
        _benchmarkIncUpdateData.StateMap[n]!.Dou = n;
        _benchmarkIncUpdateData.StateMap[n]!.Str = n.ToString();
        _benchmarkIncUpdateData.Item.Int = n;
        _benchmarkIncUpdateData.Item.Str = n.ToString();

        //保存数据
        var diffUpdateable = _benchmarkIncUpdateData as IDiffUpdateable;
        var defs = new List<UpdateDefinition<DirtyNestItem>>();
        diffUpdateable?.BuildUpdate(defs, "");
        if (defs.Count == 0) return;
        var setter = Builders<DirtyNestItem>.Update.Combine(defs);
        var filter = Builders<DirtyNestItem>.Filter.Eq("_id", 1);
        _cc.UpdateOne(filter, setter, new UpdateOptions { IsUpsert = true });
    }
    
    [Benchmark]
    public void BenchmarkTotalSave0使用update方式更新部分()
    {
        var n = _r.Next() % _stateMapCount;
        _oldOldData.StateMap[n]!.Int = n;
        _oldOldData.StateMap[n]!.Flo = n;
        _oldOldData.StateMap[n]!.Dou = n;
        _oldOldData.StateMap[n]!.Str = n.ToString();
        _oldOldData.Item.Int = n;
        _oldOldData.Item.Str = n.ToString();
        //保存数据
        var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        var update = Builders<NestOldItem>.Update.Set(f => f.StateMap, _oldOldData.StateMap);
        _cc1.UpdateOne(filter, update, new UpdateOptions() { IsUpsert = true });
    
        // var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        // _cc1.ReplaceOne(filter, _oldOldData, new ReplaceOptions() { IsUpsert = true });
    }

    [Benchmark]
    public void BenchmarkTotalSave1使用replace方式整体更新()
    {
        var n = _r.Next() % _stateMapCount;
        _oldOldData.StateMap[n]!.Int = n;
        _oldOldData.StateMap[n]!.Flo = n;
        _oldOldData.StateMap[n]!.Dou = n;
        _oldOldData.StateMap[n]!.Str = n.ToString();
        _oldOldData.Item.Int = n;
        _oldOldData.Item.Str = n.ToString();
        //保存数据
        // var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        // var update = Builders<NestOldItem>.Update.Set(f => f.StateMap, _oldOldData.StateMap);
        // _cc1.UpdateOne(filter, update, new UpdateOptions() { IsUpsert = true });
    
        var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        _cc1.ReplaceOne(filter, _oldOldData, new ReplaceOptions() { IsUpsert = true });
    }
    
    [Benchmark]
    public void BenchmarkTotalSave2先序列化再存储()
    {
        var n = _r.Next() % _stateMapCount;
        _oldOldData.StateMap[n]!.Int = n;
        _oldOldData.StateMap[n]!.Flo = n;
        _oldOldData.StateMap[n]!.Dou = n;
        _oldOldData.StateMap[n]!.Str = n.ToString();
        _oldOldData.Item.Int = n;
        _oldOldData.Item.Str = n.ToString();
        //保存数据
        // var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        // var update = Builders<NestOldItem>.Update.Set(f => f.StateMap, _oldOldData.StateMap);
        // _cc1.UpdateOne(filter, update, new UpdateOptions() { IsUpsert = true });
    
        var filter1 = Builders<BsonDocument>.Filter.Eq("_id", 1);
        _cc2.ReplaceOne(filter1, _oldOldData.ToBsonDocument(), new ReplaceOptions() { IsUpsert = true });
    }

    [Benchmark]
    public void BenchmarkTotalSave3跳过序列化只存储()
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", 1);
        _cc2.ReplaceOne(filter, test2, new ReplaceOptions() { IsUpsert = true });
    }
}
  ```