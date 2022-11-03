using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoIncUpdate.Base;

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
    // [BsonSerializer(typeof(StateMapSerializer<ulong, Item>))]
    // public StateMap<ulong, Item> Items = new();
    
    [BsonId] public string Id { get; set; }
}
*/
public class StateMapSerializer<TKey, TValue> : SerializerBase<StateMap<TKey, TValue>>, IBsonDocumentSerializer
    where TKey : notnull
{
    //应该直接传递value的序列化器。key的序列化器是StateMapSerializer<TKey, TValue>,不会打断递归,且key会被检查继承关系,检查失败会退化为原生类型。
    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        serializationInfo = new BsonSerializationInfo(memberName, // this, GetType());
            BsonSerializer.SerializerRegistry.GetSerializer<TValue>(),
            BsonSerializer.SerializerRegistry.GetSerializer<TValue>().ValueType);
        return true;
    }

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
            var c = ret.TryAdd(key, b);
            if (c == false) throw new BsonSerializationException("Deserialize error");
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
            foreach (var v in value)
            {
                var k = v.Key.ToString();
                bsonWriter.WriteName($"k_{k}");
                BsonSerializer.SerializerRegistry.GetSerializer<TValue>().Serialize(context, v.Value);
            }

        bsonWriter.WriteEndDocument();
    }
}