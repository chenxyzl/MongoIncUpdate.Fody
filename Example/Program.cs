using MongoDB.Bson;
using MongoDB.Driver;

namespace Example;

static class ItemInUpdate
{
    public static async Task SaveIm(this Item self, IMongoCollection<Item> collection)
    {
        var diffUpdateable = self as IDiffUpdateable;
        var defs = new List<UpdateDefinition<Item>>();
        diffUpdateable?.BuildUpdate(defs,"");
        if (defs.Count == 0) return;
        var setter = Builders<Item>.Update.Combine(defs);
        var filter = Builders<Item>.Filter.Eq("_id", self.Id);
        await collection.UpdateOneAsync(filter, setter, new UpdateOptions { IsUpsert = true });
    }
}

public sealed class Program
{
    public async Task Run()
    {
        /*
         * 所有的动作基于BsonDocument操作就对了
         */
        //数据库连接，格式为mongodb://账号:密码@服务器地址:端口/数据库名
        var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
        var mongoClient = new MongoClient(connectionString);
        var db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);
        var cc = db.GetCollection<Item>(nameof(Item));


        //构造查询条件 建议以模型的方式插入数据，这样子字段类型是可控的
        var item = new Item { Id = 1 };
        var filter = Builders<Item>.Filter.Eq("_id", item.Id);
        //查询老数据
        var beforList = cc.Find(filter).ToList();
        for (var i = 0; i < beforList.Count; i++) Console.WriteLine($"查询结果 {i}: " + beforList[i].ToJson());

        //修改数据
        //已经在new时候修改了
        item.Name = "newName";
        //保存数据
        await item.SaveIm(cc);

        //查询数据
        var cc1List = cc.Find(filter).ToList();
        for (var i = 0; i < cc1List.Count; i++) Console.WriteLine($"查询结果 {i}: " + cc1List[i].ToJson());
    }

    private static async Task Main(string[] args)
    {
        var a = new Program();
        await a.Run();
    }
}