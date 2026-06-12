using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 关卡管理器
/// 职责：
///   1. 根据关卡编号生成对应的障碍物布局
///   2. 管理墙体的创建和销毁
///   3. 存储小狗出生位置
///   4. 通过静态变量跨场景重载持久化关卡编号
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("墙体外观")]
    [SerializeField] private Color wallColor = new Color(0.3f, 0.55f, 0.15f, 0.6f);
    [SerializeField] private float wallWidth = 0.15f;

    private static int s_nextLevel = 1; // 跨场景持久化

    public int CurrentLevel { get; private set; }
    public List<Collider2D> terrainWalls = new List<Collider2D>(); // 所有墙体碰撞器
    public Vector2 dogSpawnPos; // 小狗出生位置

    private Transform wallsParent;

    void Awake()
    {
        Instance = this;
        CurrentLevel = s_nextLevel;
        wallsParent = new GameObject("Walls").transform;
        wallsParent.SetParent(transform);
    }

    void Start()
    {
        LoadLevel(CurrentLevel);
    }

    /// <summary>加载指定关卡</summary>
    public void LoadLevel(int levelIndex)
    {
        s_nextLevel = levelIndex;
        ClearWalls();
        CurrentLevel = levelIndex;

        switch (levelIndex)
        {
            case 1: BuildLevel1(); break;
            case 2: BuildLevel2(); break;
            case 3: BuildLevel3(); break;
            default: BuildLevel1(); break;
        }

        Debug.Log($"[LevelManager] 关卡{levelIndex} 加载完成, 墙体数={terrainWalls.Count}");
    }

    void ClearWalls()
    {
        terrainWalls.Clear();
        if (wallsParent != null)
            foreach (Transform child in wallsParent)
                Destroy(child.gameObject);
    }

    // ============================================================
    //  关卡1：锯齿通道（简单）
    //  5道横墙交替留左/右缺口，形成 Z 字形路径
    // ============================================================
    void BuildLevel1()
    {
        CreateWall("L1_WL", new Vector2(-4.5f, 0f), new Vector2(0.5f, 20f), 0);
        CreateWall("L1_WR", new Vector2(4.5f, 0f), new Vector2(0.5f, 20f), 0);
        CreateWall("L1_A", new Vector2(-2f, 6f), new Vector2(5f, 0.4f), 0);
        CreateWall("L1_B", new Vector2(2f, 3.5f), new Vector2(5f, 0.4f), 0);
        CreateWall("L1_C", new Vector2(-2f, 1f), new Vector2(5f, 0.4f), 0);
        CreateWall("L1_D", new Vector2(2f, -1.5f), new Vector2(5f, 0.4f), 0);
        CreateWall("L1_E", new Vector2(-2f, -4f), new Vector2(5f, 0.4f), 0);
        dogSpawnPos = new Vector2(0, -8f);
    }

    // ============================================================
    //  关卡2：走廊迷宫（中等）
    //  竖墙+横墙组合出房间、走廊和瓶颈
    // ============================================================
    void BuildLevel2()
    {
        CreateWall("L2_WL", new Vector2(-4.5f, 0f), new Vector2(0.5f, 20f), 0);
        CreateWall("L2_WR", new Vector2(4.5f, 0f), new Vector2(0.5f, 20f), 0);
        CreateWall("L2_V1", new Vector2(-1.5f, 7f), new Vector2(0.4f, 3f), 0);
        CreateWall("L2_V2", new Vector2(1.5f, 7f), new Vector2(0.4f, 3f), 0);
        CreateWall("L2_H1L", new Vector2(-3f, 5.5f), new Vector2(3f, 0.4f), 0);
        CreateWall("L2_H1R", new Vector2(3f, 5.5f), new Vector2(3f, 0.4f), 0);
        CreateWall("L2_H2L", new Vector2(-2.5f, 3.5f), new Vector2(3f, 0.4f), 0);
        CreateWall("L2_H2R", new Vector2(2.5f, 3.5f), new Vector2(3f, 0.4f), 0);
        CreateWall("L2_V3", new Vector2(0, 2f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L2_H3L", new Vector2(-2.5f, 0.5f), new Vector2(2.5f, 0.4f), 0);
        CreateWall("L2_H3R", new Vector2(2.5f, 0.5f), new Vector2(2.5f, 0.4f), 0);
        CreateWall("L2_V4", new Vector2(-1.5f, -1.5f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L2_V5", new Vector2(1.5f, -1.5f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L2_H4L", new Vector2(-3f, -3.5f), new Vector2(3f, 0.4f), 0);
        CreateWall("L2_H4R", new Vector2(3f, -3.5f), new Vector2(3f, 0.4f), 0);
        CreateWall("L2_H5L", new Vector2(-2.5f, -5.5f), new Vector2(2.5f, 0.4f), 0);
        CreateWall("L2_H5R", new Vector2(2.5f, -5.5f), new Vector2(2.5f, 0.4f), 0);
        dogSpawnPos = new Vector2(0, -8f);
    }

    // ============================================================
    //  关卡3：蛇形迷宫（较难）
    //  7层横墙交错 + 5道竖墙阻挡，底部收窄
    // ============================================================
    void BuildLevel3()
    {
        CreateWall("L3_WL", new Vector2(-4.5f, 0f), new Vector2(0.5f, 20f), 0);
        CreateWall("L3_WR", new Vector2(4.5f, 0f), new Vector2(0.5f, 20f), 0);
        CreateWall("L3_A", new Vector2(-1f, 7.5f), new Vector2(5.5f, 0.4f), 0);
        CreateWall("L3_B", new Vector2(1f, 5.8f), new Vector2(5.5f, 0.4f), 0);
        CreateWall("L3_V1", new Vector2(-1.5f, 4.5f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L3_C", new Vector2(-1f, 3.8f), new Vector2(4f, 0.4f), 0);
        CreateWall("L3_V2", new Vector2(1.5f, 2.5f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L3_D", new Vector2(1f, 1.8f), new Vector2(4f, 0.4f), 0);
        CreateWall("L3_V3", new Vector2(-1.5f, 0.5f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L3_E", new Vector2(-1f, -0.2f), new Vector2(4f, 0.4f), 0);
        CreateWall("L3_V4", new Vector2(1.5f, -1.5f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L3_F", new Vector2(1f, -2.2f), new Vector2(4f, 0.4f), 0);
        CreateWall("L3_V5", new Vector2(-1.5f, -3.5f), new Vector2(0.4f, 2.5f), 0);
        CreateWall("L3_G", new Vector2(-1f, -4.2f), new Vector2(4f, 0.4f), 0);
        CreateWall("L3_VL", new Vector2(-2f, -6f), new Vector2(0.4f, 2f), 0);
        CreateWall("L3_VR", new Vector2(2f, -6f), new Vector2(0.4f, 2f), 0);
        dogSpawnPos = new Vector2(0, -8f);
    }

    // ============================================================
    //  墙体创建工具方法
    // ============================================================

    /// <summary>创建矩形墙体（带碰撞器+可视化）</summary>
    GameObject CreateWall(string name, Vector2 pos, Vector2 size, float rotationDeg)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(wallsParent);
        obj.transform.position = new Vector3(pos.x, pos.y, 0);
        obj.transform.rotation = Quaternion.Euler(0, 0, rotationDeg);
        obj.layer = LayerMask.NameToLayer("Boundary");

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = size;
        terrainWalls.Add(col);

        // 半透明绿色棋盘格纹理
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite();
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = wallColor;
        sr.sortingOrder = 1;
        sr.transform.localScale = Vector3.one;

        return obj;
    }

    /// <summary>创建沿两点连线的墙体</summary>
    GameObject CreateWallSegment(string name, Vector2 start, Vector2 end)
    {
        Vector2 mid = (start + end) / 2f;
        Vector2 diff = end - start;
        float len = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        return CreateWall(name, mid, new Vector2(len, wallWidth * 2f), angle);
    }

    /// <summary>生成棋盘格纹理 Sprite</summary>
    Sprite CreateSolidSprite()
    {
        int s = 8;
        Texture2D t = new Texture2D(s, s);
        Color[] pixels = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                pixels[y * s + x] = (x + y) % 2 == 0
                    ? new Color(0.3f, 0.55f, 0.15f, 0.5f)
                    : new Color(0.25f, 0.5f, 0.12f, 0.5f);
        t.SetPixels(pixels);
        t.Apply();
        t.filterMode = FilterMode.Point;
        return Sprite.Create(t, new Rect(0, 0, s, s), Vector2.one * 0.5f, 1);
    }
}
