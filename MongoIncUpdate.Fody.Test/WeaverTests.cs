using AssemblyToProcess;
using MongoDB.Bson.Serialization;
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

[MongoIncUpdate]
public class NestStateMapItem
{
    [BsonId] public int Id { get; set; }

    [BsonSerializer(typeof(StateMapSerializer<int, ItemInt>))]
    public StateMap<int, ItemInt> StringItemInt { get; set; } = new();
}

[MongoIncUpdate]
public class NestStateMapStateMapItem
{
    [BsonId] public int Id { get; set; }

    [BsonSerializer(typeof(StateMapSerializer<int, StateMap<int, ItemInt>>))]
    public StateMap<int, StateMap<int, ItemInt>> StateMapStateMapItem { get; set; } = new();
}

public class WeaverTests
{
    private readonly IMongoDatabase _db;
    private ITestOutputHelper _output;

    public WeaverTests(ITestOutputHelper output)
    {
        BsonSerializer.RegisterGenericSerializerDefinition(typeof(StateMap<,>), typeof(StateMapSerializer<,>));

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

        _output.WriteLine("全量写入检查");
        var filer = Builders<ItemInt>.Filter.Eq(x => x.Id, a.Id);
        var result = (await cc.FindAsync(filer)).First();
        Assert.Equal(result.I, a.I);

        _output.WriteLine("增量更新obj.init");
        a.I = 22;
        await cc.IncUpdate(a);

        _output.WriteLine("获取完整对象");
        result = (await cc.FindAsync(filer)).First();
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

        //全量写入检查
        _output.WriteLine("全量写入检查");
        var filer = Builders<ItemNestInt>.Filter.Eq(x => x.Id, a.Id);
        var result = (await cc.FindAsync(filer)).First();
        Assert.Equal(result.ItemInt.I, a.ItemInt.I);

        _output.WriteLine("增量更新obj.nest.int");
        a.ItemInt.I = 44;
        await cc.IncUpdate(a);
        filer = Builders<ItemNestInt>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filer)).First();
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
        await cc.IncUpdate(a);
        var filer = Builders<ItemNestNestInt>.Filter.Eq(x => x.Id, a.Id);
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
        await cc.IncUpdate(a);
        filter = Builders<ItemIntKeyStateMapInt>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.IntInt.Count, result.IntInt.Count);
        Assert.Equal(2, result.IntInt.Count);
        Assert.Equal(111, result.IntInt[11]);


        _output.WriteLine("增量增加StateMap.key.value[IntInt.Int.Int]");
        a.IntInt[33] = 33;
        await cc.IncUpdate(a);
        filter = Builders<ItemIntKeyStateMapInt>.Filter.Eq(x => x.Id, a.Id);
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
        var a = new ItemStringKeyStateMapString
            { Id = 1, StringString = new StateMap<string, string> { { "1", "1" }, { "2", "2" } } };
        await cc.IncUpdate(a);

        _output.WriteLine("全量写入结果检查");
        var filter = Builders<ItemStringKeyStateMapString>.Filter.Eq(x => x.Id, a.Id);
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
        await cc.IncUpdate(a);
        filter = Builders<ItemStringKeyStateMapString>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(result.StringString.Count, result.StringString.Count);
        Assert.Equal(2, result.StringString.Count);
        Assert.Equal("111", result.StringString["11"]);


        _output.WriteLine("增量增加StateMap.key.value[StringString.String.String]");
        a.StringString["33"] = "33";
        await cc.IncUpdate(a);
        filter = Builders<ItemStringKeyStateMapString>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(3, result.StringString.Count);
        Assert.Equal("33", result.StringString["33"]);

        _output.WriteLine("---TestStringKeyStateMapString完成---");
    }

    [Fact]
    public async Task TestNestStateMapItem()
    {
        _output.WriteLine("---TestNestStateMapItem开始---");
        var cc = _db.GetCollection<NestStateMapItem>(nameof(NestStateMapItem));

        _output.WriteLine("全量写入");
        var a = new NestStateMapItem
        {
            Id = 1,
            StringItemInt = new StateMap<int, ItemInt> { { 1, new ItemInt { I = 1 } }, { 2, new ItemInt { I = 2 } } }
        };
        await cc.IncUpdate(a);

        _output.WriteLine("全量写入结果检查");
        var filter = Builders<NestStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        var result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StringItemInt.Count);
        Assert.Equal(1, result.StringItemInt[1]!.I);
        Assert.Equal(2, result.StringItemInt[2]!.I);

        _output.WriteLine("StateMap全量覆盖检查结果");
        a.StringItemInt = new StateMap<int, ItemInt> { { 11, new ItemInt { I = 11 } }, { 22, new ItemInt { I = 22 } } };
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StringItemInt.Count);
        Assert.Equal(11, result.StringItemInt[11]!.I);
        Assert.Equal(22, result.StringItemInt[22]!.I);

        _output.WriteLine("StateMap增加Item测试");
        a.StringItemInt.TryAdd(3, new ItemInt { I = 3 });
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(3, result.StringItemInt.Count);
        Assert.Equal(3, result.StringItemInt[3]!.I);

        _output.WriteLine("StateMap修改Item测试");
        a.StringItemInt[3]!.I = 33;
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(3, result.StringItemInt.Count);
        Assert.Equal(33, result.StringItemInt[3]!.I);

        _output.WriteLine("StateMap删除Item测试");
        a.StringItemInt.Remove(3);
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StringItemInt.Count);
        Assert.False(result.StringItemInt.ContainsKey(3));
        _output.WriteLine("---TestNestStateMapItem完成---");
    }

    [Fact]
    public async Task TestNestStateMapStateMapItem()
    {
        _output.WriteLine("---TestNestStateMapStateMapItem开始---");
        var cc = _db.GetCollection<NestStateMapStateMapItem>(nameof(NestStateMapStateMapItem));

        _output.WriteLine("全量写入");
        var a = new NestStateMapStateMapItem
        {
            Id = 1,
            StateMapStateMapItem = new StateMap<int, StateMap<int, ItemInt>>
            {
                { 1, new StateMap<int, ItemInt> { { 1, new ItemInt { I = 1 } }, { 11, new ItemInt { I = 11 } } } },
                { 2, new StateMap<int, ItemInt> { { 2, new ItemInt { I = 2 } }, { 22, new ItemInt { I = 22 } } } }
            }
        };

        _output.WriteLine("全量写入结果检查");
        await cc.IncUpdate(a);
        var filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        var result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StateMapStateMapItem.Count);
        Assert.Equal(22, result.StateMapStateMapItem[2]![22]!.I);


        _output.WriteLine("增量修改StateMap.value");
        a.StateMapStateMapItem[1] = new StateMap<int, ItemInt>
            { { 111, new ItemInt { I = 111 } }, { 1111, new ItemInt { I = 1111 } } };
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StateMapStateMapItem.Count);
        Assert.Equal(2, result.StateMapStateMapItem[1]!.Count);
        Assert.Equal(1111, result.StateMapStateMapItem[1]?[1111]!.I);

        _output.WriteLine("增量修改StateMap.StateMap.value");
        a.StateMapStateMapItem[2]![2] = new ItemInt { I = 222 };
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StateMapStateMapItem[2]!.Count);
        Assert.Equal(222, result.StateMapStateMapItem[2]![2]!.I);

        _output.WriteLine("增量修改StateMap.StateMap.value.prop");
        a.StateMapStateMapItem[2]![22]!.I = 2222;
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StateMapStateMapItem.Count);
        Assert.Equal(2222, result.StateMapStateMapItem[2]![22]!.I);

        _output.WriteLine("增量增加StateMap.value");
        a.StateMapStateMapItem[3] = new StateMap<int, ItemInt>
            { { 3, new ItemInt { I = 3 } }, { 33, new ItemInt { I = 33 } } };
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(3, result.StateMapStateMapItem.Count);
        Assert.Equal(33, result.StateMapStateMapItem[3]![33]!.I);

        _output.WriteLine("增量增加StateMap.StateMap.value");
        a.StateMapStateMapItem[1]![111] = new ItemInt { I = 11111 };
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(3, result.StateMapStateMapItem.Count);
        Assert.Equal(2, result.StateMapStateMapItem[1]!.Count);
        Assert.Equal(11111, result.StateMapStateMapItem[1]![111]!.I);

        _output.WriteLine("增量删除StateMap.value");
        _output.WriteLine("增量删除StateMap.StateMap.value");

        _output.WriteLine("增量清空StateMap.Clean");
        _output.WriteLine("增量清空StateMap.StateMap.Clean");

        _output.WriteLine("---TestNestStateMapStateMapItem完成---");
    }

    [Fact]
    public async Task TestNestStateMapItemStateMapItem()
    {
    }
}