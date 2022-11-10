using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoIncUpdate.Base;

public static class IncUpdateExt
{
    static readonly ReplaceOptions ReplaceOptions = new() { IsUpsert = true };
    static readonly UpdateOptions UpdateOptions = new() { IsUpsert = true };

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

    public static async Task<UpdateResult> IncUpdate<T>(this IMongoCollection<T> collection, T item,
        bool forceReplace = false) where T : class
    {
        //获取bsonId。这种方式性能较低,但比较通用。
        var id = item.GetBsonId();
        if (id == null) throw new Exception("BsonId not exist");
        return await collection.IncUpdate(item, id, forceReplace);
    }

    public static async Task<UpdateResult> IncUpdate<T, TK>(this IMongoCollection<T> collection, T item, TK id,
        bool forceReplace = false)
        where T : class where TK : notnull
    {
        var filter = Builders<T>.Filter.Eq("_id", id);
        if (forceReplace) //强制全量替换
        {
            var result = await collection.ReplaceOneAsync(filter, item, ReplaceOptions);
            return new UpdateResult.Acknowledged(result.MatchedCount, result.ModifiedCount, result.UpsertedId);
        }

        try //优先增量更新
        {
            var diffUpdateable = item as IDiffUpdateable;
            var defs = new List<UpdateDefinition<T>>();
            diffUpdateable?.BuildUpdate(defs, "");
            //无数据更新直接跳过
            if (defs.Count == 0) return new UpdateResult.Acknowledged(1, 0, null);
            //更新
            var setter = Builders<T>.Update.Combine(defs);
            // Console.WriteLine($"update data count:{defs.Count}");
            return await collection.UpdateOneAsync(filter, setter, UpdateOptions);
        }
        catch (Exception) //增量更新失败则退化为全量替换(逻辑上没上用,增量异常了全量替换也大概率一场--仅仅是为了保证逻辑完整--)
        {
            var result = await collection.ReplaceOneAsync(filter, item, ReplaceOptions);
            return new UpdateResult.Acknowledged(result.MatchedCount, result.ModifiedCount, result.ToBson());
        }
    }
}