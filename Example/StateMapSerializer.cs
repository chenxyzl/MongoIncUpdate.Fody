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
public class StateMapSerializer<TKey, TValue> : SerializerBase<StateMap<TKey, TValue>>, IBsonArraySerializer,
    IBsonDocumentSerializer
    where TKey : notnull
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
            a = a.Substring(2);
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

    public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
    {
        serializationInfo = new BsonSerializationInfo(null,
            BsonSerializer.SerializerRegistry.GetSerializer<TValue>(),
            BsonSerializer.SerializerRegistry.GetSerializer<TValue>().ValueType);
        return true;
    }
    
    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        if (memberName.StartsWith("k_"))
        {
            //k
            serializationInfo = new BsonSerializationInfo(memberName,// this, GetType());
                BsonSerializer.SerializerRegistry.GetSerializer<TValue>(),
                BsonSerializer.SerializerRegistry.GetSerializer<TValue>().ValueType);
            // serializationInfo = new BsonSerializationInfo(memberName.Substring(2), this, GetType());
            // // BsonSerializer.SerializerRegistry.GetSerializer<TKey>(),
            // // BsonSerializer.SerializerRegistry.GetSerializer<TKey>().ValueType);
        }
        else
        {
            //v
            serializationInfo = new BsonSerializationInfo(memberName,// this, GetType());
            BsonSerializer.SerializerRegistry.GetSerializer<TValue>(),
            BsonSerializer.SerializerRegistry.GetSerializer<TValue>().ValueType);
        }

        return true;
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
                var k = v.Key.ToString();
                bsonWriter.WriteName($"k_{k}");
                BsonSerializer.SerializerRegistry.GetSerializer<TValue>().Serialize(context, v.Value);
            }
        }

        bsonWriter.WriteEndDocument();
    }
}