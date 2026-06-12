using UnityEngine;

/// <summary>
/// UI 管理器（IMGUI 方案）
/// 职责：
///   1. HUD：倒计时数字（红色+摇晃+脉冲）+ 关卡名 + 底部提示
///   2. 结果弹窗：通关/失败提示 + 下一关/重试按钮
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("闪烁配置")]
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private float blinkInterval = 0.5f;

    private float blinkTimer;
    private bool blinkVisible = true;

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    void Update()
    {
        // 倒计时进入警告阈值后开始闪烁
        if (GameManager.Instance != null &&
            GameManager.Instance.IsPlaying &&
            GameManager.Instance.RemainingTime <= warningThreshold)
        {
            blinkTimer += Time.unscaledDeltaTime;
            if (blinkTimer >= blinkInterval) { blinkTimer = 0f; blinkVisible = !blinkVisible; }
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void OnGUI()
    {
        DrawHUD();
        DrawResult();
    }

    /// <summary>绘制 HUD：倒计时 + 关卡名 + 提示文字</summary>
    void DrawHUD()
    {
        if (GameManager.Instance == null) return;
        float remaining = GameManager.Instance.RemainingTime;
        float sw = Screen.width;
        float sh = Screen.height;

        // 倒计时数字
        int secs = Mathf.CeilToInt(remaining);
        if (secs <= 0) secs = 0;
        string timeStr = secs.ToString();

        // 颜色：红色，最后 3 秒脉冲闪烁
        Color c = Color.red;
        if (remaining <= 3f)
        {
            float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));
            c = Color.Lerp(new Color(1f, 0.15f, 0.15f), new Color(1f, 0.6f, 0.6f), pulse);
        }

        // 正弦摇晃效果（顶部左摇时底部右摇）
        float swayAngle = Mathf.Sin(Time.unscaledTime * 6f) * 8f;

        GUIStyle timeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(sw * 0.18f),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = c },
            fontStyle = FontStyle.Bold
        };

        // 屏幕中上方
        float cx = sw * 0.5f;
        float cy = sh * 0.2f;
        float labelW = sw * 0.4f;
        float labelH = sh * 0.12f;

        Vector2 pivot = new Vector2(cx, cy);
        GUIUtility.RotateAroundPivot(swayAngle, pivot);
        GUI.Label(new Rect(cx - labelW / 2f, cy - labelH / 2f, labelW, labelH), timeStr, timeStyle);
        GUI.matrix = Matrix4x4.identity;

        // 关卡名
        string levelName = "第1关";
        if (LevelManager.Instance != null)
        {
            int lv = LevelManager.Instance.CurrentLevel;
            levelName = "第" + lv + "关";
        }

        GUIStyle levelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(sw * 0.04f),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.5f) }
        };
        GUI.Label(new Rect(0, sh * 0.03f, sw, sh * 0.04f), levelName, levelStyle);

        // 底部操作提示
        if (GameManager.Instance.IsPlaying)
        {
            GUIStyle tip = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)(sw * 0.035f),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 1f, 1f, 0.4f) }
            };
            GUI.Label(new Rect(0, sh - sh * 0.06f, sw, sh * 0.04f), "按住屏幕画线 · 阻挡蜜蜂", tip);
        }
    }

    /// <summary>绘制结果弹窗：通关/失败 + 按钮</summary>
    void DrawResult()
    {
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.CurrentState;
        if (state != GameManager.GameState.Failed && state != GameManager.GameState.Won) return;

        float sw = Screen.width;
        float sh = Screen.height;

        // 半透明遮罩
        GUI.color = new Color(0, 0, 0, 0.65f);
        GUI.Box(new Rect(0, 0, sw, sh), "");
        GUI.color = Color.white;

        // 弹窗尺寸和位置
        float bw = sw * 0.72f;
        float bh = sh * 0.28f;
        float bx = (sw - bw) / 2f;
        float by = (sh - bh) / 2f;
        Rect box = new Rect(bx, by, bw, bh);

        // 弹窗背景
        GUI.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        GUI.Box(box, "");
        GUI.color = Color.white;

        bool isWin = state == GameManager.GameState.Won;

        // 标题
        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(bw * 0.14f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = isWin ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.3f) }
        };
        GUI.Label(new Rect(bx, by + bh * 0.12f, bw, bh * 0.25f), isWin ? "通关!" : "失败!", title);

        // 副标题
        GUIStyle sub = new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)(bw * 0.07f),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };
        string msg = isWin ? "狗头安全了!" : "蜜蜂蛰到了狗头...";
        GUI.Label(new Rect(bx, by + bh * 0.38f, bw, bh * 0.2f), msg, sub);

        // 按钮
        float btnW = bw * 0.5f;
        float btnH = bh * 0.18f;
        float btnX = bx + (bw - btnW) / 2f;
        float btnY = by + bh - btnH - bh * 0.1f;
        Rect btnRect = new Rect(btnX, btnY, btnW, btnH);

        GUIStyle btn = new GUIStyle(GUI.skin.button)
        {
            fontSize = (int)(btnW * 0.1f),
            fontStyle = FontStyle.Bold
        };

        if (isWin)
        {
            if (GUI.Button(btnRect, "下一关", btn))
            {
                Time.timeScale = 1f;
                if (LevelManager.Instance != null)
                    LevelManager.Instance.LoadLevel(LevelManager.Instance.CurrentLevel + 1);
                GameManager.Instance.RestartGame();
            }
        }
        else
        {
            if (GUI.Button(btnRect, "重试", btn))
                GameManager.Instance.RestartGame();
        }
    }

    void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
        {
            blinkTimer = 0f;
            blinkVisible = true;
        }
    }

    public void UpdateTimer(float remainingSeconds) { }
}
