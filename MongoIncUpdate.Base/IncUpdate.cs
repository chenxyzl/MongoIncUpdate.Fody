using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoIncUpdate.Base;

public static class IncUpdateExt
{
    public static object? GetBsonId<T>(this T self) where T : class
    {
        var find = typeof(BsonIdAttribute);
        var props = self.GetType().GetProperties();
        foreach (var v in props)
        foreach (var attr in v.GetCustomAttributes(false))
            if (attr.GetType().FullName == find.FullName)
                return v.GetValue(self);
        return null;
    }

    public static async Task<UpdateResult> IncUpdate<T>(this IMongoCollection<T> collection, T item) where T : class
    {
        //获取bsonId。这种方式性能较低,但比较通用。
        var id = item.GetBsonId();
        if (id == null) throw new Exception("BsonId not exist");

        var diffUpdateable = item as IDiffUpdateable;
        var defs = new List<UpdateDefinition<T>>();
        diffUpdateable?.BuildUpdate(defs, "");
        //无数据更新直接跳过
        if (defs.Count == 0) return new UpdateResult.Acknowledged(0, 0, 0);
        //更新
        var setter = Builders<T>.Update.Combine(defs);
        var filter = Builders<T>.Filter.Eq("_id", id);
        Console.WriteLine($"update data count:{defs.Count}");
        return await collection.UpdateOneAsync(filter, setter, new UpdateOptions { IsUpsert = true });
    }

    public static async Task<UpdateResult> IncUpdate<T, TK>(this IMongoCollection<T> collection, T item, TK id)
        where T : class
    {
        var diffUpdateable = item as IDiffUpdateable;
        var defs = new List<UpdateDefinition<T>>();
        diffUpdateable?.BuildUpdate(defs, "");
        //无数据更新直接跳过
        if (defs.Count == 0) return new UpdateResult.Acknowledged(0, 0, 0);
        //更新
        var setter = Builders<T>.Update.Combine(defs);
        var filter = Builders<T>.Filter.Eq("_id", id);
        Console.WriteLine($"update data count:{defs.Count}");
        return await collection.UpdateOneAsync(filter, setter, new UpdateOptions { IsUpsert = true });
    }
}