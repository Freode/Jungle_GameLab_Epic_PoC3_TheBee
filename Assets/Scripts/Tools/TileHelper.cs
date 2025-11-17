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

    /// <summary>
    /// 타일 내부의 랜덤한 World 위치 반환 (육각형 범위 내)
    /// </summary>
    public static Vector3 GetRandomPositionInTile(int q, int r, float hexSize, float margin = 0.2f)
    {
        // 타일 중심 위치
        Vector3 center = HexToWorld(q, r, hexSize);
        
        // 육각형 내부 랜덤 위치 생성
        // Pointy-top hexagon의 경우, 반지름 내에서 랜덤 선택
        float maxRadius = hexSize * (1f - margin);
        
        // 육각형의 6개 섹션 중 하나를 선택하여 해당 범위 내에서만 이동
        // 이렇게 하면 더 균등하게 분포됨
        float sectorAngle = 60f * Mathf.Deg2Rad; // 육각형은 6개 섹터
        int sector = Random.Range(0, 6);
        float angle = (sector * sectorAngle) + Random.Range(-sectorAngle * 0.5f, sectorAngle * 0.5f);
        
        // 육각형 경계를 고려한 최대 거리 계산
        // 육각형의 경우 각도에 따라 최대 거리가 다름
        float angleOffset = Mathf.Abs(Mathf.Repeat(angle, sectorAngle) - sectorAngle * 0.5f);
        float maxDistAtAngle = maxRadius / Mathf.Cos(angleOffset);
        float distance = Random.Range(0f, Mathf.Min(Random.Range(0f, maxRadius), maxDistAtAngle));
        
        // 2D 평면에서 X, Y 좌표 계산 (Z는 0으로 고정)
        float randomX = Mathf.Cos(angle) * distance;
        float randomY = Mathf.Sin(angle) * distance;
        
        // 2D 게임이므로 X, Y 평면 사용 (Z는 항상 0)
        return new Vector3(center.x + randomX, center.y + randomY, 0f);
    }

    /// <summary>
    /// 현재 위치에서 타일 내부 다른 랜덤 위치로 이동
    /// </summary>
    public static Vector3 GetRandomPositionInCurrentTile(Vector3 currentPos, int q, int r, float hexSize, float margin = 0.2f)
    {
        Vector3 newPos = GetRandomPositionInTile(q, r, hexSize, margin);
        
        // 현재 위치와 너무 가까우면 다시 생성 (최대 3번 시도)
        int attempts = 0;
        while (Vector3.Distance(currentPos, newPos) < hexSize * 0.15f && attempts < 3)
        {
            newPos = GetRandomPositionInTile(q, r, hexSize, margin);
            attempts++;
        }
        
        return newPos;
    }

    /// <summary>
    /// 위치가 타일 내부에 있는지 확인
    /// </summary>
    public static bool IsPositionInTile(Vector3 worldPos, int q, int r, float hexSize)
    {
        Vector3 center = HexToWorld(q, r, hexSize);
        
        // 2D 평면에서 거리 계산 (X, Y만 사용)
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);
        Vector2 center2D = new Vector2(center.x, center.y);
        float distance = Vector2.Distance(worldPos2D, center2D);
        
        // 육각형 반지름 내부인지 확인
        return distance <= hexSize * 0.95f; // 약간의 여유 (5%)
    }
}
