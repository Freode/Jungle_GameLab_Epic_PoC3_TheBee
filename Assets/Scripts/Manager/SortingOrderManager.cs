using UnityEngine;

/// <summary>
/// Sorting Order 관리자 - 유닛 타입별로 번호 할당
/// </summary>
public class SortingOrderManager : MonoBehaviour
{
    public static SortingOrderManager Instance { get; private set; }

    [Header("Sorting Order 범위 설정")]
    [Tooltip("말벌(적) Sorting Order 시작 번호")]
    public int enemyStartOrder = 1000;
    
    [Tooltip("말벌(적) Sorting Order 끝 번호")]
    public int enemyEndOrder = 20000;
    
    [Tooltip("일꾼 꿀벌(플레이어) Sorting Order 시작 번호")]
    public int workerStartOrder = 30000;
    
    [Tooltip("일꾼 꿀벌(플레이어) Sorting Order 끝 번호")]
    public int workerEndOrder = 50000;

    // 현재 할당 번호 (순환)
    private int currentEnemyOrder;
    private int currentWorkerOrder;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 초기화
        currentEnemyOrder = enemyStartOrder;
        currentWorkerOrder = workerStartOrder;
    }

    /// <summary>
    /// 유닛 타입에 따라 Sorting Order 할당
    /// </summary>
    public int GetNextSortingOrder(UnitAgent agent)
    {
        if (agent == null) return 0;

        // 적 유닛 (말벌)
        if (agent.faction == Faction.Enemy)
        {
            return GetNextEnemyOrder();
        }
        // 플레이어 유닛 (일꾼 꿀벌)
        else if (agent.faction == Faction.Player && !agent.isQueen)
        {
            return GetNextWorkerOrder();
        }
        // 여왕벌이나 기타 유닛은 기본값
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// 다음 적 유닛 Sorting Order (1000 ~ 20000 순환)
    /// </summary>
    private int GetNextEnemyOrder()
    {
        int order = currentEnemyOrder;
        
        // 다음 번호로 이동
        currentEnemyOrder++;
        
        // 끝에 도달하면 처음으로 순환
        if (currentEnemyOrder > enemyEndOrder)
        {
            currentEnemyOrder = enemyStartOrder;
            Debug.Log($"[SortingOrder] 적 유닛 Sorting Order 순환: {enemyStartOrder}부터 재시작");
        }
        
        return order;
    }

    /// <summary>
    /// 다음 일꾼 Sorting Order (30000 ~ 50000 순환)
    /// </summary>
    private int GetNextWorkerOrder()
    {
        int order = currentWorkerOrder;
        
        // 다음 번호로 이동
        currentWorkerOrder++;
        
        // 끝에 도달하면 처음으로 순환
        if (currentWorkerOrder > workerEndOrder)
        {
            currentWorkerOrder = workerStartOrder;
            Debug.Log($"[SortingOrder] 일꾼 꿀벌 Sorting Order 순환: {workerStartOrder}부터 재시작");
        }
        
        return order;
    }

    /// <summary>
    /// 유닛에 Sorting Order 적용
    /// </summary>
    public void ApplySortingOrder(UnitAgent agent)
    {
        if (agent == null) return;

        int sortingOrder = GetNextSortingOrder(agent);
        
        // SpriteRenderer에 적용
        var spriteRenderer = agent.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
            Debug.Log($"[SortingOrder] {agent.name} ({agent.faction}): Sorting Order = {sortingOrder}");
        }
        else
        {
            Debug.LogWarning($"[SortingOrder] {agent.name}: SpriteRenderer를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 현재 할당 상태 리셋
    /// </summary>
    public void ResetCounters()
    {
        currentEnemyOrder = enemyStartOrder;
        currentWorkerOrder = workerStartOrder;
        Debug.Log($"[SortingOrder] 카운터 리셋: 적={enemyStartOrder}, 일꾼={workerStartOrder}");
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public void LogStatus()
    {
        Debug.Log($"[SortingOrder] 현재 상태:\n" +
                  $"적 유닛: {currentEnemyOrder} / {enemyStartOrder}~{enemyEndOrder}\n" +
                  $"일꾼 꿀벌: {currentWorkerOrder} / {workerStartOrder}~{workerEndOrder}");
    }
}
