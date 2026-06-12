using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RDP 路径简化工具
/// 职责：使用 Ramer-Douglas-Peucker 算法简化点列表
///   减少碰撞段数量，提升物理计算性能
/// </summary>
public static class LineSimplifier
{
    /// <summary>
    /// 简化点列表
    /// </summary>
    /// <param name="points">原始点列表</param>
    /// <param name="epsilon">简化容差（越大越激进）</param>
    /// <returns>简化后的点列表</returns>
    public static List<Vector2> Simplify(List<Vector2> points, float epsilon)
    {
        if (points == null || points.Count < 3)
            return new List<Vector2>(points);

        // 找到距离首尾连线最远的点
        float maxDistance = 0f;
        int maxIndex = 0;
        int lastIndex = points.Count - 1;

        for (int i = 1; i < lastIndex; i++)
        {
            float distance = PerpendicularDistance(points[i], points[0], points[lastIndex]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                maxIndex = i;
            }
        }

        List<Vector2> result = new List<Vector2>();

        if (maxDistance > epsilon)
        {
            // 超过容差：以该点为界分段递归简化
            List<Vector2> leftPart = points.GetRange(0, maxIndex + 1);
            List<Vector2> rightPart = points.GetRange(maxIndex, lastIndex - maxIndex + 1);

            List<Vector2> leftResult = Simplify(leftPart, epsilon);
            List<Vector2> rightResult = Simplify(rightPart, epsilon);

            result.AddRange(leftResult);
            result.AddRange(rightResult.GetRange(1, rightResult.Count - 1));
        }
        else
        {
            // 所有点在容差内，只保留首尾
            result.Add(points[0]);
            result.Add(points[lastIndex]);
        }

        return result;
    }

    /// <summary>计算点到线段的垂直距离</summary>
    private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 lineVec = lineEnd - lineStart;
        float lineLength = lineVec.magnitude;

        if (lineLength < 0.0001f)
            return Vector2.Distance(point, lineStart);

        // 叉积法：|(P-A)×(B-A)| / |B-A|
        float crossProduct = Mathf.Abs(
            (point.x - lineStart.x) * (lineEnd.y - lineStart.y) -
            (point.y - lineStart.y) * (lineEnd.x - lineStart.x)
        );
        return crossProduct / lineLength;
    }
}
