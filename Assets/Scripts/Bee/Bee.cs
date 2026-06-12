using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 蜜蜂个体行为控制
/// 职责：
///   1. 通过 A* 寻路计算最优路径到达狗头
///   2. 沿路径点移动，定期重新计算（狗头可能被推动）
///   3. 碰撞处理：碰到狗头触发失败，碰到障碍物重新寻路并推挤线条
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bee : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float maxSpeed = 16f;
    [SerializeField] private float maxLife = 60f;

    private Rigidbody2D rb;
    private Transform dogTarget;
    private float life;
    private bool active;
    private System.Action<Bee> onReturn;
    private Vector2 curVel;

    // A* 寻路结果
    private List<Vector2> waypoints;
    private int wpIndex;
    private float repathTimer;

    public bool IsActive => active;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 零弹性物理材质，防止碰撞反弹
        PhysicsMaterial2D mat = new PhysicsMaterial2D("BeeFlat");
        mat.friction = 0f;
        mat.bounciness = 0f;
        rb.sharedMaterial = mat;
        rb.mass = 1.5f;
        rb.drag = 3f;
        rb.angularDrag = 5f;
    }

    void FixedUpdate()
    {
        if (!active) return;
        life += Time.fixedDeltaTime;
        if (life >= maxLife) { Return(); return; }
        if (dogTarget == null) return;

        float dt = Time.fixedDeltaTime;
        Vector2 pos = (Vector2)transform.position;

        // 每 0.5 秒重新计算路径（狗头位置可能变化）
        repathTimer += dt;
        if (repathTimer > 0.5f || waypoints == null || wpIndex >= waypoints.Count)
        {
            CalcPath(pos);
            repathTimer = 0f;
        }

        // 沿路径点移动
        Vector2 target = waypoints[wpIndex];
        Vector2 toTarget = (target - pos).normalized;

        // 到达当前路径点 → 切换到下一个
        if (Vector2.Distance(pos, target) < 0.5f)
        {
            wpIndex++;
            if (wpIndex >= waypoints.Count)
            {
                CalcPath(pos);
                return;
            }
            target = waypoints[wpIndex];
            toTarget = (target - pos).normalized;
        }

        // 平滑加速到目标方向
        curVel = Vector2.MoveTowards(curVel, toTarget * speed, 50f * dt);
        curVel = Vector2.ClampMagnitude(curVel, maxSpeed);
        rb.velocity = curVel;
    }

    /// <summary>调用 A* 寻路获取从当前位置到狗头的路径</summary>
    void CalcPath(Vector2 pos)
    {
        if (dogTarget == null) return;
        waypoints = BeePathfinder.FindPath(pos, (Vector2)dogTarget.position);
        wpIndex = 0;

        // 跳过距离过近的路径点
        while (wpIndex < waypoints.Count - 1 && Vector2.Distance(pos, waypoints[wpIndex]) < 0.5f)
            wpIndex++;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        int l = col.gameObject.layer;

        // 碰到狗头 → 推开狗头 + 游戏失败
        if (l == LayerMask.NameToLayer("Dog"))
        {
            if (col.rigidbody != null)
            {
                Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector2.up;
                col.rigidbody.AddForce(pushDir * speed * 2f, ForceMode2D.Impulse);
            }
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
                GameManager.Instance.TriggerFailure();
            return;
        }

        // 碰到障碍物（地形/线条）→ 清空路径触发重新寻路 + 滑行
        if (l == LayerMask.NameToLayer("Boundary") || l == LayerMask.NameToLayer("Line"))
        {
            waypoints = null; // 下一帧会重新计算

            // 碰到玩家画的线条时施加推力
            if (col.rigidbody != null && l == LayerMask.NameToLayer("Line"))
            {
                Vector2 pushDir = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
                col.rigidbody.AddForce(pushDir * speed * 3f, ForceMode2D.Impulse);
            }

            // 沿墙壁滑行
            if (col.contacts.Length > 0)
            {
                Vector2 n = col.contacts[0].normal;
                float dot = Vector2.Dot(curVel, n);
                if (dot < 0) curVel -= n * dot * 0.5f;
                curVel = curVel.normalized * speed;
                rb.velocity = curVel;
            }
        }
    }

    /// <summary>由 BeeSpawner 调用，初始化蜜蜂参数</summary>
    public void OnSpawn(Vector2 pos, Transform target, System.Action<Bee> cb)
    {
        transform.position = pos;
        dogTarget = target;
        onReturn = cb;
        active = true;
        life = 0f;
        repathTimer = 0f;
        waypoints = null;
        wpIndex = 0;

        // 初始速度：朝向狗头并带随机偏移
        Vector2 toT = target != null ? ((Vector2)target.position - pos).normalized : Vector2.down;
        curVel = (Quaternion.Euler(0, 0, Random.Range(-20f, 20f)) * toT).normalized * speed;
        rb.velocity = curVel;
        rb.angularVelocity = 0f;
    }

    void Return()
    {
        active = false;
        rb.velocity = Vector2.zero;
        onReturn?.Invoke(this);
    }

    public void ReturnToPool() => Return();
}
