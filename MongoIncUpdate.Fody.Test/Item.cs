using AssemblyToProcess;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoIncUpdate.Fody.Test;

[MongoIncUpdate]
public class Inner2
{
    //多层嵌套 任意测试了   
    public int I { get; set; }
    [BsonIgnore] public int XX { get; set; }
}

[MongoIncUpdate]
public class Inner1
{
    //测试嵌套的dictionary的引用类型嵌套 
    [BsonSerializer(typeof(StateMapSerializer<string, Inner2>))]
    public StateMap<string, Inner2> Dic1 { get; set; } = new();

    public Inner2 Inner2 { get; set; } = new();
}

[MongoIncUpdate]
public class Item
{
    //id
    [BsonId] public int Id { get; set; }

    //string类型带attr
    [BsonElement("RealName")] public string Name { get; set; }

    //
    //测试引用类型
    public Inner1 Inner1 { get; set; } = new();

    //
    // //测试dictionary的值类型
    [BsonSerializer(typeof(StateMapSerializer<int, string>))]
    public StateMap<int, string> Dic1 { get; set; } = new();

    //测试dictionary的引用类型嵌套
    [BsonSerializer(typeof(StateMapSerializer<string, Inner1>))]
    public StateMap<string, Inner1> Dic2 { get; set; } = new();
}