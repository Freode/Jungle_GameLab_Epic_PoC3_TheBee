using UnityEngine;
using System.Linq;

/// <summary>
/// 핫키 입력 관리자
/// - 1번 키: 하이브 또는 여왕벌 선택
/// </summary>
public class HotkeyManager : MonoBehaviour
{
    public static HotkeyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        // 1번 키: 하이브/여왕벌 선택
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectHiveOrQueen();
        }
    }

    /// <summary>
    /// 하이브 또는 여왕벌 선택
    /// </summary>
    void SelectHiveOrQueen()
    {
        // 플레이어 하이브 찾기
        Hive playerHive = FindPlayerHive();

        if (playerHive != null)
        {
            // 하이브 있으면 하이브 선택
            SelectHive(playerHive);
            Debug.Log("[핫키] 플레이어 하이브 선택");
        }
        else
        {
            // 하이브 없으면 여왕벌 선택
            UnitAgent queen = FindPlayerQueen();
            if (queen != null)
            {
                SelectQueen(queen);
                Debug.Log("[핫키] 여왕벌 선택");
            }
            else
            {
                Debug.Log("[핫키] 하이브와 여왕벌이 없습니다.");
            }
        }
    }

    /// <summary>
    /// 플레이어 하이브 찾기
    /// </summary>
    Hive FindPlayerHive()
    {
        if (HiveManager.Instance == null) return null;

        foreach (var hive in HiveManager.Instance.GetAllHives())
        {
            if (hive == null) continue;

            var agent = hive.GetComponent<UnitAgent>();
            if (agent != null && agent.faction == Faction.Player)
            {
                return hive;
            }
        }

        return null;
    }

    /// <summary>
    /// 플레이어 여왕벌 찾기
    /// </summary>
    UnitAgent FindPlayerQueen()
    {
        if (TileManager.Instance == null) return null;

        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit != null && 
                unit.faction == Faction.Player && 
                unit.isQueen)
            {
                return unit;
            }
        }

        return null;
    }

    /// <summary>
    /// 하이브 선택
    /// </summary>
    void SelectHive(Hive hive)
    {
        if (hive == null) return;

        // 기존 선택 해제
        if (DragSelector.Instance != null)
        {
            DragSelector.Instance.DeselectAll();
        }

        // TileClickMover를 통한 선택
        if (TileClickMover.Instance != null)
        {
            var agent = hive.GetComponent<UnitAgent>();
            if (agent != null)
            {
                TileClickMover.Instance.SelectUnit(agent);
            }
        }

        // UI 패널 표시
        if (UnitCommandPanel.Instance != null)
        {
            var agent = hive.GetComponent<UnitAgent>();
            if (agent != null)
            {
                UnitCommandPanel.Instance.Show(agent);
            }
        }
    }

    /// <summary>
    /// 여왕벌 선택
    /// </summary>
    void SelectQueen(UnitAgent queen)
    {
        if (queen == null) return;

        // 기존 선택 해제
        if (DragSelector.Instance != null)
        {
            DragSelector.Instance.DeselectAll();
        }

        // TileClickMover를 통한 선택
        if (TileClickMover.Instance != null)
        {
            TileClickMover.Instance.SelectUnit(queen);
        }

        // UI 패널 표시
        if (UnitCommandPanel.Instance != null)
        {
            UnitCommandPanel.Instance.Show(queen);
        }
    }
}
