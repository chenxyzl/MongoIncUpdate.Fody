using System.Collections;
using MongoDB.Driver;
using MongoIncUpdate.Fody.Test;

namespace MongoIncUpdate.Fody.Test;

public class StateMap<K, V> : Dictionary<K, V>, IDiffUpdateable where K : notnull
{
    private readonly Dictionary<K, bool> _deleteMap = new();

    public V? this[K key]
    {
        get => base[key];
        set
        {
            _deleteMap.Remove(key);
            base[key] = value;
        }
    }


    public void BuildUpdate<T>(List<UpdateDefinition<T>> defs, string? key)
    {
        var update = Builders<T>.Update;
        //先删除
        foreach (var (k, v) in _deleteMap) defs.Add(update.Unset(IDiffUpdateable.MakeKey($"k_{k}", key)));
        // defs.Add(update.Set(IDiffUpdateable.MakeKey($"k_{k}", key), default(V)));
        //后修改
        foreach (var (k, v) in this)
            if (v is IDiffUpdateable v1)
                v1.BuildUpdate(defs, IDiffUpdateable.MakeKey($"k_{k}", key));
            else
                defs.Add(update.Set(IDiffUpdateable.MakeKey($"k_{k}", key), v));
    }

    //CleanDirties 必须是public。否则IDiffUpdateable调用不到这里
    public void CleanDirties() //直接写入时候才调用这个,提高性能
    {
        foreach (var (_, v) in this)
            if (v is IDiffUpdateable df)
                df.CleanDirties(); //清除脏标记
    }

    public new bool Remove(K key)
    {
        _deleteMap[key] = true;
        return base.Remove(key);
    }

    public new void Add(K key, V v)
    {
        _deleteMap.Remove(key);
        base.Add(key, v);
    }

    public new bool TryAdd(K key, V v)
    {
        _deleteMap.Remove(key);
        return base.TryAdd(key, v);
    }

    public new void Clear()
    {
        foreach (var (k, _) in this) _deleteMap[k] = true;

        base.Clear();
    }


    #region 这些字段没什么用 仅仅为了编译过。

    BitArray IDiffUpdateable.Dirties { get; set; } = new(0);
    Dictionary<int, IPropertyCallAdapter> IDiffUpdateable.IdxMapping { get; set; } = new();
    Dictionary<string, int> IDiffUpdateable.NameMapping { get; set; } = new();

    #endregion
}