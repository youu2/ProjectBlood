using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 关卡进度存档服务：聚合各系统 Export/Import，提供 SaveNow / TryLoad / DeleteSave 入口。
    /// </summary>
    public static class RunSaveService
    {
        // 读档时由 UIGameStartPanel 填充，MapController.Start 检测此载荷后走还原路径
        public static RunSaveData PendingRestore { get; set; }

        // 上次快照的 CRC32，用于防重复保存
        private static uint lastSnapshotCrc;

        // 是否存在有效存档（供主菜单"继续游戏"按钮显隐判断）
        public static bool HasSave() => RunSaveManager.TryLoad(out _);

        public static bool TryLoad(out RunSaveData data)
        {
            if (RunSaveManager.TryLoad(out data))
            {
                if (data.difficultyIndex < 0 || data.difficultyIndex >= Global.LevelConfigs.Count)
                {
                    Debug.LogWarning($"[RunSaveService] 存档关卡索引 {data.difficultyIndex} 无效，删除存档");
                    RunSaveManager.DeleteSave();
                    return false;
                }
                return true;
            }
            return false;
        }

        // 立即保存当前完整状态（供房间离开、BtnQuit、InitializeLevel 完成时调用）
        public static void SaveNow()
        {
            var data = new RunSaveData();

            // 1. 玩家状态与局内成长
            Global.ExportTo(data);
            WeaponDataSystem.ExportTo(data);
            PlayerUpgradeState.ExportTo(data);
            BloodSigilState.ExportTo(data);

            // 2. 玩家网格坐标（从世界坐标向下取整）
            if (Player.player1 != null)
            {
                data.playerGridX = Mathf.FloorToInt(Player.player1.transform.position.x);
                data.playerGridY = Mathf.FloorToInt(Player.player1.transform.position.y);
            }

            // 3. 地图与房间
            if (MapController.instance != null)
                MapController.instance.ExportTo(data);

            // 4. 地面掉落物：遍历场景所有 DropItem 记录类型与网格位置
            CollectDrops(data);

            // 5. 快照哈希防重复
            var json = UnityEngine.JsonUtility.ToJson(data);
            var crc = RunSaveManager.ComputeCrc32(json);
            if (crc == lastSnapshotCrc) return; // 无变化，跳过写盘
            lastSnapshotCrc = crc;

            RunSaveManager.Save(data);
            Debug.Log("[RunSaveService] 存档已保存");
        }

        // 收集场景地面掉落物（含血印掉落的 sigilId）
        // 注：BloodSigilDrop 继承 InteractableBase 而非 DropItem，需单独扫描避免漏记
        private static void CollectDrops(RunSaveData data)
        {
            data.drops.Clear();
            var dm = DropManager.Instance;
            if (dm == null) return;

            var drops = UnityEngine.Object.FindObjectsByType<DropItem>(FindObjectsSortMode.None);
            foreach (var drop in drops)
            {
                if (!drop.gameObject.activeInHierarchy) continue;

                string type = DropTypeOf(drop);
                if (type == null) continue;     // 未注册类型不入档

                var pos = drop.transform.position;
                var entry = new DropSaveEntry
                {
                    dropType = type,
                    gridX = Mathf.FloorToInt(pos.x),
                    gridY = Mathf.FloorToInt(pos.y),
                };
                var sigilDrop = drop.GetComponent<BloodSigilDrop>();
                if (sigilDrop != null)
                    entry.sigilId = sigilDrop.SigilId;
                data.drops.Add(entry);
            }

            // 血印掉落单独扫描（不属于 DropItem 体系）
            var sigilDrops = UnityEngine.Object.FindObjectsByType<BloodSigilDrop>(FindObjectsSortMode.None);
            foreach (var sigilDrop in sigilDrops)
            {
                if (!sigilDrop.gameObject.activeInHierarchy) continue;
                // 已在 DropItem 扫描中处理的跳过（BloodSigilDrop 若与 DropItem 同物体）
                if (sigilDrop.GetComponent<DropItem>() != null) continue;

                var pos = sigilDrop.transform.position;
                data.drops.Add(new DropSaveEntry
                {
                    dropType = "BloodSigilDrop",
                    gridX = Mathf.FloorToInt(pos.x),
                    gridY = Mathf.FloorToInt(pos.y),
                    sigilId = sigilDrop.SigilId,
                });
            }
        }

        // DropItem → 类型 key（与 RestoreDrops 对应）
        // 注：AnnihilationCore / BloodSigilDrop 不继承 DropItem，需用 GetComponent 判断
        private static string DropTypeOf(DropItem drop)
        {
            if (drop is Coin) return "Coin";
            if (drop is Exp) return "Exp";
            if (drop is DirtyBlood) return "DirtyBlood";
            if (drop is Shield) return "Shield";
            if (drop is PureBlood) return "PureBlood";
            if (drop.GetComponent<AnnihilationCore>() != null) return "AnnihilationCore";
            if (drop is MP5Unlock) return "MP5Unlock";
            if (drop is ShotGunUnlock) return "ShotGunUnlock";
            if (drop is AKUnlock) return "AKUnlock";
            if (drop is AWPUnlock) return "AWPUnlock";
            if (drop is LaserUnlock) return "LaserUnlock";
            if (drop.GetComponent<BloodSigilDrop>() != null) return "BloodSigilDrop";
            return null;
        }

        // 读档还原：重新生成地面掉落物
        // 注：DropManager 的 ShotGunUnlock/AKUnlock/AWPUnlock/LaserUnlock 字段声明为 CircleCollider2D，
        // 统一取 .gameObject 实例化再取 DropItem 组件，规避字段类型不一致
        public static void RestoreDrops(RunSaveData data)
        {
            var dm = DropManager.Instance;
            if (dm == null) return;

            foreach (var entry in data.drops)
            {
                GameObject prefab = entry.dropType switch
                {
                    "Coin" => dm.Coin != null ? dm.Coin.gameObject : null,
                    "Exp" => dm.Exp != null ? dm.Exp.gameObject : null,
                    "DirtyBlood" => dm.DirtyBlood != null ? dm.DirtyBlood.gameObject : null,
                    "Shield" => dm.Shield != null ? dm.Shield.gameObject : null,
                    "PureBlood" => dm.PureBlood != null ? dm.PureBlood.gameObject : null,
                    "AnnihilationCore" => dm.AnnihilationCore != null ? dm.AnnihilationCore.gameObject : null,
                    "MP5Unlock" => dm.MP5Unlock != null ? dm.MP5Unlock.gameObject : null,
                    "ShotGunUnlock" => dm.ShotGunUnlock != null ? dm.ShotGunUnlock.gameObject : null,
                    "AKUnlock" => dm.AKUnlock != null ? dm.AKUnlock.gameObject : null,
                    "AWPUnlock" => dm.AWPUnlock != null ? dm.AWPUnlock.gameObject : null,
                    "LaserUnlock" => dm.LaserUnlock != null ? dm.LaserUnlock.gameObject : null,
                    "BloodSigilDrop" => dm.BloodSigilDrop != null ? dm.BloodSigilDrop.gameObject : null,
                    _ => null,
                };
                if (prefab == null) continue;

                var pos = new Vector3(entry.gridX + 0.5f, entry.gridY + 0.5f, 0);
                var dropObj = prefab.Instantiate().Position(pos);
                var drop = dropObj.GetComponent<DropItem>();
                if (dropObj.TryGetComponent<BloodSigilDrop>(out var sigilDrop) && !string.IsNullOrEmpty(entry.sigilId))
                {
                    var sigil = FindSigilById(entry.sigilId);
                    if (sigil != null) sigilDrop.Initialize(sigil);
                }
                dropObj.Show();
            }
        }

        private static BloodSigilSO FindSigilById(string id)
        {
            var manager = BloodSigilManager.Instance;
            if (manager == null) return null;
            // BloodSigilManager 需暴露按 id 查询接口；若尚未添加则遍历池
            return manager.FindById(id);
        }

        public static void DeleteSave()
        {
            RunSaveManager.DeleteSave();
            PendingRestore = null;
            lastSnapshotCrc = 0;
        }
    }
}
