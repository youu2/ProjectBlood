using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace ProjectBlood
{
    public static class PathSearchingHelper
    {
        // 搜索路径（A*）：成功返回 true，失败返回 false
        // path 输出顺序：[end, ...中间节点..., 起点的下一跳]，调用方从末尾（^1）取点即可由近及远前进
        public static bool SearchPath<T>(NodeBase<T> start, NodeBase<T> end, List<NodeBase<T>> path)
        {
            if (start == null || end == null || path == null || start.Coords == null || end.Coords == null)
                return false;

            path.Clear();
            if (start == end) return true;

            var toSearch = ListPool<NodeBase<T>>.Get();
            toSearch.Add(start);
            start.GCost = 0;
            start.HCost = start.GetDistance(end);
            var processed = ListPool<NodeBase<T>>.Get();

            while (toSearch.Count > 0)
            {
                var current = toSearch[0];
                foreach (var neighbor in toSearch)
                {
                    if (neighbor.FCost < current.FCost
                        || (Mathf.Abs(neighbor.FCost - current.FCost) < 0.001f && neighbor.HCost < current.HCost))
                    {
                        current = neighbor;
                    }
                }
                processed.Add(current);
                toSearch.Remove(current);

                if (current == end)
                {
                    var currentPathTile = end;
                    var count = 100;
                    while (currentPathTile != start)
                    {
                        path.Add(currentPathTile);
                        currentPathTile = currentPathTile.Connection;
                        if (--count <= 0) { path.Clear(); break; }
                    }
                    toSearch.Release2Pool();
                    processed.Release2Pool();
                    return path.Count > 0 || start == end;
                }

                foreach (var neighbor in current.Neighbors)
                {
                    if (processed.Contains(neighbor) || !neighbor.Walkable) continue;
                    var costToNeighbor = current.GCost + current.GetDistance(neighbor);
                    var inSearch = toSearch.Contains(neighbor);
                    if (!inSearch || costToNeighbor < neighbor.GCost)
                    {
                        neighbor.GCost = costToNeighbor;
                        neighbor.Connection = current;
                        if (!inSearch)
                        {
                            neighbor.HCost = neighbor.GetDistance(end);
                            toSearch.Add(neighbor);
                        }
                    }
                }
            }

            toSearch.Release2Pool();
            processed.Release2Pool();
            return false;
        }


        public interface ICoords<T>
        {
            float GetDistance(ICoords<T> other);
            T Position { get; set; }
        }

        // 地图节点坐标
        public class TileCoords : ICoords<Vector3Int>
        {
            /// <summary>
            /// 返回两点之间的真实欧氏距离（用于 A* 的 GCost 增量与 HCost 估计）。
            /// 4 向直线邻居距离 = 1，对角线邻居距离 = √2，与 8 向移动的真实步长一致，
            /// 同时保证 HCost 是可接受启发函数（不会高估最短路径代价）。
            /// </summary>
            public float GetDistance(ICoords<Vector3Int> other)
            {
                float dx = Position.x - other.Position.x;
                float dy = Position.y - other.Position.y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            public Vector3Int Position { get; set; }
        }

        // 地图节点基类
        public abstract class NodeBase<T>
        {
            public ICoords<T> Coords;
            public float GetDistance(NodeBase<T> other)
            {
                return Coords.GetDistance(other.Coords);
            }

            public bool Walkable { get; private set; }

            public virtual NodeBase<T> Init(ICoords<T> coords, bool walkable)
            {
                Coords = coords;
                Walkable = walkable;
                return this;
            }

            public List<NodeBase<T>> Neighbors { get; private set; } = new List<NodeBase<T>>();
            public NodeBase<T> Connection { get; set; }
            public float GCost { get; set; } = float.MaxValue;
            public float HCost { get; set; } = float.MaxValue;
            public float FCost => GCost + HCost;
            public abstract void CacheNeighbors();
        }

        public class TileNode : NodeBase<Vector3Int>
        {
            private readonly DynaGrid<TileNode> grid;
            public TileNode(DynaGrid<TileNode> grid)
            {
                this.grid = grid;
            }
            // 8 方向：4 个正交 + 4 个对角（顺序无关，对角线判定基于 dx/dy 本身而非索引）
            private static readonly List<Vector3Int> NeighborDirections = new()
            {
                new Vector3Int( 0,  1, 0),  // 上
                new Vector3Int( 0, -1, 0),  // 下
                new Vector3Int( 1,  0, 0),  // 右
                new Vector3Int(-1,  0, 0),  // 左
                new Vector3Int( 1,  1, 0),  // 右上
                new Vector3Int( 1, -1, 0),  // 右下
                new Vector3Int(-1,  1, 0),  // 左上
                new Vector3Int(-1, -1, 0),  // 左下
            };
            public override void CacheNeighbors()
            {
                Neighbors.Clear();
                Vector3Int self = Coords.Position;
                foreach (Vector3Int dir in NeighborDirections)
                {
                    bool isDiagonal = dir.x != 0 && dir.y != 0;
                    Vector3Int nPos = self + dir;
                    TileNode neighbor = grid[nPos.x, nPos.y];
                    if (neighbor == null || !neighbor.Walkable) continue;

                    if (isDiagonal)
                    {
                        // 对角线移动必须同时保证两个正交方向的格子也可走
                        // → 防止"切角穿墙"：目标格空但两侧都是墙，视觉会穿入墙角
                        TileNode sideX = grid[nPos.x, self.y];
                        TileNode sideY = grid[self.x, nPos.y];
                        if (sideX == null || !sideX.Walkable
                            || sideY == null || !sideY.Walkable)
                        {
                            continue;
                        }
                    }

                    Neighbors.Add(neighbor);
                }
            }
        }
    }
}