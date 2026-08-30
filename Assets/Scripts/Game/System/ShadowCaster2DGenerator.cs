using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace ProjectBlood
{
    // 为 wallTilemap 上的全部瓦片（围墙'1'/'2'、房间内掩体'3'、走廊墙）生成 ShadowCaster2D
    // 每个合并段对应一个 alpha=0 的 Sprite 四边形，剪影即该矩形的几何轮廓。
    // 每段都是独立的轴对齐凸矩形，不会出现闭合环/孔洞类投影问题。
    public static class ShadowCaster2DGenerator
    {
        private static Sprite _unitSprite;

        // 4x4 白图配 PPU=4 得到 1x1 世界单位的方块，便于按段长直接缩放
        private static Sprite UnitSprite
        {
            get
            {
                if (_unitSprite == null)
                {
                    _unitSprite = Sprite.Create(Texture2D.whiteTexture,
                        new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    _unitSprite.name = "TileShadowUnitSprite";
                }
                return _unitSprite;
            }
        }

        // 扫描 wallTilemap 全部瓦片后做游程合并：横向连续段(>=2) -> 纵向补剩余段 -> 孤立单格，逐段生成 caster
        // 真实门格不落墙瓦片，扫描自然跳过（门口透光）；fallback 成实墙的门格正常投影
        public static void Generate(Tilemap wallTilemap)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var pos in wallTilemap.cellBounds.allPositionsWithin)
            {
                if (wallTilemap.HasTile(pos))
                {
                    cells.Add(new Vector2Int(pos.x, pos.y));
                }
            }

            if (cells.Count == 0)
            {
                return;
            }

            var container = new GameObject("TileShadows");
            container.transform.SetParent(wallTilemap.transform, false);

            int count = 0;
            count += MergeRuns(cells, horizontal: true, container.transform);   // 第一遍：横向
            count += MergeRuns(cells, horizontal: false, container.transform);  // 第二遍：纵向补剩余
            foreach (var cell in cells)                                         // 第三遍：孤立单格
            {
                CreateCaster(cell.x, cell.y, 1, 1, container.transform);
                count++;
            }

            Debug.Log($"[ShadowCaster2DGenerator] 墙瓦片 {cells.Count} 个，生成 ShadowCaster2D x{count}");
        }

        // 为门格挂一个 1×1 隐形 caster，覆盖整个门格与相邻墙体段无缝衔接
        // 门激活(关门)时挡光，门隐藏(开门)时随物体失活自动失效，无需额外状态同步
        public static void AttachCellCaster(Transform parent)
        {
            var go = new GameObject("DoorShadow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = UnitSprite;
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f); // 透明

            var caster = go.AddComponent<ShadowCaster2D>();
            caster.useRendererSilhouette = true;
            caster.selfShadows = true;
        }

        // 按行(horizontal=true)或列(horizontal=false)分组扫描连续段，
        // 长度>=2 的段生成一个 caster 并从集合移除，剩余格子留给下一遍处理
        private static int MergeRuns(HashSet<Vector2Int> cells, bool horizontal, Transform parent)
        {
            var groups = new Dictionary<int, List<int>>();
            foreach (var cell in cells)
            {
                int key = horizontal ? cell.y : cell.x;
                int value = horizontal ? cell.x : cell.y;
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    groups.Add(key, list);
                }
                list.Add(value);
            }

            int count = 0;
            foreach (var pair in groups)
            {
                var values = pair.Value;
                values.Sort();
                for (int i = 0; i < values.Count;)
                {
                    int start = values[i];
                    int j = i;
                    while (j + 1 < values.Count && values[j + 1] == values[j] + 1)
                    {
                        j++;
                    }

                    int length = values[j] - start + 1;
                    if (length >= 2)
                    {
                        if (horizontal)
                        {
                            CreateCaster(start, pair.Key, length, 1, parent);
                        }
                        else
                        {
                            CreateCaster(pair.Key, start, 1, length, parent);
                        }
                        count++;
                        for (int v = start; v <= values[j]; v++)
                        {
                            cells.Remove(horizontal ? new Vector2Int(v, pair.Key) : new Vector2Int(pair.Key, v));
                        }
                    }

                    i = j + 1;
                }
            }

            return count;
        }

        // 生成一个隐形 Sprite 四边形 + ShadowCaster2D
        // 剪影取 Sprite 几何与颜色无关；renderer 必须保持 enabled（disable 后阴影失效），故用 alpha=0 隐形
        private static void CreateCaster(int x, int y, int width, int height, Transform parent)
        {
            var go = new GameObject($"TileShadow_{width}x{height}_{x}_{y}");
            go.transform.SetParent(parent, false);
            // 段中心沿用格心约定：起点格中心 (x+0.5, y+0.5) 再加段长的一半
            go.transform.position = new Vector3(x + width * 0.5f, y + height * 0.5f, 0f);
            go.transform.localScale = new Vector3(width, height, 1f);

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = UnitSprite;
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);

            var caster = go.AddComponent<ShadowCaster2D>();
            caster.useRendererSilhouette = true;
            caster.selfShadows = true;
        }
    }
}
