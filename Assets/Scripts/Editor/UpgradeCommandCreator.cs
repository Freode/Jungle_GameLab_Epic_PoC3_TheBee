using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 에디터 메뉴에서 7가지 업그레이드 명령을 자동으로 생성하는 도구
/// </summary>
public class UpgradeCommandCreator
{
    [MenuItem("Tools/업그레이드/7가지 업그레이드 명령 생성")]
    public static void CreateAllUpgradeCommands()
    {
        // ScriptableObjects/Commands/Upgrades 폴더 생성
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Commands"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Commands");
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Commands/Upgrades"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects/Commands", "Upgrades");

        // 7가지 업그레이드 명령 생성
        CreateUpgradeCommand(
            "Upgrade_HiveRange",
            "활동 범위 확장",
            "upgrade_hive_range",
            "하이브의 활동 범위를 1타일 증가시킵니다.",
            UpgradeType.HiveRange,
            20
        );

        CreateUpgradeCommand(
            "Upgrade_WorkerAttack",
            "날카로운 침",
            "upgrade_worker_attack",
            "일꾼 꿀벌의 공격력을 1 증가시킵니다.",
            UpgradeType.WorkerAttack,
            15
        );

        CreateUpgradeCommand(
            "Upgrade_WorkerHealth",
            "강화 외골격",
            "upgrade_worker_health",
            "일꾼 꿀벌의 체력을 5 증가시킵니다.",
            UpgradeType.WorkerHealth,
            20
        );

        CreateUpgradeCommand(
            "Upgrade_WorkerSpeed",
            "빠른 날개",
            "upgrade_worker_speed",
            "일꾼 꿀벌의 이동 속도를 1 증가시킵니다.",
            UpgradeType.WorkerSpeed,
            15
        );

        CreateUpgradeCommand(
            "Upgrade_HiveHealth",
            "강화 벌집",
            "upgrade_hive_health",
            "하이브의 체력을 30 증가시킵니다.",
            UpgradeType.HiveHealth,
            25
        );

        CreateUpgradeCommand(
            "Upgrade_MaxWorkers",
            "확장 군집",
            "upgrade_max_workers",
            "최대 일꾼 수를 5 증가시킵니다.",
            UpgradeType.MaxWorkers,
            30
        );

        CreateUpgradeCommand(
            "Upgrade_GatherAmount",
            "효율적 채집",
            "upgrade_gather_amount",
            "자원 채취량을 2 증가시킵니다.",
            UpgradeType.GatherAmount,
            20
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[업그레이드 생성기] 7가지 업그레이드 명령이 Assets/ScriptableObjects/Commands/Upgrades/에 생성되었습니다!");
        
        // 생성된 폴더를 Project 창에서 선택
        var folder = AssetDatabase.LoadAssetAtPath<Object>("Assets/ScriptableObjects/Commands/Upgrades");
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }

    private static void CreateUpgradeCommand(
        string fileName,
        string displayName,
        string id,
        string description,
        UpgradeType upgradeType,
        int cost)
    {
        string path = $"Assets/ScriptableObjects/Commands/Upgrades/{fileName}.asset";

        // 이미 존재하면 덮어쓰기 확인
        if (AssetDatabase.LoadAssetAtPath<SOUpgradeCommand>(path) != null)
        {
            if (!EditorUtility.DisplayDialog(
                "업그레이드 명령 생성",
                $"{fileName}이(가) 이미 존재합니다.\n덮어쓰시겠습니까?",
                "덮어쓰기",
                "건너뛰기"))
            {
                Debug.Log($"[업그레이드 생성기] {fileName} 건너뛰기");
                return;
            }

            AssetDatabase.DeleteAsset(path);
        }

        // SOUpgradeCommand 인스턴스 생성
        SOUpgradeCommand upgrade = ScriptableObject.CreateInstance<SOUpgradeCommand>();
        
        // 기본 설정
        upgrade.displayName = displayName;
        upgrade.id = id;
        upgrade.requiresTarget = false;
        upgrade.hidePanelOnClick = false;
        upgrade.resourceCost = cost;
        upgrade.upgradeType = upgradeType;

        // 에셋으로 저장
        AssetDatabase.CreateAsset(upgrade, path);
        
        Debug.Log($"[업그레이드 생성기] {displayName} 생성 완료: {path}");
    }

    [MenuItem("Tools/업그레이드/업그레이드 폴더 열기")]
    public static void OpenUpgradesFolder()
    {
        // 폴더가 없으면 생성
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Commands/Upgrades"))
        {
            if (EditorUtility.DisplayDialog(
                "폴더 없음",
                "업그레이드 폴더가 없습니다.\n먼저 '7가지 업그레이드 명령 생성'을 실행하세요.",
                "생성하기",
                "취소"))
            {
                CreateAllUpgradeCommands();
            }
            return;
        }

        // 폴더 선택
        var folder = AssetDatabase.LoadAssetAtPath<Object>("Assets/ScriptableObjects/Commands/Upgrades");
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }

    [MenuItem("Tools/업그레이드/업그레이드 명령 삭제")]
    public static void DeleteAllUpgradeCommands()
    {
        if (!EditorUtility.DisplayDialog(
            "업그레이드 명령 삭제",
            "모든 업그레이드 명령을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다!",
            "삭제",
            "취소"))
        {
            return;
        }

        string folderPath = "Assets/ScriptableObjects/Commands/Upgrades";
        
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.Log("[업그레이드 생성기] 삭제할 업그레이드 폴더가 없습니다.");
            return;
        }

        // 폴더 내의 모든 에셋 삭제
        string[] guids = AssetDatabase.FindAssets("t:SOUpgradeCommand", new[] { folderPath });
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
        Debug.Log($"[업그레이드 생성기] {deletedCount}개의 업그레이드 명령을 삭제했습니다.");
    }
}
#endif
