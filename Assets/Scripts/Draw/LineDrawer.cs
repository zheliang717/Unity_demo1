using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 画线系统
/// 职责：
///   1. 监听鼠标/触摸输入，绘制线条
///   2. 绘制中：实时显示 LineRenderer + 创建碰撞段
///   3. 松手后：RDP 简化路径，切换刚体为 Dynamic 受重力下落
///   4. 线条作为刚体整体下落，被地形阻挡后停留
/// </summary>
public class LineDrawer : MonoBehaviour
{
    [Header("画线参数")]
    [SerializeField] private float minDist = 0.04f;       // 最小采样间距
    [SerializeField] private float simpEps = 0.12f;       // RDP 简化容差
    [SerializeField] private int maxPts = 500;             // 单条线最大点数

    [Header("外观参数")]
    [SerializeField] private Color color = Color.black;
    [SerializeField] private float lineW = 0.08f;          // 线条宽度
    [SerializeField] private float segThickness = 0.3f;   // 碰撞段厚度

    private Camera cam;
    private Material mat;
    private bool drawing;
    private List<Vector2> pts = new List<Vector2>();

    // 当前正在绘制的线
    private LineRenderer curLR;
    private Rigidbody2D curRB;
    private List<GameObject> curSegs = new List<GameObject>();

    // 所有已绘制的线（用于清理）
    private List<DrawnLine> activeLines = new List<DrawnLine>();

    private class DrawnLine
    {
        public LineRenderer lr;
        public List<GameObject> segs;
    }

    void Awake()
    {
        cam = Camera.main;
        // 兼容多种 Shader 可用性
        Shader s = Shader.Find("Unlit/Color");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("GUI/Text Shader");
        mat = new Material(s);
        mat.color = color;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        bool dn, hd, up;
        Vector2 p;
        GetInput(out dn, out hd, out up, out p);

        if (dn && !drawing) Begin(p);
        else if (hd && drawing) Move(p);
        else if (up && drawing) End();
    }

