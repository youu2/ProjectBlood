using System;
using System.Collections.Generic;

namespace ProjectBlood
{
    // 关卡进度存档（单槽位）。纯数据 DTO，由 JsonUtility 序列化。
    // 所有引用型资产（UpgradeSO / BloodSigilSO / 掉落 prefab）一律存稳定 id，不存引用。
    [Serializable]
    public class RunSaveData
    {
        // ---- 关卡标识 ----
        public int difficultyIndex;
        public string levelName;

        // ---- 地图与房间（静态，每关一次）----
        public List<RoomSaveEntry> rooms = new List<RoomSaveEntry>();
        // 已发现（小地图可见）的房间 id 列表，id 格式为 "gridX,gridY"
        public List<string> discoveredRooms = new List<string>();

        // ---- 玩家状态（频繁变动）----
        public int playerGridX;
        public int playerGridY;
        public float currentHP;
        public float maxHP;     // 对应 Global.INGAME_MAX_HP
        public int coin;
        public int level;
        public int exp;
        public int maxExp;
        public List<WeaponSaveEntry> weapons = new List<WeaponSaveEntry>();
        public int currentWeaponIndex;

        // ---- 局内成长（中频变动）----
        public List<UpgradeSaveEntry> upgrades = new List<UpgradeSaveEntry>();
        public List<WeaponDamageSaveEntry> weaponDamageLevels = new List<WeaponDamageSaveEntry>();
        public List<WeaponDamageRatioSaveEntry> weaponDamageRatios = new List<WeaponDamageRatioSaveEntry>();
        public List<SkillCooldownSaveEntry> skillCooldownReductions = new List<SkillCooldownSaveEntry>();
        public float globalDamageRatio = 1f;
        public float moveSpeedBonus;
        public List<SigilSaveEntry> sigils = new List<SigilSaveEntry>();
        public float permanentDamageBonus;      // 献祭永久增伤台账
        public int damageImmunityCharges;       // "免疫下一次伤害"充能
        public float runElapsedSeconds;
        public float remainingTime;
        public float blazingCircleDamage;
        public float bcAttackInterval;

        // ---- 地面掉落物 ----
        public List<DropSaveEntry> drops = new List<DropSaveEntry>();
    }

    [Serializable]
    public class RoomSaveEntry
    {
        public int gridX;
        public int gridY;
        public int roomType;                            // RoomType 枚举转 int
        public List<int> doorDirs = new List<int>();    // Direction 枚举转 int
        public List<string> tileRows = new List<string>();  // 完整房间模板行，用于按档建图
        public int roomState;                           // RoomState 枚举转 int
        public bool chestCollected;
        public List<int> collectedShopItems = new List<int>();  // 已购商品在房间内的索引
    }

    [Serializable]
    public class WeaponSaveEntry
    {
        public string weaponName;   // 与 WeaponConfig.weaponName 一致
        public int currentAmmo;
        public int maxAmmo;
    }

    [Serializable]
    public class UpgradeSaveEntry
    {
        public string upgradeId;    // UpgradeSO.id
        public int count;
    }

    [Serializable]
    public class WeaponDamageSaveEntry
    {
        public string weaponType;   // WeaponType 枚举名
        public int level;
    }

    [Serializable]
    public class WeaponDamageRatioSaveEntry
    {
        public string weaponType;   // WeaponType 枚举名
        public float ratio;
    }

    [Serializable]
    public class SkillCooldownSaveEntry
    {
        public string skillName;    // SkillData.skillName
        public float reduction;
    }

    [Serializable]
    public class SigilSaveEntry
    {
        public string sigilId;  // BloodSigilSO.id
        // 与该血印 effects 列表顺序一一对应的模块快照
        public List<SigilModuleSaveEntry> modules = new List<SigilModuleSaveEntry>();
    }

    [Serializable]
    public class SigilModuleSaveEntry
    {
        public int firedCount;      // 已触发次数（对应 maxStacks 限制）
        public bool active;         // 持续型效果是否激活中
        public bool triggerLatched; // 边沿触发锁存
        public float[] endRemaining;    // 各结束条件剩余秒数
        public bool[] endLatched;       // 各结束条件事件锁存
    }

    [Serializable]
    public class DropSaveEntry
    {
        public string dropType; // DropManager 类型映射表 key
        public int gridX;
        public int gridY;
        public string sigilId;  // 仅 BloodSigilDrop 使用，其余为空
    }
}
