using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoIncUpdate.Base;
using Xunit;
using Xunit.Abstractions;

namespace MongoIncUpdate.Fody.Test;

[MongoIncUpdate]
public sealed class ItemInt
{
    [BsonId] public int Id { get; set; }
    public int I { get; set; }
}

[MongoIncUpdate]
public sealed class ItemNestInt
{
    [BsonId] public int Id { get; set; }
    public ItemInt ItemInt { get; set; }
}

[MongoIncUpdate]
public sealed class ItemNestNestInt
{
    [BsonId] public int Id { get; set; }
    public ItemNestInt ItemNestInt { get; set; }
}

[MongoIncUpdate]
public sealed class ItemIntKeyStateMapInt
{
    [BsonId] public int Id { get; set; }

    [BsonSerializer(typeof(StateMapSerializer<int, int>))]
    public StateMap<int, int> IntInt { get; set; } = new();
}

[MongoIncUpdate]
public sealed class ItemStringKeyStateMapString
{
    [BsonId] public int Id { get; set; }

    [BsonSerializer(typeof(StateMapSerializer<string, string>))]
    public StateMap<string, string> StringString { get; set; } = new();
}

[MongoIncUpdate]
public sealed  class NestStateMapItem
{
    [BsonId] public int Id { get; set; }

    [BsonSerializer(typeof(StateMapSerializer<int, ItemInt>))]
    public StateMap<int, ItemInt> StringItemInt { get; set; } = new();
}

[MongoIncUpdate]
public sealed class NestStateMapStateMapItem
{
    [BsonId] public int Id { get; set; }
    public StateMap<int, StateMap<int, ItemInt>> StateMapStateMapItem { get; set; } = new();
}

[MongoIncUpdate]
public sealed  class ItemNestItemStateMap
{
    public ItemIntKeyStateMapInt NestItem { get; set; }
}

[MongoIncUpdate]
public sealed class ItemStateMapItemStateMapItem
{
    [BsonId] public int Id { get; set; }
    public StateMap<int, ItemNestItemStateMap> Item { get; set; } = new();
}

public partial class WeaverTests
{
    private static IMongoDatabase _db;
    private static ITestOutputHelper _output;

    private static bool _init;

