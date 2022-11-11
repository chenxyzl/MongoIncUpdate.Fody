using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoIncUpdate.Base;
using Xunit;

namespace MongoIncUpdate.Fody.Test;

public interface IItemDataBase
{
    // 道具id
    int ItemId { get; set; }

    // 道具数量
    int GetCount();

    // 设置道具数量
    void SetCount(int count)
    {
    }
}

public interface IIncrItemData : IItemDataBase
{
    // 自增id
    int IncrId { get; set; }
}

public abstract class BaseIncrSingleItemData : IIncrItemData
{
    public int ItemId { get; set; }

    public int GetCount()
    {
        return 1;
    }

    public int IncrId { get; set; }
}

[MongoIncUpdate]
public sealed class WeaponItemData : BaseIncrSingleItemData
{
    [BsonId] public int PlayerId { get; set; }

    // 等级
    public int Level { get; set; }

    // 经验
    public int LevelExp { get; set; }

    // 突破等级
    public int Breach { get; set; }

    // 绑定角色id
    public int RoleId { get; set; }

    // 共鸣等级
    public int ResonLevel { get; set; }
}

[MongoIncUpdate]
public sealed class EquipItemData : BaseIncrSingleItemData
{
    [BsonId] public int PlayerId { get; set; }

    // 等级
    public int Level { get; set; }

    // 经验
    public int LevelExp { get; set; }

    // 突破等级
    public int Breach { get; set; }

    // 绑定角色id
    public int RoleId { get; set; }

    // 共鸣等级
    public int ResonLevel { get; set; }
}

public abstract class NoUsedBaseIncrSingleItemData : IIncrItemData
{
    public int ItemId { get; set; }
    public int GetCount() => 1;

    public int IncrId { get; set; }
}

public class IncUpdateInheritTest
{
    private static bool _init;
    private static IMongoCollection<WeaponItemData> _cc;
    private static IMongoCollection<EquipItemData> _cc2;

    public IncUpdateInheritTest()
    {
        lock (this)
        {
            if (_init) return;
            _init = true;
            IncUpdateExt.Register();
            var connectionString = "mongodb://admin:123456@127.0.0.1:27017/test?authSource=admin";
            var mongoClient = new MongoClient(connectionString);
            var db = mongoClient.GetDatabase(new MongoUrlBuilder(connectionString).DatabaseName);
            _cc = db.GetCollection<WeaponItemData>(nameof(WeaponItemData));
            _cc2 = db.GetCollection<EquipItemData>(nameof(EquipItemData));
        }
    }

    [Fact]
    public async Task TestItem()
    {
        {
            var a = new WeaponItemData
            {
                PlayerId = 1, ItemId = 1, IncrId = 2, Level = 3, LevelExp = 4, Breach = 5, RoleId = 6, ResonLevel = 7
            };
            await _cc.IncUpdate(a);
            a.ItemId = 11;
            a.IncrId = 12;
            a.Level = 13;
            a.LevelExp = 14;
            a.Breach = 15;
            a.RoleId = 16;
            a.ResonLevel = 17;
            await _cc.IncUpdate(a);
            var filer = Builders<WeaponItemData>.Filter.Eq(x => x.PlayerId, a.PlayerId);
            var v = (await _cc.FindAsync(filer)).First();
            Assert.Equal(v.ItemId, a.ItemId);
            Assert.Equal(v.IncrId, a.IncrId);
            Assert.Equal(v.Level, a.Level);
            Assert.Equal(v.LevelExp, a.LevelExp);
            Assert.Equal(v.Breach, a.Breach);
            Assert.Equal(v.RoleId, a.RoleId);
            Assert.Equal(v.ResonLevel, a.ResonLevel);
        }

        {
            var a = new WeaponItemData
            {
                PlayerId = 2, ItemId = 1, IncrId = 2, Level = 3, LevelExp = 4, Breach = 5, RoleId = 6, ResonLevel = 7
            };
            await _cc.IncUpdate(a);
            a.ItemId = 111;
            a.IncrId = 121;
            a.Level = 131;
            a.LevelExp = 141;
            a.Breach = 151;
            a.RoleId = 161;
            a.ResonLevel = 171;
            await _cc.IncUpdate(a);
            var filer = Builders<WeaponItemData>.Filter.Eq(x => x.PlayerId, a.PlayerId);
            var v = (await _cc.FindAsync(filer)).First();
            Assert.Equal(v.ItemId, a.ItemId);
            Assert.Equal(v.IncrId, a.IncrId);
            Assert.Equal(v.Level, a.Level);
            Assert.Equal(v.LevelExp, a.LevelExp);
            Assert.Equal(v.Breach, a.Breach);
            Assert.Equal(v.RoleId, a.RoleId);
            Assert.Equal(v.ResonLevel, a.ResonLevel);
        }

        {
            var a = new EquipItemData
            {
                PlayerId = 1, ItemId = 1, IncrId = 2, Level = 3, LevelExp = 4, Breach = 5, RoleId = 6, ResonLevel = 7
            };
            await _cc2.IncUpdate(a);
            a.ItemId = 111;
            a.IncrId = 112;
            a.Level = 113;
            a.LevelExp = 114;
            a.Breach = 115;
            a.RoleId = 116;
            a.ResonLevel = 117;
            await _cc2.IncUpdate(a);
            var filer = Builders<EquipItemData>.Filter.Eq(x => x.PlayerId, a.PlayerId);
            var v = (await _cc2.FindAsync(filer)).First();
            Assert.Equal(v.ItemId, a.ItemId);
            Assert.Equal(v.IncrId, a.IncrId);
            Assert.Equal(v.Level, a.Level);
            Assert.Equal(v.LevelExp, a.LevelExp);
            Assert.Equal(v.Breach, a.Breach);
            Assert.Equal(v.RoleId, a.RoleId);
            Assert.Equal(v.ResonLevel, a.ResonLevel);
        }
    }
}