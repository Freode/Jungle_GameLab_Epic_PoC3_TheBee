using UnityEngine;
using System.Linq; // Linq 사용을 위해 추가

public static class LandHiveCommandHandler
{
    public static void ExecuteLand(UnitAgent queenAgent)
    {
        if (queenAgent == null || queenAgent.carriedHive == null)
        {
            Debug.LogWarning("[LandHive] 착륙 불가: 여왕벌이 하이브를 들고 있지 않습니다.");
            return;
        }

        // 1. 착륙 위치 유효성 검사
        int targetQ = queenAgent.q;
        int targetR = queenAgent.r;


        // 2. 착륙 실행
        queenAgent.carriedHive.LandHive(targetQ, targetR);
    }

    private static bool IsValidLandingSpot(int q, int r, UnitAgent queen)
    {
        if (TileManager.Instance == null) return false;

        // 1. 타일 존재 여부 확인
        var tile = TileManager.Instance.GetTile(q, r);
        if (tile == null) return false;

        // 2. ✅ [수정] 장애물/물 체크 
        // HexTile의 정의를 정확히 모르므로, 만약 walkable 같은 변수가 없다면 
        // 일단 null 체크만 하거나, 프로젝트에 맞는 변수(예: tile.isWalkable)로 교체하세요.
        // 여기서는 에러 방지를 위해 주석 처리하거나 기본값 true로 둡니다.
        // if (tile.isObstacle || tile.isWater) return false; 

        // 3. ✅ [수정] 해당 위치에 다른 유닛이 있는지 확인 (GetAllUnits 순회)
        var allUnits = TileManager.Instance.GetAllUnits();
        foreach (var unit in allUnits)
        {
            if (unit == null) continue;
            
            // 좌표가 같은지 확인
            if (unit.q == q && unit.r == r)
            {
                // 여왕벌 자신은 제외 (여왕벌 위치에 착륙하는 것이므로)
                if (unit == queen) continue;

                // 자원을 들고 지나가는 일꾼이나 투사체 등은 무시해도 된다면 조건 추가
                // 여기서는 '건물'이나 '다른 유닛'이 있으면 착륙 불가로 처리
                return false;
            }
        }

        return true;
    }
}