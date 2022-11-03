# PropertyChangeWatch.Fody
# mongo增量方案
1. 给所有属性在编译器静态注入脏标记
2. mongo存储时候检查藏标记来生成UpdateDefinition,并执行UpdateOneAsync保存

## 实现增量更新的脏标记注入
1. 引用Fody包,增加FodyWeavers.xml,配置导入MongoIncUpdate(MongoIncUpdate工程为Fody的静态代码编织插件,增量方案的代码注入在这里实现)
2. MongoIncUpdate工程的增加类ModuleWeaver(继承BaseModuleWeaver),会被fody调用(原理就是msbuild会在编译以前扫描引入的包的.targets文件,并执行其中的task,详情看《07_.net fody.md》)
    1. ModuleWeave实现扫描标记为MongoIncUpdateAttribute为等待被注入的对象
    2. ModuleWeave实现扫描标记为MongoIncUpdateInterfaceAttribute为被注入的接口(基于插件模型)
    3. 对每个MongoIncUpdateAttribute标记的对象分别注入MongoIncUpdateInterfaceAttribute标记的接口
    4. 对每个MongoIncUpdateAttribute标记的对象分别注入接口的属性(Dirties,IdxMapping,NameMapping)实现
    5. 对每个MongoIncUpdateAttribute标记的对象分别注入_dirties,_idxMapping,_nameMapping字段
    6. 对每个MongoIncUpdateAttribute标记的对象分别注入set/get_Dirties,set/get_IdxMapping,set/get_NameMapping字段
    7. 对上述注入的setter和getter分别注入消息体,并在setter中注入调用MongoIncUpdateInterfaceAttribute标记的插件类的静态方法PropChange。实现了属性变化通知
    8. 扫描对象的所有ctor方法,注入调用MongoIncUpdateInterfaceAttribute标记的插件类的实例方法Init
3. 实现StateMap和StateMapSerializer增加对Dictionary的支持

## Mongo增量的存储过程
1. 存储对象调用MongoIncUpdateInterfaceAttribute注入的接口的BuildUpdate来生成UpdateDefinition
2. BuildUpdate中for循环遍历脏标记,更具标记的位置来获取对象的值和类型相关属性
3. 如果是脏则整体存储,如果不是脏则检查是否是引用类型(string特殊引用类型除外),如果类型没有被注入插件接口则退化为整体存储(保底措施),有递归调用BuildUpdate
4. 增加StateMap(继承至MongoIncUpdateInterfaceAttribute标记的接口)来支持集合类型,(实现了StateMapSerializer来支持序列化反序列化)   
   这里注意dic[key]=value时候,mongo的序列化有个大坑
   1. 在StringFieldDefinitionHelper.Resolve获取类型时候对于正常的obj.property = value中, value的类型是来至于obj的member中同名property的类型获取出来的。
   2. 在obj[key]=value中。也会变为obj.key=value形式,走上述类型推断逻辑,会变成获取obj中成员同名为key的类型,而字典的key最终都为string类型。接下来又两个错误   
        1. key为全部数值类型,尝试转化为IBsonArraySerializer且失败,直接跳出类型推断代码,在上层退化为默认类型。
        2. 非全数值类型,尝试转化为IBsonDocumentSerializer类型,而key一般为string类型,转换失败,直接跳出类型推断代码,在上层退化为默认类型。
   3. 解决方法
        1. StateMapSerializer的序列化时候把key转换为k_key的形式,避免上述中全数值类型被终止类型插件   
        2. 接着实现IBsonDocumentSerializer接口,在TryGetMemberSerializationInfo函数中返回value的序列化器。key的序列化器是StateMapSerializer<TKey, TValue>,不会打断递归,且key会被检查继承关系,检查失败会退化为原生类型,能保证key正常序列化。对于value:此操作类似把value当作了key的成员类型。
5. 调用await collection.UpdateOneAsync(filter, setter, new UpdateOptions { IsUpsert = true });来保存

## 如何使用
### 1.存储实体构造(需要保证成员都是属性,即为get;set----这里增加了编译期检查如果不需要这个现在就自行去掉)
``` C#
[MongoIncUpdate]
public class Inner2
{
    //多层嵌套 任意测试了  
    public int I { get; set; }
    [BsonIgnore]
    public int XX { get; set; }
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
    [BsonSerializer(typeof(StateMapSerializer<int, int>))]
    public StateMap<int, int> Dic1 { get; set; } = new();

    //测试dictionary的引用类型嵌套
    [BsonSerializer(typeof(StateMapSerializer<string, Inner1>))]
    public StateMap<string, Inner1> Dic2 { get; set; } = new();
}
```

### 2.保存数据
```c#
public static async Task SaveIm(this Item self, IMongoCollection<Item> collection)
    {
        var diffUpdateable = self as IDiffUpdateable;
        var defs = new List<UpdateDefinition<Item>>();
        diffUpdateable?.BuildUpdate(defs, "");
        if (defs.Count == 0) return;
        var setter = Builders<Item>.Update.Combine(defs);
        var filter = Builders<Item>.Filter.Eq("_id", self.Id);
        await collection.UpdateOneAsync(filter, setter, new UpdateOptions { IsUpsert = true });
        Console.WriteLine($"update data count:{defs.Count}");
    }
```
