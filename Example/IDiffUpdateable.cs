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
    [BsonIgnore] protected BitArray Dirties { get; set; }

    //todo 考虑实现为对象的静态成员变量
    [BsonIgnore] protected Dictionary<int, IPropertyCallAdapter> IdxMapping { get; set; }

    [BsonIgnore]
    //todo 考虑实现为对象的静态成员变量
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
            //检查是否存在字段映射
            if (!IdxMapping.TryGetValue(i, out var prop))
            {
                continue;
            }

            //有脏标记,全量更新(证明被重新赋值了,不用区分值类型和引用类型)
            object? v;
            if (Dirties.Get(i))
            {
                v = prop.InvokeGet(this);
                defs.Add(v == null
                    ? update.Unset(MakeKey(prop.PropName(), key))
                    : update.Set(MakeKey(prop.PropName(), key), v)); //构造更新
                if (v is IDiffUpdateable df) df.CleanDirties(); //清除脏标记
                continue;
            }

            //无脏标记(只需要考虑子对象了,只有引用类型才有子对象)
            if (prop.IsDirectType()) continue;
            v = prop.InvokeGet(this);
            if (v == null) continue; //null类型只能被setter,会触发藏标记前面已经拦截了
            if (v is IDiffUpdateable sv)
            {
                sv.BuildUpdate(defs, MakeKey(prop.PropName(), key));
            }
            else
            {
                defs.Add(v == null
                    ? update.Unset(MakeKey(prop.PropName(), key))
                    : update.Set(MakeKey(prop.PropName(), key), v));
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

    void CleanDirties() //直接写入时候才调用这个,提高性能
    {
        foreach (var (propName, i) in NameMapping)
        {
            if (IdxMapping.TryGetValue(i, out var prop)) continue;
            var v = prop?.InvokeGet(this);
            if (v is IDiffUpdateable df) df.CleanDirties(); //清除脏标记
        }
        Dirties.SetAll(false);
    }
}