    public WeaverTests(ITestOutputHelper output)
    {
        _output = output;
        lock (this)
        {
            if (_init) return;
            _init = true;
            IncUpdateExt.Register();

            _output.WriteLine("---init test begin---");
            //创建mongo链接
            var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
            var mongoClient = new MongoClient(connectionString);
            _db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);

            //
            _output.WriteLine("---init test success---");
        }
    }

    [Fact]
    public void TestHello()
    {
        var a = new ItemInt { Id = 1, I = 2 };
        _output.WriteLine(a.ToString());
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
        _ = await cc.IncUpdate(a, 1, true);
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
        a.StateMapStateMapItem.Remove(3);
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.StateMapStateMapItem.Count);
        Assert.False(result.StateMapStateMapItem.ContainsKey(3));

        _output.WriteLine("增量删除StateMap.StateMap.value");
        a.StateMapStateMapItem[1]?.Remove(111);
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Single(result.StateMapStateMapItem[1]!);
        Assert.False(result.StateMapStateMapItem[1]!.ContainsKey(111));

        _output.WriteLine("增量清空StateMap.StateMap.Clean");
        a.StateMapStateMapItem[1]?.Clear();
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Empty(result.StateMapStateMapItem[1]!);
        Assert.Equal(2, result.StateMapStateMapItem[2]!.Count);

        _output.WriteLine("增量清空StateMap.Clean");
        a.StateMapStateMapItem.Clear();
        await cc.IncUpdate(a);
        filter = Builders<NestStateMapStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Empty(result.StateMapStateMapItem);
        _output.WriteLine("---TestNestStateMapStateMapItem完成---");
    }

    [Fact]
    public async Task TestNestStateMapItemStateMapItem()
    {
        _output.WriteLine("---TestNestStateMapItemStateMapItem开始---");
        var cc = _db.GetCollection<ItemStateMapItemStateMapItem>(nameof(ItemStateMapItemStateMapItem));

        _output.WriteLine("全量写入");
        var a = new ItemStateMapItemStateMapItem
        {
            Id = 1,
            Item = new StateMap<int, ItemNestItemStateMap>
            {
                {
                    1, new ItemNestItemStateMap
                    {
                        NestItem = new ItemIntKeyStateMapInt
                        {
                            IntInt = new StateMap<int, int> { { 1, 1 }, { 11, 11 } }
                        }
                    }
                },
                {
                    2, new ItemNestItemStateMap
                    {
                        NestItem = new ItemIntKeyStateMapInt
                        {
                            IntInt = new StateMap<int, int> { { 2, 2 }, { 22, 22 } }
                        }
                    }
                }
            }
        };
        _output.WriteLine("全量写入结果检查");
        await cc.IncUpdate(a);
        var filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        var result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Equal(2, result.Item[1]!.NestItem.IntInt.Count);
        Assert.Equal(2, result.Item[2]!.NestItem.IntInt.Count);
        Assert.Equal(11, result.Item[1]!.NestItem.IntInt[11]);
        Assert.Equal(22, result.Item[2]!.NestItem.IntInt[22]);

        _output.WriteLine("Item全量覆盖测试");
        a.Item = new StateMap<int, ItemNestItemStateMap>
        {
            {
                3, new ItemNestItemStateMap
                {
                    NestItem = new ItemIntKeyStateMapInt
                    {
                        IntInt = new StateMap<int, int> { { 3, 3 }, { 33, 33 } }
                    }
                }
            },
            {
                4, new ItemNestItemStateMap
                {
                    NestItem = new ItemIntKeyStateMapInt
                    {
                        IntInt = new StateMap<int, int> { { 4, 4 }, { 44, 44 } }
                    }
                }
            }
        };
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        Assert.Equal(33, result.Item[3]!.NestItem.IntInt[33]);
        Assert.Equal(44, result.Item[4]!.NestItem.IntInt[44]);

        _output.WriteLine("v.Item[3]覆盖检查");
        a.Item[3] = new ItemNestItemStateMap
        {
            NestItem = new ItemIntKeyStateMapInt
            {
                IntInt = new StateMap<int, int> { { 333, 333 }, { 3333, 3333 } }
            }
        };
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        Assert.Equal(333, result.Item[3]!.NestItem.IntInt[333]);
        Assert.Equal(44, result.Item[4]!.NestItem.IntInt[44]);

        _output.WriteLine("v.Item[3].NestItem覆盖检查");
        a.Item[4]!.NestItem = new ItemIntKeyStateMapInt
        {
            IntInt = new StateMap<int, int> { { 444, 444 }, { 4444, 4444 } }
        };
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        Assert.Equal(3333, result.Item[3]!.NestItem.IntInt[3333]);
        Assert.Equal(4444, result.Item[4]!.NestItem.IntInt[4444]);

        _output.WriteLine("v.Item[3].NestItem.IntInt覆盖检查");
        a.Item[4]!.NestItem.IntInt = new StateMap<int, int> { { 44444, 44444 }, { 444444, 444444 } };
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        Assert.Equal(3333, result.Item[3]!.NestItem.IntInt[3333]);
        Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);

        _output.WriteLine("v.Item[3].NestItem.IntInt[3333]覆盖检查");
        a.Item[3]!.NestItem.IntInt[3333] = 3;
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        Assert.Equal(3, result.Item[3]!.NestItem.IntInt[3333]);
        Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);

        _output.WriteLine("v.Item[3].NestItem.IntInt.remove(3333)删除检查");
        a.Item[3]!.NestItem.IntInt.Remove(3333);
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Single(result.Item[3]!.NestItem.IntInt);
        Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        Assert.False(result.Item[3]!.NestItem.IntInt.ContainsKey(3333));
        Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);

        _output.WriteLine("v.Item[3].NestItem.IntInt.Clean()删除检查");
        a.Item[3]!.NestItem.IntInt.Clear();
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Equal(2, result.Item.Count);
        Assert.Empty(result.Item[3]!.NestItem.IntInt);
        Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);

        _output.WriteLine("v.Item.Clean()删除检查");
        a.Item.Clear();
        await cc.IncUpdate(a);
        filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        result = (await cc.FindAsync(filter)).First();
        Assert.Empty(result.Item);

        _output.WriteLine("---TestNestStateMapItemStateMapItem完成---");
    }
}