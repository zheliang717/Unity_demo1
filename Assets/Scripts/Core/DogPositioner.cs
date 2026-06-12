using UnityEngine;
using System.Collections;

/// <summary>
/// 狗头位置设置器
/// 职责：等 LevelManager 加载完地形后，将狗头放置到关卡指定的出生位置
/// 使用协程延迟 0.15 秒，确保 LevelManager.Start() 已执行完毕
/// </summary>
public class DogPositioner : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.15f);
        if (LevelManager.Instance != null)
        {
            transform.position = new Vector3(
                LevelManager.Instance.dogSpawnPos.x,
                LevelManager.Instance.dogSpawnPos.y,
                0);
            Debug.Log($"[DogPositioner] 狗头位置: {transform.position}");
        }
    }
}
