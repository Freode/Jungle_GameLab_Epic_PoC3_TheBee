using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여왕벌 페르몬 효과 관리자
/// - 페르몬이 뿌려진 타일 추적
/// - 타일별 강조 효과 표시
/// </summary>
public class PheromoneManager : MonoBehaviour
{
    public static PheromoneManager Instance { get; private set; }
    
    [Header("페르몬 강조 설정")]
    [Tooltip("페르몬 강조선 크기 (기본보다 작게)")]
    public float pheromoneHighlightScale = 0.85f;
    
    [Tooltip("페르몬 강조선 두께")]
    public float pheromoneLineWidth = 0.04f;
    
    // 부대별 페르몬 타일 (q, r 좌표) - 최대 3개, FIFO 유지
    private Dictionary<WorkerSquad, HashSet<Vector2Int>> pheromoneTiles = new Dictionary<WorkerSquad, HashSet<Vector2Int>>()
    {
        { WorkerSquad.Squad1, new HashSet<Vector2Int>() },
        { WorkerSquad.Squad2, new HashSet<Vector2Int>() },
        { WorkerSquad.Squad3, new HashSet<Vector2Int>() }
    };

    // 추가 순서를 유지하기 위한 리스트 (FIFO)
    private Dictionary<WorkerSquad, List<Vector2Int>> pheromoneOrder = new Dictionary<WorkerSquad, List<Vector2Int>>()
    {
        { WorkerSquad.Squad1, new List<Vector2Int>() },
        { WorkerSquad.Squad2, new List<Vector2Int>() },
        { WorkerSquad.Squad3, new List<Vector2Int>() }
    };

    private const int MaxPheromonePerSquad = 3;

    // 동일 프레임 중복 요청 방지용
    private Dictionary<WorkerSquad, (Vector2Int coord, int frame)> lastPheromoneRequest = new Dictionary<WorkerSquad, (Vector2Int, int)>()
    {
        { WorkerSquad.Squad1, (new Vector2Int(int.MinValue, int.MinValue), -1) },
        { WorkerSquad.Squad2, (new Vector2Int(int.MinValue, int.MinValue), -1) },
        { WorkerSquad.Squad3, (new Vector2Int(int.MinValue, int.MinValue), -1) }
    };
    
    // ? 부대별 페르몬 색상 (HiveManager에서 가져옴) (요구사항 2)
    // private Dictionary는 제거하고 GetPheromoneColor 메서드로 대체
    
    // ? 타일별 부대 리스트 (겹침 추적) (요구사항 4)
    private Dictionary<Vector2Int, List<WorkerSquad>> tileSquads = new Dictionary<Vector2Int, List<WorkerSquad>>();
    
