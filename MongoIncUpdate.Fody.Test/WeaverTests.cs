using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoIncUpdate.Base;
using Xunit;

namespace MongoIncUpdate.Fody.Test;

[MongoIncUpdate]
public class TestCoInt
{
    [BsonId] public int Id { get; set; }
    public int I { get; set; }
}

public class WeaverTests
{
    private readonly IMongoDatabase _db;

    public WeaverTests()
    {
        Console.WriteLine("---init test begin---");
        //创建mongo链接
        var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
        var mongoClient = new MongoClient(connectionString);
        _db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);
        //构造序列化
        Console.WriteLine("---init test end---");
    }


    //测试int更新
    [Fact]
    public void TestSampleInt()
    {
        //构造存储对象
        var cc = _db.GetCollection<TestCoInt>(nameof(TestCoInt));
        var a = new TestCoInt();
        IncUpdateExt.IncUpdate(cc, a);
        Assert.Equal(3, 3);
    }

    [Fact]
    public void TestNestedStateMap()
    {
        Assert.Equal(3, 3);
    }
}