using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A* 寻路工具类（静态）
/// 职责：
///   1. 读取 LevelManager 的障碍物数据构建导航网格
///   2. 使用 A* 算法计算从起点到终点的最短无障碍路径
///   3. 路径平滑：射线检测删除多余拐点
/// </summary>
public static class BeePathfinder
{
    private static float cellSize = 0.4f;   // 网格单元大小
    private static int[,] grid;             // 0=可通行, 1=障碍
    private static int gridW, gridH;
    private static Vector2 gridOrigin;

    /// <summary>计算从 start 到 end 的最优路径</summary>
    public static List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        if (LevelManager.Instance == null) return new List<Vector2> { start, end };

        BuildGrid();

        Vector2Int startCell = WorldToCell(start);
        Vector2Int endCell = WorldToCell(end);

        if (!InBounds(startCell) || !InBounds(endCell))
            return new List<Vector2> { start, end };

        // 起点/终点在墙内时，找最近的可通行格子
        if (grid[startCell.x, startCell.y] == 1)
            startCell = NearestFree(startCell);
        if (grid[endCell.x, endCell.y] == 1)
            endCell = NearestFree(endCell);

        if (startCell == endCell)
            return new List<Vector2> { end };

        // A* 寻路
        List<Vector2> path = AStar(startCell, endCell);
        if (path.Count == 0)
            return new List<Vector2> { start, end }; // 回退：直走

        // 修正首尾为实际坐标
        if (path.Count > 0) path[0] = start;
        if (path.Count > 1) path[path.Count - 1] = end;

