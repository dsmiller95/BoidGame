using UnityEngine;

public static class VectorUtil
{
    public static Vector2 MultComponents(Vector2Int a, Vector2 b)
    {
        return new Vector2(a.x * b.x, a.y * b.y);
    }
}