    // 타일별 LineRenderer 캐시 (부대별로 분리)
    private Dictionary<string, GameObject> pheromoneRenderers = new Dictionary<string, GameObject>(); // Key: "q_r_squad"
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    /// <summary>
    /// 부대별 페르몬 색상 가져오기 (HiveManager에서 참조) (요구사항 2)
    /// </summary>
    private Color GetPheromoneColor(WorkerSquad squad)
    {
        if (HiveManager.Instance == null)
        {
            // HiveManager가 없으면 기본 색상
            switch (squad)
            {
                case WorkerSquad.Squad1: return Color.red;
                case WorkerSquad.Squad2: return Color.green;
                case WorkerSquad.Squad3: return Color.blue;
                default: return Color.white;
            }
        }
        
        // ? HiveManager의 색상 참조
        switch (squad)
        {
            case WorkerSquad.Squad1: return HiveManager.Instance.squad1Color;
            case WorkerSquad.Squad2: return HiveManager.Instance.squad2Color;
            case WorkerSquad.Squad3: return HiveManager.Instance.squad3Color;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 페르몬 뿌리기 (타일 강조 효과 추가)
    /// </summary>
    public void AddPheromone(int q, int r, WorkerSquad squad)
    {
        Vector2Int coord = new Vector2Int(q, r);

        Debug.Log($"[페르몬] 요청: {squad} ({q}, {r}), 현재 리스트: {string.Join(" | ", pheromoneOrder[squad])}");

        // 동일 프레임에 같은 좌표 요청이면 무시 (중복 호출 방지)
        var last = lastPheromoneRequest[squad];
        if (last.frame == Time.frameCount && last.coord == coord)
        {
            Debug.Log($"[페르몬] {squad} 동일 프레임 중복 요청 무시: ({q}, {r})");
            return;
        }
        lastPheromoneRequest[squad] = (coord, Time.frameCount);

        // 이미 같은 타일에 뿌려져 있으면 삭제 후 종료 (총 개수 감소)
        if (pheromoneTiles[squad].Contains(coord))
        {
            RemovePheromone(coord, squad);
            Debug.Log($"[페르몬] {squad} 기존 타일에 재분사 → 삭제: ({q}, {r}), 삭제 후 리스트: {string.Join(" | ", pheromoneOrder[squad])}");
            return;
        }

        // 최대 개수 초과 시 가장 오래된 페르몬 제거 (FIFO)
        if (pheromoneOrder[squad].Count >= MaxPheromonePerSquad)
        {
            var oldest = pheromoneOrder[squad][0];
            RemovePheromone(oldest, squad);
            Debug.Log($"[페르몬] {squad} 최대치 초과, 가장 오래된 페르몬 제거: ({oldest.x}, {oldest.y})");
        }

        pheromoneTiles[squad].Add(coord);
        pheromoneOrder[squad].Add(coord);

        // 타일별 부대 추적 (겹침 고려)
        if (!tileSquads.ContainsKey(coord))
        {
            tileSquads[coord] = new List<WorkerSquad>();
        }

        if (!tileSquads[coord].Contains(squad))
        {
            tileSquads[coord].Add(squad);
        }

        RefreshPheromoneHighlightsForTile(coord);

        Debug.Log($"[페르몬] {squad} 페르몬 추가: ({q}, {r}), 현재 개수: {pheromoneOrder[squad].Count}");
    }
    
    /// <summary>
    /// 특정 부대의 모든 페르몬 제거
    /// </summary>
    public void ClearPheromone(WorkerSquad squad)
    {
        if (!pheromoneTiles.ContainsKey(squad)) return;

        // 리스트 복사 후 순회(제거 중 컬렉션 수정 방지)
        var coords = new List<Vector2Int>(pheromoneOrder[squad]);
        foreach (var coord in coords)
        {
            RemovePheromone(coord, squad);
        }

        pheromoneTiles[squad].Clear();
        pheromoneOrder[squad].Clear();
        lastPheromoneRequest[squad] = (new Vector2Int(int.MinValue, int.MinValue), -1);
        Debug.Log($"[페르몬] {squad} 페르몬 모두 제거");
    }
    
    /// <summary>
    /// 모든 페르몬 제거 (꿀벌집 파괴 시)
    /// </summary>
    public void ClearAllPheromones()
    {
        foreach (var squad in pheromoneTiles.Keys)
        {
            ClearPheromone(squad);
        }

        tileSquads.Clear();
        lastPheromoneRequest[WorkerSquad.Squad1] = (new Vector2Int(int.MinValue, int.MinValue), -1);
        lastPheromoneRequest[WorkerSquad.Squad2] = (new Vector2Int(int.MinValue, int.MinValue), -1);
        lastPheromoneRequest[WorkerSquad.Squad3] = (new Vector2Int(int.MinValue, int.MinValue), -1);

        Debug.Log("[페르몬] 모든 페르몬 제거");
    }
    
    /// <summary>
    /// 특정 타일의 모든 페르몬 강조 재생성 (겹침 고려) (요구사항 4)
    /// </summary>
    private void RefreshPheromoneHighlightsForTile(Vector2Int coord)
    {
        if (!tileSquads.ContainsKey(coord)) return;
        
        var squads = tileSquads[coord];
        int squadCount = squads.Count;
        
        // ? 기존 강조 모두 제거
        foreach (var squad in squads)
        {
            string key = $"{coord.x}_{coord.y}_{squad}";
            if (pheromoneRenderers.ContainsKey(key))
            {
                Destroy(pheromoneRenderers[key]);
                pheromoneRenderers.Remove(key);
            }
        }
        
        // ? 겹침 수에 따라 크기 축소 (요구사항 4)
        float scaleStep = 0.1f; // 각 겹침마다 10% 축소
        
        for (int i = 0; i < squads.Count; i++)
        {
            WorkerSquad squad = squads[i];
            float scaleOffset = scaleStep * i; // 0, 0.1, 0.2
            
            ShowPheromoneHighlight(coord.x, coord.y, squad, scaleOffset);
        }
    }
    
    /// <summary>
    /// 페르몬 타일 강조 효과 표시 (TileHighlighter 방식 사용)
    /// </summary>
    private void ShowPheromoneHighlight(int q, int r, WorkerSquad squad, float scaleOffset = 0f)
    {
        Vector2Int coord = new Vector2Int(q, r);
        string key = $"{q}_{r}_{squad}";
        
        // 이미 강조 중이면 제거 후 재생성
        if (pheromoneRenderers.ContainsKey(key))
        {
            Destroy(pheromoneRenderers[key]);
            pheromoneRenderers.Remove(key);
        }
        
        // TileBoundaryHighlighter처럼 육각형 테두리 생성
        GameObject highlightObj = new GameObject($"Pheromone_{squad}_{q}_{r}");
        highlightObj.transform.SetParent(transform);
        
        LineRenderer lr = highlightObj.AddComponent<LineRenderer>();
        
        // ? HiveManager에서 색상 가져오기 (요구사항 2)
        Color squadColor = GetPheromoneColor(squad);
        
        // ? TileHighlighter 방식: sortingOrder 제거, z-position만 사용
        lr.positionCount = 7; // 육각형 + 닫힘
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.startWidth = pheromoneLineWidth;
        lr.endWidth = pheromoneLineWidth;
        lr.startColor = squadColor;
        lr.endColor = squadColor;
        
        // Material 설정
        Material mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.material.color = squadColor;
        
        // 육각형 꼭짓점 계산 (크기 축소 + 겹침 오프셋)
        float hexSize = 0.5f;
        if (GameManager.Instance != null) hexSize = GameManager.Instance.hexSize;
        
        // ? 겹침 수에 따라 크기 축소 (요구사항 4)
        float scaledSize = hexSize * (pheromoneHighlightScale - scaleOffset);
        Vector3 center = TileHelper.HexToWorld(q, r, hexSize);
        
        // ? z 좌표를 -1.0으로 설정하여 타일보다 훨씬 앞에 표시 (TileHighlighter처럼)
        center.z = -1.0f;
        
        Vector3[] corners = new Vector3[7];
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i + 30f;
            float angleRad = Mathf.Deg2Rad * angleDeg;
            corners[i] = center + new Vector3(
                scaledSize * Mathf.Cos(angleRad),
                scaledSize * Mathf.Sin(angleRad),
                -1.0f // ? 각 꼭짓점도 z = -1.0
            );
        }
        corners[6] = corners[0]; // 닫힘
        
        lr.SetPositions(corners);
        
        // 캐시에 추가
        pheromoneRenderers[key] = highlightObj;
        
        Debug.Log($"[페르몬] 강조 효과 생성: ({q}, {r}), 부대: {squad}, 색상: {squadColor}, 크기 오프셋: {scaleOffset}");
    }
    
    /// <summary>
    /// 페르몬 타일 강조 제거
    /// </summary>
    private void RemovePheromoneHighlight(Vector2Int coord, WorkerSquad squad)
    {
        string key = $"{coord.x}_{coord.y}_{squad}";
        
        if (pheromoneRenderers.ContainsKey(key))
        {
            Destroy(pheromoneRenderers[key]);
            pheromoneRenderers.Remove(key);
        }
    }
    
    /// <summary>
    /// 특정 부대의 페르몬 타일 목록 가져오기
    /// </summary>
    public HashSet<Vector2Int> GetPheromoneTiles(WorkerSquad squad)
    {
        return new HashSet<Vector2Int>(pheromoneTiles[squad]);
    }
    
    /// <summary>
    /// 특정 부대의 페르몬 위치 목록(추가 순서) 가져오기
    /// </summary>
    public List<Vector2Int> GetPheromonePositionsOrdered(WorkerSquad squad)
    {
        if (!pheromoneOrder.ContainsKey(squad)) return new List<Vector2Int>();
        return new List<Vector2Int>(pheromoneOrder[squad]);
    }
    
    /// <summary>
    /// 특정 부대의 가장 최근 페르몬 위치 가져오기 (없으면 null)
    /// </summary>
    public Vector2Int? GetCurrentPheromonePosition(WorkerSquad squad)
    {
        if (!pheromoneOrder.ContainsKey(squad)) return null;
        var list = pheromoneOrder[squad];
        if (list.Count == 0) return null;
        return list[list.Count - 1];
    }

    /// <summary>
    /// 특정 좌표의 페르몬 제거 (내부 사용)
    /// </summary>
    private void RemovePheromone(Vector2Int coord, WorkerSquad squad)
    {
        if (!pheromoneTiles.ContainsKey(squad)) return;

        if (!pheromoneTiles[squad].Contains(coord)) return;

        pheromoneTiles[squad].Remove(coord);
        pheromoneOrder[squad].Remove(coord);

        RemovePheromoneHighlight(coord, squad);

        if (tileSquads.ContainsKey(coord))
        {
            tileSquads[coord].Remove(squad);

            if (tileSquads[coord].Count > 0)
            {
                RefreshPheromoneHighlightsForTile(coord);
            }
            else
            {
                tileSquads.Remove(coord);
            }
        }
    }
}
