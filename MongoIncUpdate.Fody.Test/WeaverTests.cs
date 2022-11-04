using AssemblyToProcess;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace MongoIncUpdate.Fody.Test;

[MongoIncUpdate]
public class ItemInt
{
    [BsonId] public int Id { get; set; }
    public int I { get; set; }
}

[MongoIncUpdate]
public class ItemNestInt
{
    [BsonId] public int Id { get; set; }
    public ItemInt ItemInt { get; set; }
}

[MongoIncUpdate]
public class ItemNestNestInt
{
    [BsonId] public int Id { get; set; }
    public ItemNestInt ItemNestInt { get; set; }
}

[MongoIncUpdate]
public class ItemIntKeyStateMapInt
{
    [BsonId] public int Id { get; set; }
    
    [BsonSerializer(typeof(StateMapSerializer<int, int>))]
    public StateMap<int, int> IntInt { get; set; } = new();
}

[MongoIncUpdate]
public class ItemStringKeyStateMapString
{
    [BsonId] public int Id { get; set; }
    
    [BsonSerializer(typeof(StateMapSerializer<string, string>))]
    public StateMap<string, string> StringString { get; set; } = new();
}


public class WeaverTests
{
    private readonly IMongoDatabase _db;
    private ITestOutputHelper _output;

