using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 线条对象池（当前未使用，保留接口兼容）
/// LineDrawer 已改为直接创建 GameObject，不再依赖此池
/// </summary>
public class LineObjectPool : MonoBehaviour
{
    private Queue<GameObject> availableQueue = new Queue<GameObject>();
    private LinkedList<GameObject> activeLines = new LinkedList<GameObject>();

    public GameObject GetLine()
    {
        if (availableQueue.Count > 0)
            return availableQueue.Dequeue();
        return new GameObject("PooledLine");
    }

    public void ReturnLine(GameObject line)
    {
        if (line == null) return;
        activeLines.Remove(line);
        line.SetActive(false);
        availableQueue.Enqueue(line);
    }

    public void ReturnAll()
    {
        while (activeLines.Count > 0)
        {
            GameObject line = activeLines.First.Value;
            activeLines.RemoveFirst();
            line.SetActive(false);
            availableQueue.Enqueue(line);
        }
    }
}
