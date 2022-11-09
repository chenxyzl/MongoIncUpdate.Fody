using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using MongoIncUpdate.Base;

namespace MongoIncUpdate.Fody.Example;

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
    public Dictionary<int, OldItem> OItems { get; set; } //1
}

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

    [BsonSerializer(typeof(StateMapSerializer<int, DirtyItem>))]
    public StateMap<int, DirtyItem> XItems { get; set; } //0

    public DirtyItem Item { get; set; } //1
}

[MemoryDiagnoser]
[RankColumn]
public class IncUpdateBenchmark
{
    private const int _stateMapCount = 1000;
    private static DirtyNestItem _benchmarkIncUpdateData;
    private static NestOldItem _oldOldData;
    private static IMongoDatabase _db;
    private static IMongoCollection<DirtyNestItem> _cc;
    private static IMongoCollection<NestOldItem> _cc1;
    private static IMongoCollection<BsonDocument> _cc2;
    private static BsonDocument test2;
    private static Random _r;

    private static bool _init;

    public IncUpdateBenchmark()
    {
        lock (this)
        {
            if (_init) return;
            _init = true;
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(StateMap<,>), typeof(StateMapSerializer<,>));
            //创建mongo链接
            var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
            var mongoClient = new MongoClient(connectionString);
            _db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);
            _cc = _db.GetCollection<DirtyNestItem>(nameof(DirtyNestItem));
            _cc1 = _db.GetCollection<NestOldItem>(nameof(NestOldItem));
            _cc2 = _db.GetCollection<BsonDocument>("xxx");

            //初始化数据
            _benchmarkIncUpdateData = new DirtyNestItem
            {
                Id = 1,
                Item = new DirtyItem { Int = 1, Str = "1", Flo = 1.0f, Dou = 1.0 },
                XItems = new StateMap<int, DirtyItem>()
            };
            _r = new Random();
            for (var i = 0; i < _stateMapCount; i++)
            {
                var n = _r.Next() % 1000;
                _benchmarkIncUpdateData.XItems.Add(i, new DirtyItem
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
                OItems = new Dictionary<int, OldItem>()
            };
            _r = new Random();
            for (var i = 0; i < _stateMapCount; i++)
            {
                var n = _r.Next() % 1000;
                _oldOldData.OItems.Add(i, new OldItem
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
    public void BenchmarkIncUpdate()
    {
        var n = _r.Next() % _stateMapCount;
        _benchmarkIncUpdateData.XItems[n]!.Int = n;
        _benchmarkIncUpdateData.XItems[n]!.Flo = n;
        _benchmarkIncUpdateData.XItems[n]!.Dou = n;
        _benchmarkIncUpdateData.XItems[n]!.Str = n.ToString();
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
    public void BenchmarkTotalSave0()
    {
        var n = _r.Next() % _stateMapCount;
        _oldOldData.OItems[n]!.Int = n;
        _oldOldData.OItems[n]!.Flo = n;
        _oldOldData.OItems[n]!.Dou = n;
        _oldOldData.OItems[n]!.Str = n.ToString();
        _oldOldData.Item.Int = n;
        _oldOldData.Item.Str = n.ToString();
        //保存数据
        var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        var update = Builders<NestOldItem>.Update.Set(f => f.OItems, _oldOldData.OItems);
        _cc1.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });

        // var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        // _cc1.ReplaceOne(filter, _oldOldData, new ReplaceOptions() { IsUpsert = true });
    }

    [Benchmark]
    public void BenchmarkTotalSave1()
    {
        var n = _r.Next() % _stateMapCount;
        _oldOldData.OItems[n]!.Int = n;
        _oldOldData.OItems[n]!.Flo = n;
        _oldOldData.OItems[n]!.Dou = n;
        _oldOldData.OItems[n]!.Str = n.ToString();
        _oldOldData.Item.Int = n;
        _oldOldData.Item.Str = n.ToString();
        //保存数据
        // var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        // var update = Builders<NestOldItem>.Update.Set(f => f.StateMap, _oldOldData.StateMap);
        // _cc1.UpdateOne(filter, update, new UpdateOptions() { IsUpsert = true });

        var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        _cc1.ReplaceOne(filter, _oldOldData, new ReplaceOptions { IsUpsert = true });
    }

    [Benchmark]
    public void BenchmarkTotalSave2()
    {
        var n = _r.Next() % _stateMapCount;
        _oldOldData.OItems[n]!.Int = n;
        _oldOldData.OItems[n]!.Flo = n;
        _oldOldData.OItems[n]!.Dou = n;
        _oldOldData.OItems[n]!.Str = n.ToString();
        _oldOldData.Item.Int = n;
        _oldOldData.Item.Str = n.ToString();
        //保存数据
        // var filter = Builders<NestOldItem>.Filter.Eq("_id", 1);
        // var update = Builders<NestOldItem>.Update.Set(f => f.StateMap, _oldOldData.StateMap);
        // _cc1.UpdateOne(filter, update, new UpdateOptions() { IsUpsert = true });

        var filter1 = Builders<BsonDocument>.Filter.Eq("_id", 1);
        _cc2.ReplaceOne(filter1, _oldOldData.ToBsonDocument(), new ReplaceOptions { IsUpsert = true });
    }

    [Benchmark]
    public void BenchmarkTotalSave3()
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", 1);
        _cc2.ReplaceOne(filter, test2, new ReplaceOptions { IsUpsert = true });
    }
}