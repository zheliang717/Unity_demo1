using UnityEngine;

/// <summary>
/// 物理碰撞矩阵初始化器
/// 职责：在运行时强制设置 2D 碰撞矩阵
///   - Line ↔ Bee：线条阻挡蜜蜂
///   - Line ↔ Boundary：线条被地形挡住
///   - Line ↔ Dog：线条可以推动狗头
///   - Bee ↔ Dog：蜜蜂碰到狗头触发失败
///   - Bee ↔ Boundary：蜜蜂被地形阻挡
///   - Dog ↔ Boundary：狗头被地形阻挡
///   - Bee ↔ Bee：蜜蜂之间不碰撞
/// </summary>
public class PhysicsInit : MonoBehaviour
{
    void Awake()
    {
        int line = LayerMask.NameToLayer("Line");
        int bee = LayerMask.NameToLayer("Bee");
        int dog = LayerMask.NameToLayer("Dog");
        int boundary = LayerMask.NameToLayer("Boundary");

        Debug.Log($"[PhysicsInit] Layers: Line={line}, Bee={bee}, Dog={dog}, Bound={boundary}");

        if (line < 0 || bee < 0 || dog < 0 || boundary < 0)
        {
            Debug.LogError("[PhysicsInit] 层级未设置！请在 Tags and Layers 中添加 Line, Bee, Dog, Boundary");
            return;
        }

        // 先禁用所有碰撞
        for (int i = 0; i < 32; i++)
            for (int j = 0; j < 32; j++)
                Physics2D.IgnoreLayerCollision(i, j, true);

        // 按需开启碰撞
        Physics2D.IgnoreLayerCollision(line, bee, false);
        Physics2D.IgnoreLayerCollision(line, boundary, false);
        Physics2D.IgnoreLayerCollision(line, dog, false);
        Physics2D.IgnoreLayerCollision(boundary, dog, false);
        Physics2D.IgnoreLayerCollision(bee, dog, false);
        Physics2D.IgnoreLayerCollision(bee, boundary, false);
        Physics2D.IgnoreLayerCollision(bee, bee, true);

        Debug.Log("[PhysicsInit] 碰撞矩阵已初始化");
    }
}
