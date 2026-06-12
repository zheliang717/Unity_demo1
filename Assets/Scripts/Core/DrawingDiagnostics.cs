using UnityEngine;

/// <summary>
/// 画线诊断组件
/// 职责：在屏幕左上角显示当前画线状态和鼠标世界坐标，辅助调试
/// </summary>
public class DrawingDiagnostics : MonoBehaviour
{
    void OnGUI()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            GUI.Label(new Rect(5, 80, 350, 25), "等待游戏开始...");
            return;
        }

        bool mouseDown = Input.GetMouseButton(0) || (Input.touchCount > 0);
        string status = mouseDown ? "画线中" : "按住鼠标可画线";
        Color c = mouseDown ? Color.green : Color.yellow;

        GUIStyle s = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = c }
        };
        GUI.Label(new Rect(5, 80, 350, 25), status, s);

        if (mouseDown && Camera.main != null)
        {
            Vector3 mp = Input.mousePosition;
            if (Input.touchCount > 0) mp = Input.GetTouch(0).position;
            Vector3 wp = Camera.main.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 10f));
            GUI.Label(new Rect(5, 100, 350, 25),
                $"世界坐标: ({wp.x:F1}, {wp.y:F1})", s);
        }
    }
}