        return SmoothPath(path);
    }

    /// <summary>构建导航网格：标记障碍物和屏幕边界</summary>
    static void BuildGrid()
    {
        float minX = -6f, maxX = 6f, minY = -10f, maxY = 10f;

        gridW = Mathf.CeilToInt((maxX - minX) / cellSize) + 1;
        gridH = Mathf.CeilToInt((maxY - minY) / cellSize) + 1;
        gridOrigin = new Vector2(minX, minY);
        grid = new int[gridW, gridH];

        // 标记所有关卡墙体（膨胀 0.3 单位，确保蜜蜂不会贴墙太近）
        foreach (var wall in LevelManager.Instance.terrainWalls)
        {
            if (wall == null) continue;
            Bounds b = wall.bounds;
            b.Expand(0.3f);

            Vector2Int min = WorldToCell(new Vector2(b.min.x, b.min.y));
            Vector2Int max = WorldToCell(new Vector2(b.max.x, b.max.y));

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                    if (InBounds(x, y)) grid[x, y] = 1;
        }

        // 标记屏幕边界
        for (int x = 0; x < gridW; x++) { grid[x, 0] = 1; grid[x, gridH - 1] = 1; }
        for (int y = 0; y < gridH; y++) { grid[0, y] = 1; grid[gridW - 1, y] = 1; }
    }

    /// <summary>A* 算法：支持 8 方向移动（含对角线）</summary>
    static List<Vector2> AStar(Vector2Int start, Vector2Int end)
    {
        var openList = new List<NodeCompare>();
        var closedSet = new HashSet<int>();
        var gScore = new Dictionary<int, float>();
        var cameFrom = new Dictionary<int, Vector2Int>();

        int Key(Vector2Int v) => v.x * 10000 + v.y;
        float H(Vector2Int a) => Mathf.Abs(a.x - end.x) + Mathf.Abs(a.y - end.y);

        int sk = Key(start);
        gScore[sk] = 0;
        openList.Add(new NodeCompare(start, H(start)));

        // 8 方向：上下左右 + 四个对角
        Vector2Int[] dirs = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(-1,1),
            new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        int iterations = 0;
        int maxIter = gridW * gridH * 2; // 防止无限循环

        while (openList.Count > 0 && iterations < maxIter)
        {
            iterations++;

            // 找 f 值最小的节点（简单线性扫描，稳定可靠）
            int bestIdx = 0;
            for (int i = 1; i < openList.Count; i++)
                if (openList[i].f < openList[bestIdx].f)
                    bestIdx = i;

            var current = openList[bestIdx];
            openList.RemoveAt(bestIdx);

            int ck = Key(current.pos);
            if (closedSet.Contains(ck)) continue;
            closedSet.Add(ck);

            // 到达终点 → 回溯路径
            if (current.pos == end)
                return ReconstructPath(cameFrom, current.pos);

            foreach (var d in dirs)
            {
                Vector2Int nb = current.pos + d;
                int nk = Key(nb);
                if (!InBounds(nb) || grid[nb.x, nb.y] == 1 || closedSet.Contains(nk)) continue;

                // 对角线移动：检查两个相邻格是否通畅（防止穿墙角）
                if (d.x != 0 && d.y != 0)
                {
                    if (grid[current.pos.x + d.x, current.pos.y] == 1 ||
                        grid[current.pos.x, current.pos.y + d.y] == 1)
                        continue;
                }

                float moveCost = (d.x != 0 && d.y != 0) ? 1.414f : 1f;
                float tentativeG = gScore[ck] + moveCost;

                if (!gScore.ContainsKey(nk) || tentativeG < gScore[nk])
                {
                    cameFrom[nk] = current.pos;
                    gScore[nk] = tentativeG;
                    openList.Add(new NodeCompare(nb, tentativeG + H(nb)));
                }
            }
        }

        return new List<Vector2>(); // 无法到达
    }

    /// <summary>从终点回溯到起点，构建路径</summary>
    static List<Vector2> ReconstructPath(Dictionary<int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2>();
        path.Add(CellToWorld(current));
        int k = current.x * 10000 + current.y;
        while (cameFrom.ContainsKey(k))
        {
            current = cameFrom[k];
            path.Add(CellToWorld(current));
            k = current.x * 10000 + current.y;
        }
        path.Reverse();
        return path;
    }

    /// <summary>路径平滑：射线检测删除可直达的中间路径点</summary>
    static List<Vector2> SmoothPath(List<Vector2> path)
    {
        if (path.Count <= 2) return path;

        var smoothed = new List<Vector2> { path[0] };
        int i = 0;
        while (i < path.Count - 1)
        {
            int furthest = i + 1;
            // 从最远的点开始尝试，找到能直线到达的最远点
            for (int j = path.Count - 1; j > i + 1; j--)
            {
                if (IsClear(path[i], path[j]))
                {
                    furthest = j;
                    break;
                }
            }
            smoothed.Add(path[furthest]);
            i = furthest;
        }
        return smoothed;
    }

    /// <summary>检测两点之间是否没有障碍物</summary>
    static bool IsClear(Vector2 a, Vector2 b)
    {
        Vector2 dir = b - a;
        float dist = dir.magnitude;
        int steps = Mathf.CeilToInt(dist / (cellSize * 0.5f));
        for (int i = 1; i < steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
            Vector2Int c = WorldToCell(p);
            if (InBounds(c) && grid[c.x, c.y] == 1) return false;
        }
        return true;
    }

    /// <summary>查找距离指定格子最近的可通行格子</summary>
    static Vector2Int NearestFree(Vector2Int cell)
    {
        Vector2Int best = cell;
        float bestDist = float.MaxValue;

        for (int r = 0; r <= 15; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    Vector2Int nb = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (InBounds(nb) && grid[nb.x, nb.y] == 0)
                    {
                        float d = Mathf.Abs(dx) + Mathf.Abs(dy);
                        if (d < bestDist) { bestDist = d; best = nb; }
                    }
                }
            }
            if (bestDist < float.MaxValue) break;
        }
        return best;
    }

    // 坐标转换工具
    static Vector2Int WorldToCell(Vector2 w)
    {
        int x = Mathf.FloorToInt((w.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((w.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    static Vector2 CellToWorld(Vector2Int c)
    {
        return gridOrigin + new Vector2((c.x + 0.5f) * cellSize, (c.y + 0.5f) * cellSize);
    }

    static bool InBounds(Vector2Int c) => c.x >= 0 && c.x < gridW && c.y >= 0 && c.y < gridH;
    static bool InBounds(int x, int y) => x >= 0 && x < gridW && y >= 0 && y < gridH;

    /// <summary>A* 节点：存储格子坐标和 f 评分</summary>
    struct NodeCompare
    {
        public Vector2Int pos;
        public float f;
        public NodeCompare(Vector2Int p, float f) { this.pos = p; this.f = f; }
    }
}
