using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테크트리 관리자
/// - 테크트리 연구 상태 관리
/// - 연구 가능 여부 확인
/// - 업데이트 목록 관리
/// </summary>
public class TechManager : MonoBehaviour
{
    public static TechManager Instance { get; private set; }
    
    [Header("테크트리 데이터")]
    [Tooltip("모든 테크트리 데이터 목록")]
    public List<TechData> allTechs = new List<TechData>();
    
    // 연구 완료된 테크트리 목록
    private HashSet<TechType> researchedTechs = new HashSet<TechType>();
    
    // 업데이트 가능한 테크트리 버튼 목록
    private HashSet<TechButton> updateableButtons = new HashSet<TechButton>();
    
    // 테크트리 연구 완료 이벤트
    public event System.Action<TechType> OnTechResearched;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // HiveManager의 자원 변경 이벤트 구독
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged += OnResourcesChanged;
        }
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (HiveManager.Instance != null)
        {
            HiveManager.Instance.OnResourcesChanged -= OnResourcesChanged;
        }
    }
    
    /// <summary>
    /// 자원량이 변경되었을 때 호출
    /// </summary>
    void OnResourcesChanged()
    {
        UpdateAllButtons();
    }
    
    /// <summary>
    /// 모든 업데이트 가능한 버튼 갱신
    /// </summary>
    public void UpdateAllButtons()
    {
        foreach (var button in updateableButtons)
        {
            if (button != null)
            {
                button.UpdateButtonState();
            }
        }
    }
    
    /// <summary>
    /// 업데이트 가능 목록에 버튼 추가
    /// </summary>
    public void AddUpdateableButton(TechButton button)
    {
        if (button != null)
        {
            updateableButtons.Add(button);
        }
    }
    
    /// <summary>
    /// 업데이트 가능 목록에서 버튼 제거
    /// </summary>
    public void RemoveUpdateableButton(TechButton button)
    {
        if (button != null)
        {
            updateableButtons.Remove(button);
        }
    }
    
    /// <summary>
    /// 테크트리 연구 시도
    /// </summary>
    public bool TryResearchTech(TechData tech)
    {
        if (tech == null)
        {
            Debug.LogWarning("[TechManager] 테크트리 데이터가 null입니다.");
            return false;
        }
        
        // 이미 연구된 테크트리인지 확인
        if (IsResearched(tech.techType))
        {
            Debug.LogWarning($"[TechManager] {tech.techName}은(는) 이미 연구되었습니다.");
            return false;
        }
        
        // 선행 테크트리 확인
        if (!CheckPrerequisites(tech))
        {
            Debug.LogWarning($"[TechManager] {tech.techName}의 선행 테크트리가 완료되지 않았습니다.");
            return false;
        }
        
        // 자원 확인 및 소모
        if (!HiveManager.Instance.TrySpendResources(tech.honeyResearchCost))
        {
            Debug.LogWarning($"[TechManager] 꿀이 부족합니다. (필요: {tech.honeyResearchCost}, 현재: {HiveManager.Instance.playerStoredResources})");
            return false;
        }
        
        // 연구 완료 처리
        researchedTechs.Add(tech.techType);
        Debug.Log($"[TechManager] {tech.techName} 연구 완료!");
        
        // 테크 효과 적용
        ApplyTechEffect(tech.techType);
        
        // 이벤트 발생
        OnTechResearched?.Invoke(tech.techType);
        
        // 이후 테크트리 버튼 업데이트 가능 목록에 추가는 TechButton에서 처리
        
        return true;
    }
    
    /// <summary>
    /// 테크트리가 연구되었는지 확인
    /// </summary>
    public bool IsResearched(TechType techType)
    {
        return researchedTechs.Contains(techType);
    }
    
    /// <summary>
    /// 선행 테크트리가 모두 완료되었는지 확인
    /// </summary>
    public bool CheckPrerequisites(TechData tech)
    {
        if (tech == null) return false;
        
        foreach (var prerequisite in tech.prerequisites)
        {
            if (prerequisite == null) continue;
            
            if (!IsResearched(prerequisite.techType))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 테크 효과 적용
    /// </summary>
    void ApplyTechEffect(TechType techType)
    {
        if (HiveManager.Instance == null) return;
        
        switch (techType)
        {
            // ===== 일꾼 꿀벌 탭 =====
            
            // 채취량 증가
            case TechType.Worker_GatherAmount_Lv1:
            case TechType.Worker_GatherAmount_Lv2:
                HiveManager.Instance.gatherAmountLevel++;
                HiveManager.Instance.UpdateAllWorkerGatherAmount();
                Debug.Log($"[TechManager] 채취량 업그레이드: {HiveManager.Instance.GetGatherAmount()}");
                break;
            case TechType.Worker_GatherAmount_Lv3:
                HiveManager.Instance.gatherAmountLevel+=2;
                HiveManager.Instance.UpdateAllWorkerGatherAmount();
                Debug.Log($"[TechManager] 채취량 업그레이드: {HiveManager.Instance.GetGatherAmount()}");
                break;
            case TechType.Worker_Gathering_Max:
                HiveManager.Instance.gatherAmountLevel+=3;
                WorkerBehaviorController.gatheringDuration -= 0.5f;
                HiveManager.Instance.UpdateAllWorkerGatherAmount();
                Debug.Log($"[TechManager] 채취량 업그레이드: {HiveManager.Instance.GetGatherAmount()}");
                break;
            
            // 채취 시간 단축
            case TechType.Worker_GatherTime_Lv1:
                WorkerBehaviorController.gatheringDuration -= 0.3f;
                break;
            case TechType.Worker_GatherTime_Lv2:
                WorkerBehaviorController.gatheringDuration -= 0.3f;
                break;
            case TechType.Worker_GatherTime_Lv3:
                // TODO: 채취 시간 로직 구현
                WorkerBehaviorController.gatheringDuration -= 0.4f;
                Debug.Log($"[TechManager] 채취 시간 단축 업그레이드");
                break;
            
            // 자동 자원 탐색
            case TechType.Worker_AutoSearch:
                // TODO: 자동 탐색 로직 구현
                WorkerBehaviorController.isAutoSearchNearResource = true;
                Debug.Log($"[TechManager] 자동 자원 탐색 활성화");
                break;
            
            // 체력
            case TechType.Worker_Health_Lv1:
                HiveManager.Instance.workerHealthLevel+=3;
                HiveManager.Instance.UpdateAllWorkerCombat();
                break;
            case TechType.Worker_Health_Lv2:
                HiveManager.Instance.workerHealthLevel += 5;
                HiveManager.Instance.UpdateAllWorkerCombat();
                break;
            case TechType.Worker_Health_Lv3:
                HiveManager.Instance.workerHealthLevel += 7;
                HiveManager.Instance.UpdateAllWorkerCombat();
                break;
            case TechType.Worker_Health_Max:
                HiveManager.Instance.workerHealthLevel += 10;
                HiveManager.Instance.UpdateAllWorkerCombat();
                Debug.Log($"[TechManager] 일꾼 체력 업그레이드: {HiveManager.Instance.GetWorkerMaxHealth()}");
                break;
            
            // 회복
            case TechType.Worker_Regen_Lv1:
            case TechType.Worker_Regen_Lv2:
            case TechType.Worker_Regen_Max:
                // TODO: 회복 로직 구현
                Debug.Log($"[TechManager] 일꾼 회복 업그레이드");
                break;
            
            // 공격력
            case TechType.Worker_Attack_Lv1:
                HiveManager.Instance.workerAttackLevel += 3;
                HiveManager.Instance.UpdateAllWorkerCombat();
                break;
            case TechType.Worker_Attack_Lv2:
                HiveManager.Instance.workerAttackLevel += 3;
                HiveManager.Instance.UpdateAllWorkerCombat();
                break;
            case TechType.Worker_Attack_Lv3:
                HiveManager.Instance.workerAttackLevel += 5;
                HiveManager.Instance.UpdateAllWorkerCombat();
                break;
            case TechType.Worker_Attack_Max:
                HiveManager.Instance.workerAttackLevel += 8;
                HiveManager.Instance.UpdateAllWorkerCombat();
                Debug.Log($"[TechManager] 일꾼 공격력 업그레이드: {HiveManager.Instance.GetWorkerAttack()}");
                break;
            
            // 이동 속도
            case TechType.Worker_Speed_Lv1:
            case TechType.Worker_Speed_Lv2:
            case TechType.Worker_Speed_Max:
                HiveManager.Instance.workerSpeedLevel++;
                HiveManager.Instance.UpdateAllWorkerSpeed();
                Debug.Log($"[TechManager] 일꾼 이동속도 업그레이드: {HiveManager.Instance.GetWorkerSpeed()}");
                break;
            
            // ===== 여왕벌 탭 =====
            
            // 여왕벌 체력
            case TechType.Queen_Health_Lv1:
            case TechType.Queen_Health_Lv2:
            case TechType.Queen_Health_Lv3:
            case TechType.Queen_Health_Max:
                // TODO: 여왕벌 체력 로직 구현
                Debug.Log($"[TechManager] 여왕벌 체력 업그레이드");
                break;
            
            // 여왕벌 회복
            case TechType.Queen_Regen_Lv1:
            case TechType.Queen_Regen_Lv2:
            case TechType.Queen_Regen_Lv3:
            case TechType.Queen_Regen_Max:
                // TODO: 여왕벌 회복 로직 구현
                Debug.Log($"[TechManager] 여왕벌 회복 업그레이드");
                break;
            
            // ===== 꿀벌집 탭 =====
            
            // 꿀벌집 체력
            case TechType.Hive_Health_Lv1:
            case TechType.Hive_Health_Lv2:
            case TechType.Hive_Health_Lv3:
            case TechType.Hive_Health_Max:
                HiveManager.Instance.hiveHealthLevel++;
                HiveManager.Instance.UpdateAllHiveHealth();
                Debug.Log($"[TechManager] 하이브 체력 업그레이드: {HiveManager.Instance.GetHiveMaxHealth()}");
                break;
            
            // 꿀벌집 회복
            case TechType.Hive_Regen_Lv1:
            case TechType.Hive_Regen_Lv2:
            case TechType.Hive_Regen_Lv3:
            case TechType.Hive_Regen_Max:
                // TODO: 하이브 회복 로직 구현
                Debug.Log($"[TechManager] 하이브 회복 업그레이드");
                break;
            
            // 꿀벌 최대 수
            case TechType.Hive_MaxWorkers_Lv1:
            case TechType.Hive_MaxWorkers_Lv2:
            case TechType.Hive_MaxWorkers_Lv3:
            case TechType.Hive_MaxWorkers_Max:
                HiveManager.Instance.maxWorkersLevel++;
                HiveManager.Instance.UpdateAllHiveMaxWorkers();
                Debug.Log($"[TechManager] 최대 일꾼 수 업그레이드: {HiveManager.Instance.GetMaxWorkers()}");
                break;
            
            // 꿀벌 생성 주기
            case TechType.Hive_SpawnInterval_Lv1:
            case TechType.Hive_SpawnInterval_Lv2:
            case TechType.Hive_SpawnInterval_Max:
                // TODO: 생성 주기 로직 구현
                Debug.Log($"[TechManager] 꿀벌 생성 주기 단축");
                break;
            
            // 꿀벌 활동 거리
            case TechType.Hive_ActivityRange_Lv1:
            case TechType.Hive_ActivityRange_Lv2:
            case TechType.Hive_ActivityRange_Max:
                HiveManager.Instance.hiveRangeLevel++;
                HiveManager.Instance.hiveActivityRadius = 2 + HiveManager.Instance.hiveRangeLevel;
                // 모든 하이브의 경계선 업데이트
                foreach (var hive in HiveManager.Instance.GetAllHives())
                {
                    if (hive != null && HexBoundaryHighlighter.Instance != null)
                    {
                        HexBoundaryHighlighter.Instance.ShowBoundary(hive, HiveManager.Instance.hiveActivityRadius);
                    }
                }
                Debug.Log($"[TechManager] 하이브 활동 범위 업그레이드: {HiveManager.Instance.hiveActivityRadius}");
                break;
        }
    }
    
    /// <summary>
    /// 테크트리 데이터 가져오기
    /// </summary>
    public TechData GetTechData(TechType techType)
    {
        return allTechs.Find(t => t.techType == techType);
    }
}