    /// <summary>获取输入状态：按下、按住、抬起 + 屏幕坐标</summary>
    void GetInput(out bool dn, out bool hd, out bool up, out Vector2 p)
    {
        dn = hd = up = false;
        p = Vector2.zero;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            p = t.position;
            dn = t.phase == TouchPhase.Began;
            hd = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
        }
        else
        {
            dn = Input.GetMouseButtonDown(0);
            hd = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);
            p = Input.mousePosition;
        }
    }

    /// <summary>屏幕坐标转世界坐标</summary>
    Vector2 S2W(Vector2 sp)
    {
        if (cam == null) return Vector2.zero;
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -cam.transform.position.z));
        return new Vector2(w.x, w.y);
    }

    /// <summary>开始绘制：创建容器对象 + LineRenderer + Rigidbody2D（Kinematic）</summary>
    void Begin(Vector2 sp)
    {
        drawing = true;
        pts.Clear();
        Vector2 wp = S2W(sp);
        pts.Add(wp);

        curSegs.Clear();

        // 容器对象：承载 LineRenderer、Rigidbody2D 和子碰撞段
        GameObject container = new GameObject("DrawLine");
        container.layer = LayerMask.NameToLayer("Line");
        container.transform.position = Vector3.zero;

        // Rigidbody2D：绘制时 Kinematic（不动），松手后切 Dynamic（受重力）
        curRB = container.AddComponent<Rigidbody2D>();
        curRB.bodyType = RigidbodyType2D.Kinematic;
        curRB.gravityScale = 0f;
        curRB.mass = 0.5f;
        curRB.drag = 0.5f;
        curRB.angularDrag = 3f;
        curRB.freezeRotation = false;
        curRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        curRB.interpolation = RigidbodyInterpolation2D.Interpolate;

        // LineRenderer：跟随容器移动，useWorldSpace=false 表示本地坐标
        curLR = container.AddComponent<LineRenderer>();
        curLR.material = mat;
        curLR.startColor = color;
        curLR.endColor = color;
        curLR.startWidth = lineW;
        curLR.endWidth = lineW;
        curLR.useWorldSpace = false;
        curLR.sortingOrder = 10;
        curLR.numCapVertices = 8;
        curLR.numCornerVertices = 8;
        curLR.alignment = LineAlignment.TransformZ;
        curLR.positionCount = 1;
        curLR.SetPosition(0, wp);

        // 第一个碰撞段
        AddSeg(wp, 0f, 0.15f);
    }

    /// <summary>绘制中：添加新点和碰撞段</summary>
    void Move(Vector2 sp)
    {
        Vector2 wp = S2W(sp);
        if (Vector2.Distance(wp, pts[pts.Count - 1]) < minDist) return;
        if (pts.Count >= maxPts) { End(); Begin(sp); return; } // 超长自动断线

        pts.Add(wp);

        Vector2 prev = pts[pts.Count - 2];
        Vector2 diff = wp - prev;
        float len = diff.magnitude;
        if (len < 0.01f) return;

        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        AddSeg(wp, angle, len);

        RefreshLR();
    }

    /// <summary>松手：简化路径、重建碰撞段、切换刚体为 Dynamic</summary>
    void End()
    {
        drawing = false;
        if (pts.Count < 2 || curLR == null || curSegs.Count < 2)
        {
            CleanupCurrent();
            return;
        }

        // RDP 简化：减少碰撞段数量
        List<Vector2> simplified = LineSimplifier.Simplify(pts, simpEps);
        if (simplified.Count < 2)
            simplified = new List<Vector2> { pts[0], pts[pts.Count - 1] };

        // 清除绘制过程中的临时段，用简化后的段替代
        ClearSegChildren();

        for (int i = 0; i < simplified.Count - 1; i++)
        {
            Vector2 a = simplified[i];
            Vector2 b = simplified[i + 1];
            Vector2 diff = b - a;
            float len = diff.magnitude;
            if (len < 0.01f) continue;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            Vector2 mid = (a + b) * 0.5f;
            AddSeg(mid, angle, len);
        }

        RefreshLR();

        // 切换为动态刚体，线条开始受重力下落
        curRB.bodyType = RigidbodyType2D.Dynamic;
        curRB.gravityScale = 1f;

        activeLines.Add(new DrawnLine { lr = curLR, segs = new List<GameObject>(curSegs) });

        curLR = null;
        curRB = null;
        curSegs.Clear();
    }

    /// <summary>创建一个碰撞段（子对象，带 BoxCollider2D）</summary>
    void AddSeg(Vector2 worldPos, float angleDeg, float length)
    {
        if (curLR == null) return;
        GameObject seg = new GameObject("Seg");
        seg.layer = curLR.gameObject.layer;
        seg.transform.SetParent(curLR.transform);
        seg.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
        seg.transform.rotation = Quaternion.Euler(0, 0, angleDeg);

        BoxCollider2D col = seg.AddComponent<BoxCollider2D>();
        col.size = new Vector2(length, segThickness);

        curSegs.Add(seg);
    }

    /// <summary>用当前绘制点更新 LineRenderer 显示</summary>
    void RefreshLR()
    {
        if (curLR == null) return;
        curLR.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
            curLR.SetPosition(i, pts[i]);
    }

    /// <summary>清除所有子碰撞段</summary>
    void ClearSegChildren()
    {
        if (curLR == null) return;
        for (int i = curLR.transform.childCount - 1; i >= 0; i--)
            Destroy(curLR.transform.GetChild(i).gameObject);
        curSegs.Clear();
    }

    /// <summary>清除当前正在绘制的线</summary>
    void CleanupCurrent()
    {
        if (curLR != null) Destroy(curLR.gameObject);
        curLR = null;
        curRB = null;
        curSegs.Clear();
    }

    /// <summary>清除所有已绘制的线</summary>
    public void ClearAllLines()
    {
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            var line = activeLines[i];
            if (line.lr != null) Destroy(line.lr.gameObject);
        }
        activeLines.Clear();
        CleanupCurrent();
    }
}
