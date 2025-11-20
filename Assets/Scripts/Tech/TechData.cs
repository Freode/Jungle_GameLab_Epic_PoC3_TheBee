using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테크트리 타입 열거형
/// </summary>
public enum TechType
{
    None = 0,
    
    // ========== 일꾼 꿀벌 탭 ==========
    // 채취량 증가
    Worker_GatherAmount_Lv1,
    Worker_GatherAmount_Lv2,
    Worker_GatherAmount_Lv3,
    Worker_Gathering_Max,           // 채취 최종 업그레이드
    
    // 꿀벌 체력
    Worker_Health_Lv1,
    Worker_Health_Lv2,
    Worker_Health_Lv3,
    Worker_Health_Max,
    
    // 꿀벌 회복
    Worker_Regen_Lv1,
    Worker_Regen_Lv2,
    Worker_Regen_Max,
    
    // 채취 시간 단축
    Worker_GatherTime_Lv1,
    Worker_GatherTime_Lv2,
    Worker_GatherTime_Lv3,
    
    // 주변 탐색 채취
    Worker_AutoSearch,
    
    // 꿀벌 공격력
    Worker_Attack_Lv1,
    Worker_Attack_Lv2,
    Worker_Attack_Lv3,
    Worker_Attack_Max,
    
    // 꿀벌 이동 속도
    Worker_Speed_Lv1,
    Worker_Speed_Lv2,
    Worker_Speed_Max,
    
    // ========== 여왕벌 탭 ==========
    // 여왕벌 체력
    Queen_Health_Lv1,
    Queen_Health_Lv2,
    Queen_Health_Lv3,
    Queen_Health_Max,
    
    // 여왕벌 회복
    Queen_Regen_Lv1,
    Queen_Regen_Lv2,
    Queen_Regen_Lv3,
    Queen_Regen_Max,
    
    // ========== 꿀벌집 탭 ==========
    // 꿀벌집 체력
    Hive_Health_Lv1,
    Hive_Health_Lv2,
    Hive_Health_Lv3,
    Hive_Health_Max,
    
    // 꿀벌집 회복
    Hive_Regen_Lv1,
    Hive_Regen_Lv2,
    Hive_Regen_Lv3,
    Hive_Regen_Max,
    
    // 꿀벌 최대 수
    Hive_MaxWorkers_Lv1,
    Hive_MaxWorkers_Lv2,
    Hive_MaxWorkers_Lv3,
    Hive_MaxWorkers_Max,
    
    // 꿀벌 생성 주기
    Hive_SpawnInterval_Lv1,
    Hive_SpawnInterval_Lv2,
    Hive_SpawnInterval_Max,
    
    // 꿀벌 활동 거리
    Hive_ActivityRange_Lv1,
    Hive_ActivityRange_Lv2,
    Hive_ActivityRange_Max,
}

/// <summary>
/// 테크트리 카테고리
/// </summary>
public enum TechCategory
{
    Worker,     // 일꾼 꿀벌
    Queen,      // 여왕벌
    Hive        // 꿀벌집
}

/// <summary>
/// 테크트리 데이터 ScriptableObject
/// </summary>
[CreateAssetMenu(menuName = "Tech/TechData", fileName = "TechData")]
public class TechData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("테크트리 ID")]
    public TechType techType = TechType.None;
    
    [Tooltip("테크트리 이름")]
    public string techName = "기술 이름";
    
    [Tooltip("테크트리 설명")]
    [TextArea(3, 5)]
    public string description = "기술 설명";
    
    [Tooltip("테크트리 카테고리")]
    public TechCategory category = TechCategory.Worker;
    
    [Header("비용")]
    [Tooltip("연구에 필요한 꿀 양")]
    public int honeyResearchCost = 100;
    
    [Header("선후 관계")]
    [Tooltip("선행 테크트리 목록 (모두 연구되어야 함)")]
    public List<TechData> prerequisites = new List<TechData>();
    
    [Tooltip("이후 테크트리 목록 (연구 시 해제됨)")]
    public List<TechData> nextTechs = new List<TechData>();
    
    [Header("UI 위치")]
    [Tooltip("ScrollView Content에서의 위치 (X, Y)")]
    public Vector2 uiPosition = Vector2.zero;
    
    [Header("효과 설명")]
    [Tooltip("업그레이드 효과 목록 (예: \"일꾼 공격력\", \"3\", \"4\")")]
    public List<TechEffect> effects = new List<TechEffect>();
}

/// <summary>
/// 테크트리 효과 정보
/// </summary>
[System.Serializable]
public class TechEffect
{
    [Tooltip("효과 이름 (예: \"일꾼 공격력\")")]
    public string effectName = "효과 이름";
    
    [Tooltip("현재 값")]
    public string currentValue = "0";
    
    [Tooltip("업그레이드 후 값")]
    public string upgradeValue = "0";
}
