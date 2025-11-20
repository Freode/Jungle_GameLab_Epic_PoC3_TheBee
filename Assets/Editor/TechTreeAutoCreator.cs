using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 테크 트리 ScriptableObject를 자동으로 생성하는 에디터 도구
/// Tools -> 테크 트리 -> 모든 테크 데이터 생성 메뉴를 통해 실행
/// </summary>
public class TechTreeAutoCreator
{
    private const string BASE_PATH = "Assets/ScriptableObjects/TechTree";
    
    [MenuItem("Tools/테크 트리/모든 테크 데이터 생성")]
    public static void CreateAllTechData()
    {
        // 폴더 생성
        CreateFolderStructure();
        
        // 딕셔너리에 생성된 TechData 저장
        Dictionary<TechType, TechData> techDataDict = new Dictionary<TechType, TechData>();
        
        // 1. 모든 TechData 생성
        CreateWorkerTechs(techDataDict);
        CreateQueenTechs(techDataDict);
        CreateHiveTechs(techDataDict);
        
        // 2. 전제조건 연결 (prerequisites와 nextTechs)
        LinkPrerequisites(techDataDict);
        
        // 3. 저장 및 리프레시
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[테크 트리 생성기] 총 {techDataDict.Count}개의 테크 데이터가 생성되었습니다!");
        
        // 생성된 폴더 선택
        var folder = AssetDatabase.LoadAssetAtPath<Object>(BASE_PATH);
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }
    
