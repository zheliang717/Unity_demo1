using UnityEngine;

/// <summary>
/// 蜜蜂生成器
/// 职责：
///   1. 游戏开始时从左右蜂巢批量生成蜜蜂
///   2. 所有蜜蜂在指定时间内出完（默认 2 秒内出 40 只）
///   3. 使用对象池管理蜜蜂生命周期
/// </summary>
public class BeeSpawner : MonoBehaviour
{
    [Header("蜂巢位置")]
    [SerializeField] private Transform leftHive;
    [SerializeField] private Transform rightHive;

    [Header("生成参数")]
    [SerializeField] private int maxBeeCount = 40;
    [SerializeField] private float totalSpawnDuration = 2f;

    [Header("蜜蜂预制体")]
    [SerializeField] private Bee beePrefab;

    private ObjectPool<Bee> beePool;
    private int activeBeeCount;
    private Transform dogTarget;
    private bool spawnFromLeft = true;
    private int totalToSpawn;
    private int spawned;
    private float spawnTimer;
    private float spawnInterval;
    private bool spawning;

    void Awake()
    {
        beePool = new ObjectPool<Bee>(beePrefab, transform, maxBeeCount);
    }

    void Start()
    {
        dogTarget = GameManager.Instance?.DogTarget;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
            // 兼容：如果 GameManager 已经在 Playing 状态（场景重载时序问题）
            if (GameManager.Instance.IsPlaying)
                OnGameStateChanged(GameManager.GameState.Playing);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (!spawning) return;

        // 按固定间隔生成蜜蜂
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f && spawned < totalToSpawn && activeBeeCount < maxBeeCount)
        {
            SpawnOneBee();
            spawned++;
            spawnTimer = spawnInterval;
        }

        if (spawned >= totalToSpawn)
            spawning = false;
    }

    /// <summary>从左右蜂巢交替生成一只蜜蜂</summary>
    void SpawnOneBee()
    {
        Transform hive = spawnFromLeft ? leftHive : rightHive;
        spawnFromLeft = !spawnFromLeft;
        if (hive == null) return;

        Bee bee = beePool.Get();
        if (bee == null) return;

        // 蜂巢位置 + 随机偏移
        Vector2 spawnPos = (Vector2)hive.position + new Vector2(
            Random.Range(-0.3f, 0.3f),
            Random.Range(-0.4f, 0.4f)
        );

        activeBeeCount++;
        bee.OnSpawn(spawnPos, dogTarget, OnBeeReturned);
    }

    void OnBeeReturned(Bee bee)
    {
        activeBeeCount--;
        beePool.Return(bee);
    }

    void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
        {
            // 开始生成：计算每只蜜蜂的间隔
            totalToSpawn = maxBeeCount;
            spawnInterval = totalSpawnDuration / maxBeeCount;
            spawnTimer = 0f;
            spawned = 0;
            spawning = true;
        }
        else if (state == GameManager.GameState.Failed || state == GameManager.GameState.Won)
        {
            // 游戏结束：回收所有蜜蜂
            beePool.ReturnAll();
            activeBeeCount = 0;
            spawning = false;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }
}