    public WeaverTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("---init test begin---");
        //创建mongo链接
        var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
        var mongoClient = new MongoClient(connectionString);
        _db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);
        //构造序列化
        _output.WriteLine("---init test success---");
    }


    //测试int更新
    [Fact]
    public async Task TestSampleInt()
    {
        _output.WriteLine("---obj.int增量更新测试开始---");
        //构造存储对象
        _output.WriteLine("全量写入");
        var cc = _db.GetCollection<ItemInt>(nameof(ItemInt));
        var a = new ItemInt { Id = 1, I = 2 };
        await cc.IncUpdate(a);

        _output.WriteLine("增量更新obj.init");
        var filer = Builders<ItemInt>.Filter.Eq(x => x.Id, a.Id);
        a.I = 22;
        await cc.IncUpdate(a);

        _output.WriteLine("获取完整对象");
        var result = (await cc.FindAsync(filer)).First();
        Assert.Equal(result.I, a.I);
        //
        _output.WriteLine("---obj.int增量更新测试完成---");
    }

    [Fact]
    public async Task TestNestedItem()
    {
        _output.WriteLine("---obj.nest.int增量更新测试开始---");
        //构造存储对象
        _output.WriteLine("全量写入");
        var cc = _db.GetCollection<ItemNestInt>(nameof(ItemNestInt));
        var a = new ItemNestInt { Id = 1, ItemInt = new ItemInt { I = 4 } };
        await cc.IncUpdate(a);

        _output.WriteLine("增量更新obj.nest.int");
        a.ItemInt.I = 44;
        var filer = Builders<ItemNestInt>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        var result = (await cc.FindAsync(filer)).First();
        Assert.Equal(result.ItemInt.I, a.ItemInt.I);
        //
        _output.WriteLine("---obj.nest.int增量更新测试完成---");
    }

    [Fact]
    public async Task TestNestNestedItem()
    {
        _output.WriteLine("---obj.nest.nest.int增量更新测试开始---");
        //构造存储对象
        _output.WriteLine("全量写入");
        var cc = _db.GetCollection<ItemNestNestInt>(nameof(ItemNestNestInt));
        var a = new ItemNestNestInt { Id = 1, ItemNestInt = new ItemNestInt { ItemInt = new ItemInt { I = 5 } } };
        await cc.IncUpdate(a);

        _output.WriteLine("增量更新obj.nest.nest.int");
        a.ItemNestInt.ItemInt.I = 55;
        var filer = Builders<ItemNestNestInt>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        var result = (await cc.FindAsync(filer)).First();
        Assert.Equal(result.ItemNestInt.ItemInt.I, a.ItemNestInt.ItemInt.I);
        //
        _output.WriteLine("---obj.nest.nest.int增量更新测试完成---");
    }

    [Fact]
    public async Task TestIntKeyStateMapInt()
    {
        _output.WriteLine("---TestIntKeyStateMapInt开始---");
        var cc = _db.GetCollection<ItemIntKeyStateMapInt>(nameof(ItemIntKeyStateMapInt));
        
        _output.WriteLine("全量写入");
        var a = new ItemIntKeyStateMapInt { Id = 1, IntInt = new StateMap<int, int> { { 1, 1 }, { 2, 2 } } };
        await cc.IncUpdate(a);
        
        _output.WriteLine("全量写入结果检查");
        var filter = Builders<ItemIntKeyStateMapInt>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        var result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.IntInt.Count, result.IntInt.Count);
        Assert.Equal(2, result.IntInt.Count);
        Assert.Equal(1, result.IntInt[1]);
        Assert.Equal(result.IntInt[1], result.IntInt[1]);
        Assert.Equal(2, result.IntInt[2]);
        Assert.Equal(result.IntInt[2], result.IntInt[2]);
        
        _output.WriteLine("增量覆盖写入StateMap[IntInt]整体");
        a.IntInt = new StateMap<int, int> { { 11, 11 }, { 22, 22 } };
        await cc.IncUpdate(a);
        filter = Builders<ItemIntKeyStateMapInt>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.IntInt.Count, result.IntInt.Count);
        Assert.Equal(2, result.IntInt.Count);
        Assert.Equal(11, result.IntInt[11]);
        Assert.Equal(result.IntInt[11], result.IntInt[11]);
        Assert.Equal(22, result.IntInt[22]);
        Assert.Equal(result.IntInt[22], result.IntInt[22]);
        
        _output.WriteLine("增量修改StateMap.value[IntInt.Int]");
        a.IntInt[11] = 111;
        filter = Builders<ItemIntKeyStateMapInt>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.IntInt.Count, result.IntInt.Count);
        Assert.Equal(2, result.IntInt.Count);
        Assert.Equal(111, result.IntInt[11]);
        
        
        _output.WriteLine("增量增加StateMap.key.value[IntInt.Int.Int]");
        a.IntInt[33] = 33;
        filter = Builders<ItemIntKeyStateMapInt>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(3, result.IntInt.Count);
        Assert.Equal(33, result.IntInt[33]);
        
        _output.WriteLine("---TestIntKeyStateMapInt完成---");
    }

    [Fact]
    public async Task TestStringKeyStateMapString()
    {
        _output.WriteLine("---TestStringKeyStateMapString开始---");
        var cc = _db.GetCollection<ItemStringKeyStateMapString>(nameof(ItemStringKeyStateMapString));
        
        _output.WriteLine("全量写入");
        var a = new ItemStringKeyStateMapString { Id = 1, StringString = new StateMap<string, string> { { "1", "1" }, { "2", "2" } } };
        await cc.IncUpdate(a);
        
        _output.WriteLine("全量写入结果检查");
        var filter = Builders<ItemStringKeyStateMapString>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        var result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.StringString.Count, result.StringString.Count);
        Assert.Equal(2, result.StringString.Count);
        Assert.Equal("1", result.StringString["1"]);
        Assert.Equal(result.StringString["1"], result.StringString["1"]);
        Assert.Equal("2", result.StringString["2"]);
        Assert.Equal(result.StringString["2"], result.StringString["2"]);
        
        _output.WriteLine("增量覆盖写入StateMap[StringString]整体");
        a.StringString = new StateMap<string, string> { { "11", "11" }, { "22", "22" } };
        await cc.IncUpdate(a);
        filter = Builders<ItemStringKeyStateMapString>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.StringString.Count, result.StringString.Count);
        Assert.Equal(2, result.StringString.Count);
        Assert.Equal("11", result.StringString["11"]);
        Assert.Equal(result.StringString["11"], result.StringString["11"]);
        Assert.Equal("22", result.StringString["22"]);
        Assert.Equal(result.StringString["22"], result.StringString["22"]);
        
        _output.WriteLine("增量修改StateMap.value[StringString.String]");
        a.StringString["11"] = "111";
        filter = Builders<ItemStringKeyStateMapString>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.StringString.Count, result.StringString.Count);
        Assert.Equal(2, result.StringString.Count);
        Assert.Equal("111", result.StringString["11"]);
        
        
        _output.WriteLine("增量增加StateMap.key.value[StringString.String.String]");
        a.StringString["33"] = "33";
        filter = Builders<ItemStringKeyStateMapString>.Filter.Eq(x => x.Id, a.Id);
        await cc.IncUpdate(a);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(3, result.StringString.Count);
        Assert.Equal("33", result.StringString["33"]);
        
        _output.WriteLine("---TestStringKeyStateMapString完成---");
    }

    [Fact]
    public async Task TestNestStateMapItem()
    {
    }

    [Fact]
    public async Task TestNestStateMapStateMapItem()
    {
    }

    [Fact]
    public async Task TestNestStateMapItemStateMapItem()
    {
    }
}