    private static void CreateFolderStructure()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(BASE_PATH))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "TechTree");
        if (!AssetDatabase.IsValidFolder($"{BASE_PATH}/Worker"))
            AssetDatabase.CreateFolder(BASE_PATH, "Worker");
        if (!AssetDatabase.IsValidFolder($"{BASE_PATH}/Queen"))
            AssetDatabase.CreateFolder(BASE_PATH, "Queen");
        if (!AssetDatabase.IsValidFolder($"{BASE_PATH}/Hive"))
            AssetDatabase.CreateFolder(BASE_PATH, "Hive");
    }
    
    private static void CreateWorkerTechs(Dictionary<TechType, TechData> dict)
    {
        // 채취량 증가 Lv.1
        dict[TechType.Worker_GatherAmount_Lv1] = CreateTech(
            TechType.Worker_GatherAmount_Lv1, "채취량 증가 Lv.1", 
            "꿀벌이 한 번에 자원을 채취하는 양이 1 증가합니다.", 
            TechCategory.Worker, 10, new Vector2(0, 0),
            new[] { ("꿀벌(일꾼) 채취량", "+1", "+1") }
        );
        
        // 꿀벌 체력 Lv.1
        dict[TechType.Worker_Health_Lv1] = CreateTech(
            TechType.Worker_Health_Lv1, "꿀벌 체력 Lv.1", 
            "꿀벌 체력이 3 증가합니다.", 
            TechCategory.Worker, 35, new Vector2(150, 0),
            new[] { ("꿀벌 체력", "+3", "+3") }
        );
        
        // 꿀벌 체력 Lv.2
        dict[TechType.Worker_Health_Lv2] = CreateTech(
            TechType.Worker_Health_Lv2, "꿀벌 체력 Lv.2", 
            "꿀벌 체력이 5 증가합니다.", 
            TechCategory.Worker, 150, new Vector2(300, 0),
            new[] { ("꿀벌 체력", "+5", "+5") }
        );
        
        // 꿀벌 체력 Lv.3
        dict[TechType.Worker_Health_Lv3] = CreateTech(
            TechType.Worker_Health_Lv3, "꿀벌 체력 Lv.3", 
            "꿀벌 체력이 7 증가합니다.", 
            TechCategory.Worker, 500, new Vector2(450, 0),
            new[] { ("꿀벌 체력", "+7", "+7") }
        );
        
        // 꿀벌 체력 Lv.Max
        dict[TechType.Worker_Health_Max] = CreateTech(
            TechType.Worker_Health_Max, "꿀벌 체력 Lv.Max", 
            "꿀벌 체력이 10 증가합니다.", 
            TechCategory.Worker, 1250, new Vector2(600, 0),
            new[] { ("꿀벌 체력", "+10", "+10") }
        );
        
        // 꿀벌 회복 Lv.1
        dict[TechType.Worker_Regen_Lv1] = CreateTech(
            TechType.Worker_Regen_Lv1, "꿀벌 회복 Lv.1", 
            "꿀벌 체력 회복 속도가 초당 0.25만큼 증가합니다.", 
            TechCategory.Worker, 150, new Vector2(150, 150),
            new[] { ("꿀벌 체력 회복", "0.25/s", "0.25/s") }
        );
        
        // 꿀벌 회복 Lv.2
        dict[TechType.Worker_Regen_Lv2] = CreateTech(
            TechType.Worker_Regen_Lv2, "꿀벌 회복 Lv.2", 
            "꿀벌 체력 회복 속도가 초당 0.5만큼 증가합니다.", 
            TechCategory.Worker, 500, new Vector2(300, 150),
            new[] { ("꿀벌 체력 회복", "0.5/s", "0.5/s") }
        );
        
        // 꿀벌 회복 Lv.Max
        dict[TechType.Worker_Regen_Max] = CreateTech(
            TechType.Worker_Regen_Max, "꿀벌 회복 Lv.Max", 
            "꿀벌 체력 회복 속도가 초당 0.75만큼 증가합니다.", 
            TechCategory.Worker, 1250, new Vector2(450, 150),
            new[] { ("꿀벌 체력 회복", "0.75/s", "0.75/s") }
        );
        
        // 채취 시간 단축 Lv.1
        dict[TechType.Worker_GatherTime_Lv1] = CreateTech(
            TechType.Worker_GatherTime_Lv1, "채취 시간 단축 Lv.1", 
            "꿀벌 채취 시간이 -0.3초 감소합니다.", 
            TechCategory.Worker, 35, new Vector2(0, 300),
            new[] { ("꿀벌 채취 시간", "-0.3s", "-0.3s") }
        );
        
        // 채취 시간 단축 Lv.2
        dict[TechType.Worker_GatherTime_Lv2] = CreateTech(
            TechType.Worker_GatherTime_Lv2, "채취 시간 단축 Lv.2", 
            "꿀벌 채취 시간이 -0.3초 감소합니다.", 
            TechCategory.Worker, 150, new Vector2(150, 300),
            new[] { ("꿀벌 채취 시간", "-0.3s", "-0.3s") }
        );
        
        // 채취 시간 단축 Lv.3
        dict[TechType.Worker_GatherTime_Lv3] = CreateTech(
            TechType.Worker_GatherTime_Lv3, "채취 시간 단축 Lv.3", 
            "꿀벌 채취 시간이 -0.4초 감소합니다.", 
            TechCategory.Worker, 500, new Vector2(300, 300),
            new[] { ("꿀벌 채취 시간", "-0.4s", "-0.4s") }
        );
        
        // 채취량 증가 Lv.2
        dict[TechType.Worker_GatherAmount_Lv2] = CreateTech(
            TechType.Worker_GatherAmount_Lv2, "채취량 증가 Lv.2", 
            "꿀벌이 한 번에 자원을 채취하는 양이 1 증가합니다.", 
            TechCategory.Worker, 150, new Vector2(150, 450),
            new[] { ("꿀벌(일꾼) 채취량", "+1", "+1") }
        );
        
        // 채취량 증가 Lv.3
        dict[TechType.Worker_GatherAmount_Lv3] = CreateTech(
            TechType.Worker_GatherAmount_Lv3, "채취량 증가 Lv.3", 
            "꿀벌이 한 번에 자원을 채취하는 양이 2 증가합니다.", 
            TechCategory.Worker, 500, new Vector2(300, 450),
            new[] { ("꿀벌(일꾼) 채취량", "+2", "+2") }
        );
        
        // 채취 Lv.Max
        dict[TechType.Worker_Gathering_Max] = CreateTech(
            TechType.Worker_Gathering_Max, "채취 Lv.Max", 
            "채취에 대한 고도의 연구로 꿀벌의 채취 효율이 크게 상승합니다.", 
            TechCategory.Worker, 1250, new Vector2(450, 375),
            new[] { ("꿀벌(일꾼) 채취량", "+3", "+3"), ("꿀벌 채취 시간", "-0.5s", "-0.5s") }
        );
        
        // 주변 탐색 채취
        dict[TechType.Worker_AutoSearch] = CreateTech(
            TechType.Worker_AutoSearch, "주변 탐색 채취", 
            "명령 내린 지역의 자원이 모두 고갈되면, 그 근처에 자원이 있는지 자동으로 탐색합니다.", 
            TechCategory.Worker, 1250, new Vector2(300, 600),
            new[] { ("자원 타일 주변 자동 탐사", "활성화", "활성화") }
        );
        
        // 꿀벌 공격력 Lv.1
        dict[TechType.Worker_Attack_Lv1] = CreateTech(
            TechType.Worker_Attack_Lv1, "꿀벌 공격력 Lv.1", 
            "꿀벌의 공격력이 2 상승합니다.", 
            TechCategory.Worker, 35, new Vector2(0, 750),
            new[] { ("꿀벌 공격력", "+2", "+2") }
        );
        
        // 꿀벌 공격력 Lv.2
        dict[TechType.Worker_Attack_Lv2] = CreateTech(
            TechType.Worker_Attack_Lv2, "꿀벌 공격력 Lv.2", 
            "꿀벌의 공격력이 3 상승합니다.", 
            TechCategory.Worker, 150, new Vector2(150, 750),
            new[] { ("꿀벌 공격력", "+3", "+3") }
        );
        
        // 꿀벌 공격력 Lv.3
        dict[TechType.Worker_Attack_Lv3] = CreateTech(
            TechType.Worker_Attack_Lv3, "꿀벌 공격력 Lv.3", 
            "꿀벌의 공격력이 5 상승합니다.", 
            TechCategory.Worker, 500, new Vector2(300, 750),
            new[] { ("꿀벌 공격력", "+5", "+5") }
        );
        
        // 꿀벌 공격력 Lv.Max
        dict[TechType.Worker_Attack_Max] = CreateTech(
            TechType.Worker_Attack_Max, "꿀벌 공격력 Lv.Max", 
            "꿀벌의 공격력이 8 상승합니다.", 
            TechCategory.Worker, 1250, new Vector2(450, 750),
            new[] { ("꿀벌 공격력", "+8", "+8") }
        );
        
        // 꿀벌 이동 속도 Lv.1
        dict[TechType.Worker_Speed_Lv1] = CreateTech(
            TechType.Worker_Speed_Lv1, "꿀벌 이동 속도 Lv.1", 
            "꿀벌 이동 속도가 0.2만큼 증가합니다.", 
            TechCategory.Worker, 150, new Vector2(150, 900),
            new[] { ("꿀벌 이동 속도", "+0.2", "+0.2") }
        );
        
        // 꿀벌 이동 속도 Lv.2
        dict[TechType.Worker_Speed_Lv2] = CreateTech(
            TechType.Worker_Speed_Lv2, "꿀벌 이동 속도 Lv.2", 
            "꿀벌 이동 속도가 0.3만큼 증가합니다.", 
            TechCategory.Worker, 500, new Vector2(300, 900),
            new[] { ("꿀벌 이동 속도", "+0.3", "+0.3") }
        );
        
        // 꿀벌 이동 속도 Lv.Max
        dict[TechType.Worker_Speed_Max] = CreateTech(
            TechType.Worker_Speed_Max, "꿀벌 이동 속도 Lv.Max", 
            "꿀벌 이동 속도가 0.5만큼 증가합니다.", 
            TechCategory.Worker, 1250, new Vector2(450, 900),
            new[] { ("꿀벌 이동 속도", "+0.5", "+0.5") }
        );
    }
    
    private static void CreateQueenTechs(Dictionary<TechType, TechData> dict)
    {
        // 여왕벌 체력 Lv.1
        dict[TechType.Queen_Health_Lv1] = CreateTech(
            TechType.Queen_Health_Lv1, "여왕벌 체력 Lv.1", 
            "여왕벌 체력이 15 증가합니다.", 
            TechCategory.Queen, 60, new Vector2(0, 0),
            new[] { ("여왕벌 체력", "+15", "+15") }
        );
        
        // 여왕벌 회복 Lv.1
        dict[TechType.Queen_Regen_Lv1] = CreateTech(
            TechType.Queen_Regen_Lv1, "여왕벌 회복 Lv.1", 
            "여왕벌 체력 회복 속도가 초당 0.25만큼 증가합니다.", 
            TechCategory.Queen, 60, new Vector2(150, 0),
            new[] { ("여왕벌 체력 회복", "0.25/s", "0.25/s") }
        );
        
        // 여왕벌 체력 Lv.2
        dict[TechType.Queen_Health_Lv2] = CreateTech(
            TechType.Queen_Health_Lv2, "여왕벌 체력 Lv.2", 
            "여왕벌 체력이 25 증가합니다.", 
            TechCategory.Queen, 125, new Vector2(300, 0),
            new[] { ("여왕벌 체력", "+25", "+25") }
        );
        
        // 여왕벌 체력 Lv.3
        dict[TechType.Queen_Health_Lv3] = CreateTech(
            TechType.Queen_Health_Lv3, "여왕벌 체력 Lv.3", 
            "여왕벌 체력이 50 증가합니다.", 
            TechCategory.Queen, 500, new Vector2(450, 0),
            new[] { ("여왕벌 체력", "+50", "+50") }
        );
        
        // 여왕벌 체력 Lv.Max
        dict[TechType.Queen_Health_Max] = CreateTech(
            TechType.Queen_Health_Max, "여왕벌 체력 Lv.Max", 
            "여왕벌 체력이 75 증가합니다.", 
            TechCategory.Queen, 1250, new Vector2(600, 0),
            new[] { ("여왕벌 체력", "+75", "+75") }
        );
        
        // 여왕벌 회복 Lv.2
        dict[TechType.Queen_Regen_Lv2] = CreateTech(
            TechType.Queen_Regen_Lv2, "여왕벌 회복 Lv.2", 
            "여왕벌 체력 회복 속도가 초당 0.50만큼 증가합니다.", 
            TechCategory.Queen, 125, new Vector2(150, 150),
            new[] { ("여왕벌 체력 회복", "0.50/s", "0.50/s") }
        );
        
        // 여왕벌 회복 Lv.3
        dict[TechType.Queen_Regen_Lv3] = CreateTech(
            TechType.Queen_Regen_Lv3, "여왕벌 회복 Lv.3", 
            "여왕벌 체력 회복 속도가 초당 0.75만큼 증가합니다.", 
            TechCategory.Queen, 500, new Vector2(300, 150),
            new[] { ("여왕벌 체력 회복", "0.75/s", "0.75/s") }
        );
        
        // 여왕벌 회복 Lv.Max
        dict[TechType.Queen_Regen_Max] = CreateTech(
            TechType.Queen_Regen_Max, "여왕벌 회복 Lv.Max", 
            "여왕벌 체력 회복 속도가 초당 1.00만큼 증가합니다.", 
            TechCategory.Queen, 1250, new Vector2(450, 150),
            new[] { ("여왕벌 체력 회복", "1.00/s", "1.00/s") }
        );
    }
    
    private static void CreateHiveTechs(Dictionary<TechType, TechData> dict)
    {
        // 꿀벌집 체력 Lv.1
        dict[TechType.Hive_Health_Lv1] = CreateTech(
            TechType.Hive_Health_Lv1, "꿀벌집 체력 Lv.1", 
            "꿀벌집 체력이 25 증가합니다.", 
            TechCategory.Hive, 20, new Vector2(0, 0),
            new[] { ("여왕벌 체력", "+25", "+25") }
        );
        
        // 꿀벌집 회복 Lv.1
        dict[TechType.Hive_Regen_Lv1] = CreateTech(
            TechType.Hive_Regen_Lv1, "꿀벌집 회복 Lv.1", 
            "꿀벌집 체력 회복 속도가 초당 0.25만큼 증가합니다.", 
            TechCategory.Hive, 60, new Vector2(150, 0),
            new[] { ("꿀벌집 체력 회복", "0.25/s", "0.25/s") }
        );
        
        // 꿀벌집 체력 Lv.2
        dict[TechType.Hive_Health_Lv2] = CreateTech(
            TechType.Hive_Health_Lv2, "꿀벌집 체력 Lv.2", 
            "꿀벌집 체력이 50 증가합니다.", 
            TechCategory.Hive, 125, new Vector2(300, 0),
            new[] { ("여왕벌 체력", "+50", "+50") }
        );
        
        // 꿀벌집 체력 Lv.3
        dict[TechType.Hive_Health_Lv3] = CreateTech(
            TechType.Hive_Health_Lv3, "꿀벌집 체력 Lv.3", 
            "꿀벌집 체력이 75 증가합니다.", 
            TechCategory.Hive, 500, new Vector2(450, 0),
            new[] { ("여왕벌 체력", "+75", "+75") }
        );
        
        // 꿀벌집 체력 Lv.Max
        dict[TechType.Hive_Health_Max] = CreateTech(
            TechType.Hive_Health_Max, "꿀벌집 체력 Lv.Max", 
            "꿀벌집 체력이 100 증가합니다.", 
            TechCategory.Hive, 1250, new Vector2(600, 0),
            new[] { ("여왕벌 체력", "+100", "+100") }
        );
        
        // 꿀벌집 회복 Lv.2
        dict[TechType.Hive_Regen_Lv2] = CreateTech(
            TechType.Hive_Regen_Lv2, "꿀벌집 회복 Lv.2", 
            "꿀벌집 체력 회복 속도가 초당 0.50만큼 증가합니다.", 
            TechCategory.Hive, 125, new Vector2(150, 150),
            new[] { ("꿀벌집 체력 회복", "0.50/s", "0.50/s") }
        );
        
        // 꿀벌집 회복 Lv.3
        dict[TechType.Hive_Regen_Lv3] = CreateTech(
            TechType.Hive_Regen_Lv3, "꿀벌집 회복 Lv.3", 
            "꿀벌집 체력 회복 속도가 초당 0.75만큼 증가합니다.", 
            TechCategory.Hive, 500, new Vector2(300, 150),
            new[] { ("꿀벌집 체력 회복", "0.75/s", "0.75/s") }
        );
        
        // 꿀벌집 회복 Lv.Max
        dict[TechType.Hive_Regen_Max] = CreateTech(
            TechType.Hive_Regen_Max, "꿀벌집 회복 Lv.Max", 
            "꿀벌집 체력 회복 속도가 초당 1.00만큼 증가합니다.", 
            TechCategory.Hive, 1250, new Vector2(450, 150),
            new[] { ("꿀벌집 체력 회복", "1.00/s", "1.00/s") }
        );
        
        // 꿀벌 최대 수 Lv.1
        dict[TechType.Hive_MaxWorkers_Lv1] = CreateTech(
            TechType.Hive_MaxWorkers_Lv1, "꿀벌 최대 수 Lv.1", 
            "꿀벌 최대 수가 5만큼 증가합니다.", 
            TechCategory.Hive, 40, new Vector2(0, 300),
            new[] { ("꿀벌 최대 수", "+5", "+5") }
        );
        
        // 꿀벌 최대 수 Lv.2
        dict[TechType.Hive_MaxWorkers_Lv2] = CreateTech(
            TechType.Hive_MaxWorkers_Lv2, "꿀벌 최대 수 Lv.2", 
            "꿀벌 최대 수가 7만큼 증가합니다.", 
            TechCategory.Hive, 125, new Vector2(150, 300),
            new[] { ("꿀벌 최대 수", "+7", "+7") }
        );
        
        // 꿀벌 최대 수 Lv.3
        dict[TechType.Hive_MaxWorkers_Lv3] = CreateTech(
            TechType.Hive_MaxWorkers_Lv3, "꿀벌 최대 수 Lv.3", 
            "꿀벌 최대 수가 12만큼 증가합니다.", 
            TechCategory.Hive, 500, new Vector2(300, 300),
            new[] { ("꿀벌 최대 수", "+12", "+12") }
        );
        
        // 꿀벌 최대 수 Lv.Max
        dict[TechType.Hive_MaxWorkers_Max] = CreateTech(
            TechType.Hive_MaxWorkers_Max, "꿀벌 최대 수 Lv.Max", 
            "꿀벌 최대 수가 18만큼 증가합니다.", 
            TechCategory.Hive, 1250, new Vector2(450, 300),
            new[] { ("꿀벌 최대 수", "+18", "+18") }
        );
        
        // 꿀벌 생성 주기 Lv.1
        dict[TechType.Hive_SpawnInterval_Lv1] = CreateTech(
            TechType.Hive_SpawnInterval_Lv1, "꿀벌 생성 주기 Lv.1", 
            "꿀벌집에서 꿀벌 생성 주기가 0.5초만큼 감소합니다.", 
            TechCategory.Hive, 125, new Vector2(150, 450),
            new[] { ("꿀벌 생성 주기", "-0.5s", "-0.5s") }
        );
        
        // 꿀벌 생성 주기 Lv.2
        dict[TechType.Hive_SpawnInterval_Lv2] = CreateTech(
            TechType.Hive_SpawnInterval_Lv2, "꿀벌 생성 주기 Lv.2", 
            "꿀벌집에서 꿀벌 생성 주기가 0.75초만큼 감소합니다.", 
            TechCategory.Hive, 500, new Vector2(300, 450),
            new[] { ("꿀벌 생성 주기", "-0.75s", "-0.75s") }
        );
        
        // 꿀벌 생성 주기 Lv.Max
        dict[TechType.Hive_SpawnInterval_Max] = CreateTech(
            TechType.Hive_SpawnInterval_Max, "꿀벌 생성 주기 Lv.Max", 
            "꿀벌집에서 꿀벌 생성 주기가 1.00초만큼 감소합니다.", 
            TechCategory.Hive, 1250, new Vector2(450, 450),
            new[] { ("꿀벌 생성 주기", "-1.00s", "-1.00s") }
        );
        
        // 꿀벌 활동 거리 Lv.1
        dict[TechType.Hive_ActivityRange_Lv1] = CreateTech(
            TechType.Hive_ActivityRange_Lv1, "꿀벌 활동 거리 Lv.1", 
            "꿀벌의 활동 거리가 1칸 증가합니다.", 
            TechCategory.Hive, 125, new Vector2(150, 600),
            new[] { ("꿀벌 활동 거리", "+1칸", "+1칸") }
        );
        
        // 꿀벌 활동 거리 Lv.2
        dict[TechType.Hive_ActivityRange_Lv2] = CreateTech(
            TechType.Hive_ActivityRange_Lv2, "꿀벌 활동 거리 Lv.2", 
            "꿀벌의 활동 거리가 1칸 증가합니다.", 
            TechCategory.Hive, 500, new Vector2(300, 600),
            new[] { ("꿀벌 활동 거리", "+1칸", "+1칸") }
        );
        
        // 꿀벌 활동 거리 Lv.Max
        dict[TechType.Hive_ActivityRange_Max] = CreateTech(
            TechType.Hive_ActivityRange_Max, "꿀벌 활동 거리 Lv.Max", 
            "꿀벌의 활동 거리가 1칸 증가합니다.", 
            TechCategory.Hive, 1250, new Vector2(450, 600),
            new[] { ("꿀벌 활동 거리", "+1칸", "+1칸") }
        );
    }
    
    private static TechData CreateTech(
        TechType techType, 
        string name, 
        string desc, 
        TechCategory category, 
        int cost, 
        Vector2 uiPos,
        (string effectName, string current, string upgrade)[] effects)
    {
        string folderPath = $"{BASE_PATH}/{category}";
        string path = $"{folderPath}/{techType}.asset";
        
        // 이미 존재하면 덮어쓰기
        TechData existing = AssetDatabase.LoadAssetAtPath<TechData>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        // 새로운 TechData 생성
        TechData tech = ScriptableObject.CreateInstance<TechData>();
        tech.techType = techType;
        tech.techName = name;
        tech.description = desc;
        tech.category = category;
        tech.honeyResearchCost = cost;
        tech.uiPosition = uiPos;
        
        // 효과 추가
        tech.effects = new List<TechEffect>();
        foreach (var effect in effects)
        {
            tech.effects.Add(new TechEffect
            {
                effectName = effect.effectName,
                currentValue = effect.current,
                upgradeValue = effect.upgrade
            });
        }
        
        AssetDatabase.CreateAsset(tech, path);
        Debug.Log($"[테크 트리 생성기] {name} 생성 완료: {path}");
        
        return tech;
    }
    
    private static void LinkPrerequisites(Dictionary<TechType, TechData> dict)
    {
        // [일꾼 꿀벌 탭]
        // 채취량 증가 Lv.1 -> 꿀벌 체력 Lv.1, 채취 시간 단축 Lv.1, 꿀벌 공격력 Lv.1
        LinkTech(dict, TechType.Worker_GatherAmount_Lv1, 
            TechType.Worker_Health_Lv1, 
            TechType.Worker_GatherTime_Lv1, 
            TechType.Worker_Attack_Lv1);
        
        // 꿀벌 체력 체인
        LinkTechChain(dict,
            TechType.Worker_Health_Lv1,
            TechType.Worker_Health_Lv2,
            TechType.Worker_Health_Lv3,
            TechType.Worker_Health_Max
        );
        
        // 꿀벌 체력 Lv.1 -> 꿀벌 회복 Lv.1
        LinkTech(dict, TechType.Worker_Health_Lv1, TechType.Worker_Regen_Lv1);
        
        // 꿀벌 회복 체인
        LinkTechChain(dict,
            TechType.Worker_Regen_Lv1,
            TechType.Worker_Regen_Lv2,
            TechType.Worker_Regen_Max
        );
        
        // 채취 시간 단축 Lv.1 -> 채취량 증가 Lv.2, 채취 시간 단축 Lv.2
        LinkTech(dict, TechType.Worker_GatherTime_Lv1, 
            TechType.Worker_GatherAmount_Lv2, 
            TechType.Worker_GatherTime_Lv2);
        
        // 채취 시간 단축 체인
        LinkTechChain(dict,
            TechType.Worker_GatherTime_Lv2,
            TechType.Worker_GatherTime_Lv3
        );
        
        // 채취량 증가 체인
        LinkTechChain(dict,
            TechType.Worker_GatherAmount_Lv2,
            TechType.Worker_GatherAmount_Lv3
        );
        
        // 채취 Lv.Max는 채취 시간 단축 Lv.3과 채취량 증가 Lv.3 둘 다 필요
        LinkTech(dict, TechType.Worker_GatherTime_Lv3, TechType.Worker_Gathering_Max);
        LinkTech(dict, TechType.Worker_GatherAmount_Lv3, TechType.Worker_Gathering_Max);
        
        // 채취량 증가 Lv.3 -> 주변 탐색 채취
        LinkTech(dict, TechType.Worker_GatherAmount_Lv3, TechType.Worker_AutoSearch);
        
        // 꿀벌 공격력 체인
        LinkTechChain(dict,
            TechType.Worker_Attack_Lv1,
            TechType.Worker_Attack_Lv2,
            TechType.Worker_Attack_Lv3,
            TechType.Worker_Attack_Max
        );
        
        // 꿀벌 공격력 Lv.1 -> 꿀벌 이동 속도 Lv.1
        LinkTech(dict, TechType.Worker_Attack_Lv1, TechType.Worker_Speed_Lv1);
        
        // 꿀벌 이동 속도 체인
        LinkTechChain(dict,
            TechType.Worker_Speed_Lv1,
            TechType.Worker_Speed_Lv2,
            TechType.Worker_Speed_Max
        );
        
        // [여왕벌 탭]
        // 여왕벌 체력 Lv.1 -> 여왕벌 회복 Lv.1
        LinkTech(dict, TechType.Queen_Health_Lv1, TechType.Queen_Regen_Lv1);
        
        // 여왕벌 회복 Lv.1 -> 여왕벌 회복 Lv.2, 여왕벌 체력 Lv.2
        LinkTech(dict, TechType.Queen_Regen_Lv1, 
            TechType.Queen_Regen_Lv2, 
            TechType.Queen_Health_Lv2);
        
        // 여왕벌 체력 체인
        LinkTechChain(dict,
            TechType.Queen_Health_Lv2,
            TechType.Queen_Health_Lv3,
            TechType.Queen_Health_Max
        );
        
        // 여왕벌 회복 체인
        LinkTechChain(dict,
            TechType.Queen_Regen_Lv2,
            TechType.Queen_Regen_Lv3,
            TechType.Queen_Regen_Max
        );
        
        // [꿀벌집 탭]
        // 꿀벌집 체력 Lv.1 -> 꿀벌 최대 수 Lv.1, 꿀벌집 회복 Lv.1
        LinkTech(dict, TechType.Hive_Health_Lv1, 
            TechType.Hive_MaxWorkers_Lv1, 
            TechType.Hive_Regen_Lv1);
        
        // 꿀벌집 회복 Lv.1 -> 꿀벌집 회복 Lv.2, 꿀벌집 체력 Lv.2
        LinkTech(dict, TechType.Hive_Regen_Lv1, 
            TechType.Hive_Regen_Lv2, 
            TechType.Hive_Health_Lv2);
        
        // 꿀벌집 체력 체인
        LinkTechChain(dict,
            TechType.Hive_Health_Lv2,
            TechType.Hive_Health_Lv3,
            TechType.Hive_Health_Max
        );
        
        // 꿀벌집 회복 체인
        LinkTechChain(dict,
            TechType.Hive_Regen_Lv2,
            TechType.Hive_Regen_Lv3,
            TechType.Hive_Regen_Max
        );
        
        // 꿀벌 최대 수 체인
        LinkTechChain(dict,
            TechType.Hive_MaxWorkers_Lv1,
            TechType.Hive_MaxWorkers_Lv2,
            TechType.Hive_MaxWorkers_Lv3,
            TechType.Hive_MaxWorkers_Max
        );
        
        // 꿀벌 최대 수 Lv.1 -> 꿀벌 생성 주기 Lv.1, 꿀벌 활동 거리 Lv.1
        LinkTech(dict, TechType.Hive_MaxWorkers_Lv1, 
            TechType.Hive_SpawnInterval_Lv1, 
            TechType.Hive_ActivityRange_Lv1);
        
        // 꿀벌 생성 주기 체인
        LinkTechChain(dict,
            TechType.Hive_SpawnInterval_Lv1,
            TechType.Hive_SpawnInterval_Lv2,
            TechType.Hive_SpawnInterval_Max
        );
        
        // 꿀벌 활동 거리 체인
        LinkTechChain(dict,
            TechType.Hive_ActivityRange_Lv1,
            TechType.Hive_ActivityRange_Lv2,
            TechType.Hive_ActivityRange_Max
        );
        
        // 모든 TechData를 다시 저장
        foreach (var tech in dict.Values)
        {
            EditorUtility.SetDirty(tech);
        }
    }
    
    private static void LinkTechChain(Dictionary<TechType, TechData> dict, params TechType[] chain)
    {
        for (int i = 0; i < chain.Length - 1; i++)
        {
            if (!dict.ContainsKey(chain[i])) continue;
            if (!dict.ContainsKey(chain[i + 1])) continue;
            
            TechData current = dict[chain[i]];
            TechData next = dict[chain[i + 1]];
            
            // 다음 테크를 nextTechs에 추가
            if (!current.nextTechs.Contains(next))
                current.nextTechs.Add(next);
            
            // 현재 테크를 다음 테크의 prerequisites에 추가
            if (!next.prerequisites.Contains(current))
                next.prerequisites.Add(current);
        }
    }
    
    private static void LinkTech(Dictionary<TechType, TechData> dict, TechType from, params TechType[] toList)
    {
        if (!dict.ContainsKey(from)) return;
        
        TechData fromTech = dict[from];
        
        foreach (var to in toList)
        {
            if (!dict.ContainsKey(to)) continue;
            
            TechData toTech = dict[to];
            
            // from -> to 연결
            if (!fromTech.nextTechs.Contains(toTech))
                fromTech.nextTechs.Add(toTech);
            
            // to의 prerequisites에 from 추가
            if (!toTech.prerequisites.Contains(fromTech))
                toTech.prerequisites.Add(fromTech);
        }
    }
    
    [MenuItem("Tools/테크 트리/테크 트리 폴더 열기")]
    public static void OpenTechTreeFolder()
    {
        if (!AssetDatabase.IsValidFolder(BASE_PATH))
        {
            if (EditorUtility.DisplayDialog(
                "폴더 없음",
                "테크 트리 폴더가 없습니다.\n먼저 '모든 테크 데이터 생성'을 실행하세요.",
                "생성하기",
                "취소"))
            {
                CreateAllTechData();
            }
            return;
        }
        
        var folder = AssetDatabase.LoadAssetAtPath<Object>(BASE_PATH);
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }
    
    [MenuItem("Tools/테크 트리/모든 테크 데이터 삭제")]
    public static void DeleteAllTechData()
    {
        if (!EditorUtility.DisplayDialog(
            "테크 데이터 삭제",
            "모든 테크 데이터를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다!",
            "삭제",
            "취소"))
        {
            return;
        }
        
        if (!AssetDatabase.IsValidFolder(BASE_PATH))
        {
            Debug.Log("[테크 트리 생성기] 삭제할 테크 트리 폴더가 없습니다.");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:TechData", new[] { BASE_PATH });
        int deletedCount = 0;
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.DeleteAsset(assetPath))
            {
                deletedCount++;
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"[테크 트리 생성기] {deletedCount}개의 테크 데이터를 삭제했습니다.");
    }
}
#endif
