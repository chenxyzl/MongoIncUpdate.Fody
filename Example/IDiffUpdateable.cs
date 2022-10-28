using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using AssemblyToProcess;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Example;

[MongoIncUpdateInterface]
public interface IDiffUpdateable
{
    protected BitArray Dirties { get; set; }

    protected Dictionary<int, IPropertyCallAdapter> IdxMapping { get; set; }
    protected Dictionary<string, int> NameMapping { get; set; }

    //fody调用 插入到实现类的构造函数中
    void Init()
    {
        IdxMapping = new();
        NameMapping = new();
        //生成map
        var props = GetType().GetProperties();
        var idx = 0;
        foreach (var prop in props)
        {
            if (prop.GetCustomAttributes<BsonIdAttribute>(true).Any()) continue;
            if (prop.GetCustomAttributes<BsonIgnoreAttribute>(true).Any()) continue;
            IdxMapping[idx] = PropertyCallAdapterProvider.GetInstance(this, prop);
            NameMapping[prop.Name] = idx;
            idx++;
        }
 
        //重新设置大小
        Dirties = new BitArray(IdxMapping.Count);
    }

    //fody触发 可以考虑在在setter直接插入 dirty(写在c#里则要灵活一些)
    static void PropChange(object? sender, string propName)
    {
        if (sender is IDiffUpdateable self && self.NameMapping.TryGetValue(propName, out var idx))
        {
            self.Dirties[idx] = true;
        }
    }

    void BuildUpdate<T>(List<UpdateDefinition<T>> defs, string key)
        // void BuildUpdate<T>(List<UpdateDefinition<T>> defs, object[] obj)
    {
        // var key = obj[0] as string;
        var update = Builders<T>.Update;
        for (int i = 0; i < Dirties.Length; i++)
        {
            if (Dirties.Get(i) && IdxMapping.TryGetValue(i, out var prop))
            {
                //更新
                //object如果继承了IDiffUpdateable 调用object.IDiffUpdateable。 否者直接调用
                //var v = prop.GetValue(this, null); GetValue性能太低了
                var v = prop.InvokeGet(this);
                if (v is IDiffUpdateable sv)
                {
                    // sv.BuildUpdate(defs, new[] { MakeKey(prop.PropName(), key) });
                    sv.BuildUpdate(defs, MakeKey(prop.PropName(), key));
                }
                else
                {
                    defs.Add(update.Set(MakeKey(prop.PropName(), key), v));
                }
            }
        }

        Dirties.SetAll(false);
    }

    static string MakeKey(string name, string? key)
    {
        if (string.IsNullOrEmpty(key)) return name;

        var builder = new DefaultInterpolatedStringHandler();
        builder.AppendLiteral(key);
        builder.AppendLiteral(".");
        builder.AppendLiteral(name);

        return builder.ToStringAndClear();
    }
}