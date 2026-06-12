using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏全局状态管理器（单例模式）
/// 职责：
///   1. 管理游戏状态切换（Waiting → Playing → Failed/Won）
///   2. 倒计时逻辑
///   3. 对外提供状态查询和事件通知
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Waiting, Playing, Failed, Won }

    [Header("游戏配置")]
    [SerializeField] private float gameDuration = 10f;

    [Header("狗头引用")]
    [SerializeField, Tooltip("底部狗头的 Transform，蜜蜂的追踪目标")]
    private Transform dogTarget;

    [Header("UI 引用")]
    [SerializeField] private UIManager uiManager;

    private GameState currentState = GameState.Waiting;
    private float remainingTime;

    // 公开属性：供其他模块读取
    public GameState CurrentState => currentState;
    public float RemainingTime => remainingTime;
    public Transform DogTarget => dogTarget;
    public bool IsPlaying => currentState == GameState.Playing;

    // 事件：状态变化通知
    public System.Action<GameState> OnStateChanged;
    public System.Action<float> OnTimerUpdated;

    private void Awake()
    {
        // 单例：场景重载时保留唯一实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        // 倒计时递减
        remainingTime -= Time.deltaTime;
        OnTimerUpdated?.Invoke(remainingTime);

        if (uiManager != null)
            uiManager.UpdateTimer(remainingTime);

        // 时间到 → 胜利
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            TriggerVictory();
        }
    }

    /// <summary>开始游戏，重置倒计时</summary>
    public void StartGame()
    {
        remainingTime = 10f;
        SetState(GameState.Playing);
    }

    /// <summary>重新开始：重新加载当前场景</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>蜜蜂碰到狗头 → 失败</summary>
    public void TriggerFailure()
    {
        if (currentState == GameState.Playing)
            SetState(GameState.Failed);
    }

    /// <summary>倒计时归零 → 胜利</summary>
    public void TriggerVictory()
    {
        if (currentState == GameState.Playing)
            SetState(GameState.Won);
    }

    private void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"[GameManager] 状态: {newState}");
        OnStateChanged?.Invoke(newState);

        // 失败或胜利时暂停游戏
        if (newState == GameState.Failed || newState == GameState.Won)
            Time.timeScale = 0f;
    }
}
