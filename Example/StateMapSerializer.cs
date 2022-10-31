using System.Collections;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Example;

//MongoKeyParse 负责key值的转换
public static class MongoKeyParse
{
    public static T ToEnum<T>(this string str)
    {
        return (T)Enum.Parse(typeof(T), str);
    }

    public static T ChangeType<T>(this string obj)
    {
        return (T)Convert.ChangeType(obj, typeof(T));
    }
}

//StateMapSerializer 自定义序列化
// mongo中实现k,v存储
// 使用方式
/*
internal class TestDoc
{
    // [BsonStateMapOptions(StateMapRepresentation.ArrayOfDocuments)]
    // public StateMap<ulong, Item> Items = new();
    
    [BsonId] public string Id { get; set; }
}
*/
public class StateMapSerializer<TKey, TValue> : SerializerBase<StateMap<TKey, TValue>> where TKey : notnull
{
    public override StateMap<TKey, TValue> Deserialize(BsonDeserializationContext context,
        BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;

        var ret = new StateMap<TKey, TValue>();
        bsonReader.ReadStartDocument();
        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
        {
            var a = bsonReader.ReadName();
            TKey key;

            if (typeof(TKey).IsEnum)
                key = a.ToEnum<TKey>();
            else
                key = a.ChangeType<TKey>();

            var b = BsonSerializer.SerializerRegistry.GetSerializer<TValue>().Deserialize(context);
            bool c = ret.TryAdd(key, b);
            if (c == false)
            {
                throw new BsonSerializationException("Deserialize error");
            }
        }

        bsonReader.ReadEndDocument();

        return ret;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args,
        StateMap<TKey, TValue>? value)
    {
        var bsonWriter = context.Writer;
        bsonWriter.WriteStartDocument();
        if (value != null)
        {
            foreach (var v in value)
            {
                bsonWriter.WriteName(v.Key.ToString());
                BsonSerializer.SerializerRegistry.GetSerializer<TValue>().Serialize(context, v.Value);
            }
        }

        bsonWriter.WriteEndDocument();
    }
}