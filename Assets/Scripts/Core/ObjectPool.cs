using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 通用对象池
/// 职责：
///   1. 预生成对象避免运行时 Instantiate 开销
///   2. 复用已回收对象，减少 GC 压力
///   3. 支持动态扩展（池耗尽时自动创建新对象）
/// </summary>
/// <typeparam name="T">池管理的 Component 类型</typeparam>
public class ObjectPool<T> where T : Component
{
    private readonly T prefab;
    private readonly Queue<T> availableQueue = new Queue<T>();
    private readonly List<T> allObjects = new List<T>();
    private readonly Transform parent;

    public int Count => allObjects.Count;
    public int AvailableCount => availableQueue.Count;

    public ObjectPool(T prefab, Transform parent = null, int initialCapacity = 10)
    {
        this.prefab = prefab;
        this.parent = parent;
        Prewarm(initialCapacity);
    }

    /// <summary>预生成指定数量的对象</summary>
    private void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T obj = CreateNew();
            obj.gameObject.SetActive(false);
            availableQueue.Enqueue(obj);
        }
    }

    /// <summary>从池中获取一个对象</summary>
    public T Get()
    {
        T obj;
        if (availableQueue.Count > 0)
            obj = availableQueue.Dequeue();
        else
        {
            obj = CreateNew();
            Debug.LogWarning($"[ObjectPool] 动态扩展。总数: {allObjects.Count}");
        }
        obj.gameObject.SetActive(true);
        return obj;
    }

    /// <summary>归还对象到池中</summary>
    public void Return(T obj)
    {
        if (obj == null) return;
        obj.gameObject.SetActive(false);
        availableQueue.Enqueue(obj);
    }

    /// <summary>归还所有活跃对象</summary>
    public void ReturnAll()
    {
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.gameObject.activeSelf)
            {
                obj.gameObject.SetActive(false);
                availableQueue.Enqueue(obj);
            }
        }
    }

    private T CreateNew()
    {
        T newObj = Object.Instantiate(prefab, parent);
        allObjects.Add(newObj);
        return newObj;
    }
}
