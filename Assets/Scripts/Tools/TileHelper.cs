using UnityEngine;

public static class TileHelper
{
    // axial (q,r) -> world position (pointy-top) for 2D (x,y)
    public static Vector3 HexToWorld(int q, int r, float size)
    {
        float width = Mathf.Sqrt(3f) * size;
        float x = width * (q + r * 0.5f);
        float y = (3f / 2f) * size * r;
        return new Vector3(x, y, 0f);
    }
}
