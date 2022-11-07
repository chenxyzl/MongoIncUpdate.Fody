using System.Collections;
using System.Reflection;
using MongoDB.Driver;
using Xunit;

namespace MongoIncUpdate.Fody.Test;

public partial class WeaverTests
{
    static BitArray? GetDirtiesFromObject(object a)
    {
        var props = a.GetType()
            .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .First(v => v.Name == "MongoIncUpdate.Fody.Test.IDiffUpdateable.Dirties");
        var dirties = props.GetValue(a) as BitArray;
        return dirties;
    }

    [Fact]
    void DirtyTest()
    {
        _output.WriteLine("---DirtyTest---");
        _output.WriteLine("全量赋值脏检查");
        var a = new ItemStateMapItemStateMapItem
        {
            Id = 1,
            Item = new StateMap<int, ItemNestItemStateMap>
            {
                {
                    1, new ItemNestItemStateMap
                    {
                        NestItem = new ItemIntKeyStateMapInt
                        {
                            IntInt = new StateMap<int, int> { { 1, 1 }, { 11, 11 } }
                        }
                    }
                },
                {
                    2, new ItemNestItemStateMap
                    {
                        NestItem = new ItemIntKeyStateMapInt
                        {
                            IntInt = new StateMap<int, int> { { 2, 2 }, { 22, 22 } }
                        }
                    }
                }
            }
        };
        var b = (IDiffUpdateable)a;
        Assert.NotNull(b);
        var dirties = GetDirtiesFromObject(a);
        Assert.True(dirties != null);
        Assert.Single(dirties);
        Assert.True(dirties.Get(0));
        b.CleanDirties();
        Assert.False(dirties.Get(0));
        

        //var dirties = b.GetType().GetProperties(BindingFlags.NonPublic).First(v=>v.Name =="Dirties").GetValue(b) as BitArray;
        //     Assert.True(dirties != null && dirties.Count == 1);
        //     Assert.True(dirties[0]);
        //     
        //     // _output.WriteLine("全量写入结果检查");
        //     // await cc.IncUpdate(a);
        //     // var filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // var result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Equal(2, result.Item[1]!.NestItem.IntInt.Count);
        //     // Assert.Equal(2, result.Item[2]!.NestItem.IntInt.Count);
        //     // Assert.Equal(11, result.Item[1]!.NestItem.IntInt[11]);
        //     // Assert.Equal(22, result.Item[2]!.NestItem.IntInt[22]);
        //     //
        //     // _output.WriteLine("Item全量覆盖测试");
        //     // a.Item = new StateMap<int, ItemNestItemStateMap>
        //     // {
        //     //     {
        //     //         3, new ItemNestItemStateMap
        //     //         {
        //     //             NestItem = new ItemIntKeyStateMapInt
        //     //             {
        //     //                 IntInt = new StateMap<int, int> { { 3, 3 }, { 33, 33 } }
        //     //             }
        //     //         }
        //     //     },
        //     //     {
        //     //         4, new ItemNestItemStateMap
        //     //         {
        //     //             NestItem = new ItemIntKeyStateMapInt
        //     //             {
        //     //                 IntInt = new StateMap<int, int> { { 4 ,4 }, { 44, 44 } }
        //     //             }
        //     //         }
        //     //     }
        //     // };
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        //     // Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        //     // Assert.Equal(33, result.Item[3]!.NestItem.IntInt[33]);
        //     // Assert.Equal(44, result.Item[4]!.NestItem.IntInt[44]);
        //     //
        //     // _output.WriteLine("v.Item[3]覆盖检查");
        //     // a.Item[3] = new ItemNestItemStateMap
        //     // {
        //     //     NestItem = new ItemIntKeyStateMapInt
        //     //     {
        //     //         IntInt = new StateMap<int, int> { { 333, 333 }, { 3333, 3333 } }
        //     //     }
        //     // };
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        //     // Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        //     // Assert.Equal(333, result.Item[3]!.NestItem.IntInt[333]);
        //     // Assert.Equal(44, result.Item[4]!.NestItem.IntInt[44]);
        //     //
        //     // _output.WriteLine("v.Item[3].NestItem覆盖检查");
        //     // a.Item[4]!.NestItem = new ItemIntKeyStateMapInt
        //     // {
        //     //     IntInt = new StateMap<int, int> { { 444, 444 }, { 4444, 4444 } }
        //     // };
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        //     // Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        //     // Assert.Equal(3333, result.Item[3]!.NestItem.IntInt[3333]);
        //     // Assert.Equal(4444, result.Item[4]!.NestItem.IntInt[4444]);
        //     //
        //     // _output.WriteLine("v.Item[3].NestItem.IntInt覆盖检查");
        //     // a.Item[4]!.NestItem.IntInt = new StateMap<int, int> { { 44444, 44444 }, { 444444, 444444 } };
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        //     // Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        //     // Assert.Equal(3333, result.Item[3]!.NestItem.IntInt[3333]);
        //     // Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);
        //     //
        //     // _output.WriteLine("v.Item[3].NestItem.IntInt[3333]覆盖检查");
        //     // a.Item[3]!.NestItem.IntInt[3333] = 3;
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Equal(2, result.Item[3]!.NestItem.IntInt.Count);
        //     // Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        //     // Assert.Equal(3, result.Item[3]!.NestItem.IntInt[3333]);
        //     // Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);
        //     //
        //     // _output.WriteLine("v.Item[3].NestItem.IntInt.remove(3333)删除检查");
        //     // a.Item[3]!.NestItem.IntInt.Remove(3333);
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Single(result.Item[3]!.NestItem.IntInt);
        //     // Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        //     // Assert.False(result.Item[3]!.NestItem.IntInt.ContainsKey(3333));
        //     // Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);
        //     //
        //     // _output.WriteLine("v.Item[3].NestItem.IntInt.Clean()删除检查");
        //     // a.Item[3]!.NestItem.IntInt.Clear();
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Equal(2, result.Item.Count);
        //     // Assert.Empty(result.Item[3]!.NestItem.IntInt);
        //     // Assert.Equal(2, result.Item[4]!.NestItem.IntInt.Count);
        //     // Assert.Equal(44444, result.Item[4]!.NestItem.IntInt[44444]);
        //     //
        //     // _output.WriteLine("v.Item.Clean()删除检查");
        //     // a.Item.Clear();
        //     // await cc.IncUpdate(a);
        //     // filter = Builders<ItemStateMapItemStateMapItem>.Filter.Eq(x => x.Id, a.Id);
        //     // result = (await cc.FindAsync(filter)).First();
        //     // Assert.Empty(result.Item);
        //     
        //     _output.WriteLine("---DirtyTest完成---");
    }
}