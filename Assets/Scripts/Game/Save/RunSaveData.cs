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
        public bool isDarkLevel;    // 本关是否为黑暗关（读档时不重新掷骰，保持进度一致）

        // ---- 地图与房间（静态，每关一次）----
        public List<RoomSaveEntry> rooms = new List<RoomSaveEntry>();
        // 已发现（小地图可见）的房间 id 列表，id 格式为 "gridX,gridY"
        public List<string> discoveredRooms = new List<string>();

        // ---- 玩家状态（频繁变动）----
        public int playerGridX;
        public int playerGridY;
        public float currentHP;
        public float maxHP;     // 对应 Global.INGAME_MAX_HP
        public int bloodBankMax = 100;     // 血库容量（含强化/血印/局外养成加成）
        public int bloodBankCurrent = 100; // 血库当前储量（特殊换弹/强化子弹的消耗资源）
        public int coin;
        public int level;
        public int exp;
        public int maxExp;
        public int pendingUpgradeCount;   // 累计可选择升级次数（升级 +1，选强化 -1）
        public List<WeaponSaveEntry> weapons = new List<WeaponSaveEntry>();
        public int currentWeaponIndex;

        // ---- 局内成长（中频变动）----
        public List<UpgradeSaveEntry> upgrades = new List<UpgradeSaveEntry>();
        public List<WeaponDamageSaveEntry> weaponDamageLevels = new List<WeaponDamageSaveEntry>();
        public List<WeaponDamageRatioSaveEntry> weaponDamageRatios = new List<WeaponDamageRatioSaveEntry>();
        public List<SkillCooldownSaveEntry> skillCooldownReductions = new List<SkillCooldownSaveEntry>();
        public List<SkillMaxChargesSaveEntry> skillMaxChargesBonuses = new List<SkillMaxChargesSaveEntry>();
        public float globalDamageRatio = 1f;
        public float moveSpeedBonus;
        public List<SigilSaveEntry> sigils = new List<SigilSaveEntry>();
        // 主动血印槽位表（固定长度 4，对应数字键 1~4；空槽为空字符串）。
        // 按槽位位置持久化，继续游戏后槽位不重新排列；旧存档无此字段时自动补分槽。
        public List<string> activeSigilSlotIds = new List<string>();
        public float permanentDamageBonus;      // 献祭永久增伤台账
        public int damageImmunityCharges;       // "免疫下一次伤害"充能
        public float runElapsedSeconds;
        public float remainingTime;
        public float blazingCircleDamage;
        public float bcAttackInterval;

        // ---- 宝箱武器掉落进度 ----
        // 已从宝箱掉落的武器数量（即下一次掉落的索引），对应 Chest.currentWeaponIndex
        public int chestWeaponIndex;

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
    public class SkillMaxChargesSaveEntry
    {
        public string skillName;    // SkillData.skillName
        public int bonus;           // 最大充能层数累计加成（血印"翻滚层数变为3"等）
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
        // 注：门控开关状态不持久化（生命周期不存档，继续游戏按初始/就绪状态还原）
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
