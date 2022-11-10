using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;
using MongoIncUpdate.Base;
using Xunit;

namespace MongoIncUpdate.Fody.Test;

[MongoIncUpdate]
public sealed class DirtyItem
{
    public int Int { get; set; } //0
    public float Flo { get; set; } //1
    public double? Dou { get; set; } //2 
    public string? Str { get; set; } //3
}

[MongoIncUpdate]
public sealed class DirtyNestItem
{
    [BsonId] public int Id { get; set; }

    public DirtyItem Item { get; set; } //0

    [BsonSerializer(typeof(StateMapSerializer<int, DirtyItem>))]
    public StateMap<int, DirtyItem> StateMap { get; set; } //1
}

public partial class WeaverTests
{
    private static BitArray? GetDirtiesFromObject(object a)
    {
        var filed = a.GetType().GetField("_dirties", BindingFlags.NonPublic | BindingFlags.Instance);
        var dirties = filed.GetValue(a) as BitArray;
        return dirties;
    }

    private static HashSet<K>? GetDirtySetFromObject<K, V>(StateMap<K, V> a) where K : notnull
    {
        var filed = a.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .First(v => v.Name == "_dirtySet");
        var dirties = filed.GetValue(a) as HashSet<K>;
        return dirties;
    }

    [Fact]
    private void DirtyTest()
    {
        _output.WriteLine("---DirtyTest---");
        _output.WriteLine("全量赋值脏检查");
        var a = new DirtyNestItem
        {
            Item = new DirtyItem { Int = 1, Str = "1", Flo = 1.0f, Dou = 1.0 },
            StateMap = new StateMap<int, DirtyItem>
                { { 2, new DirtyItem { Int = 2, Str = "2", Flo = 2.0f, Dou = 2.0 } } }
        };
        var b = (IDiffUpdateable)((object)a);
        Assert.NotNull(b);

        _output.WriteLine("全量写入结果检查");
        var dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        Assert.True(dirties.Get(0));
        Assert.True(dirties.Get(1));
        //还原
        _output.WriteLine("a.CleanDirties检查");
        b.CleanDirties();
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));

        _output.WriteLine("v.Item覆盖脏检查");
        a.Item = new DirtyItem { Int = 1, Str = "1", Flo = 1.0f, Dou = 1.0 };
        dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        for (var i = 0; i < dirties.Count; i++)
            if (i == 0) Assert.True(dirties.Get(i));
            else Assert.False(dirties.Get(i));

        //还原
        _output.WriteLine("a.CleanDirties检查");
        b.CleanDirties();
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));

        _output.WriteLine("v.StateMap覆盖脏检查");
        a.StateMap = new StateMap<int, DirtyItem>
            { { 2, new DirtyItem { Int = 2, Str = "2", Flo = 2.0f, Dou = 2.0 } } };
        dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        for (var i = 0; i < dirties.Count; i++)
            if (i == 1) Assert.True(dirties.Get(i));
            else Assert.False(dirties.Get(i));

        //还原
        _output.WriteLine("a.CleanDirties检查");
        b.CleanDirties();
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));

        _output.WriteLine("v.Item.Int,v.Item.Dou,v.Item.Str覆盖脏检查");
        a.Item.Int = 1;
        a.Item.Dou = null;
        a.Item.Str = "";
        dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));
        dirties = GetDirtiesFromObject(a.Item);
        Assert.True(dirties != null);
        Assert.Equal(4, dirties.Count);
        Assert.True(dirties.Get(0));
        Assert.False(dirties.Get(1));
        Assert.True(dirties.Get(2));
        Assert.True(dirties.Get(3));

        //还原
        _output.WriteLine("a.CleanDirties检查");
        b.CleanDirties();
        dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));
        dirties = GetDirtiesFromObject(a.Item);
        Assert.True(dirties != null);
        Assert.Equal(4, dirties.Count);
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));

        //map检查 map的脏机制不一样
        _output.WriteLine("a.StateMap[1]脏检查和a.CleanDirties()再检查");
        a.StateMap[2] = new DirtyItem { Int = 2, Str = "2", Flo = 2.0f, Dou = 2.0 };
        var dirtySet = GetDirtySetFromObject(a.StateMap);
        Assert.True(dirtySet != null);
        Assert.Single(dirtySet);
        Assert.Contains(2, dirtySet);
        b.CleanDirties();
        dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));
        dirtySet = GetDirtySetFromObject(a.StateMap);
        Assert.True(dirtySet != null);
        Assert.Empty(dirtySet);

        //map检查 map的脏机制不一样
        _output.WriteLine("a.StateMap[2].Str检查和a.CleanDirties()再检查");
        a.StateMap[2]!.Str = null;
        dirtySet = GetDirtySetFromObject(a.StateMap);
        Assert.True(dirtySet != null);
        Assert.Empty(dirtySet);
        dirties = GetDirtiesFromObject(a.StateMap[2]!);
        Assert.True(dirties != null);
        Assert.Equal(4, dirties.Count);
        Assert.False(dirties.Get(0));
        Assert.False(dirties.Get(1));
        Assert.False(dirties.Get(2));
        Assert.True(dirties.Get(3));
        dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        //
        b.CleanDirties();
        dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Equal(2, dirties.Count);
        dirtySet = GetDirtySetFromObject(a.StateMap);
        Assert.True(dirtySet != null);
        Assert.Empty(dirtySet);
        dirties = GetDirtiesFromObject(a.StateMap[2]!);
        Assert.True(dirties != null);
        Assert.Equal(4, dirties.Count);
        for (var i = 0; i < dirties.Count; i++) Assert.False(dirties.Get(i));

        _output.WriteLine("---DirtyTest完成---");
    }
}