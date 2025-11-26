using UnityEngine;

// Move camera when mouse near screen edges
public class EdgePanCamera : MonoBehaviour
{
    public Camera cam;
    public float panSpeed = 10f;
    public float borderThickness = 10f; // pixels
    public Vector2 panLimitsMin = new Vector2(-50, -50);
    public Vector2 panLimitsMax = new Vector2(50, 50);
    
    [Header("여왕벌 추적 모드")]
    [Tooltip("여왕벌을 항상 따라다니는 모드")]
    public bool followQueenMode = false;
    
    [Tooltip("여왕벌 추적 부드러움 (0 = 즉시, 1 = 매우 부드러움)")]
    [Range(0f, 1f)]
    public float followSmoothness = 0.1f;
    
    private UnitAgent queenBee;
    private bool needsFindQueen = true;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        // ? 여왕벌 추적 모드
        if (followQueenMode)
        {
            UpdateQueenFollow();
            return; // ? 경계 이동 비활성화
        }
        
        // ? 기존 모드: 마우스 경계 이동
        Vector3 delta = Vector3.zero;
        Vector2 mouse = Input.mousePosition;
        if (mouse.x <= borderThickness) delta.x = -1f;
        else if (mouse.x >= Screen.width - borderThickness) delta.x = 1f;

        if (mouse.y <= borderThickness) delta.y = -1f;
        else if (mouse.y >= Screen.height - borderThickness) delta.y = 1f;

        if (delta.sqrMagnitude > 0f)
        {
            Vector3 move = new Vector3(delta.x, delta.y, 0f) * panSpeed * Time.deltaTime;
            Vector3 newPos = cam.transform.position + move;
            newPos.x = Mathf.Clamp(newPos.x, panLimitsMin.x, panLimitsMax.x);
            newPos.y = Mathf.Clamp(newPos.y, panLimitsMin.y, panLimitsMax.y);
            cam.transform.position = newPos;
        }
    }
    
    /// <summary>
    /// 여왕벌 추적 업데이트
    /// </summary>
    void UpdateQueenFollow()
    {
        // ? 여왕벌 찾기 (필요 시)
        if (needsFindQueen || queenBee == null)
        {
            FindQueen();
            needsFindQueen = false;
        }
        
        // ? 여왕벌이 있으면 따라다니기
        if (queenBee != null)
        {
            Vector3 queenPos = queenBee.transform.position;
            Vector3 targetPos = new Vector3(queenPos.x, queenPos.y, cam.transform.position.z);
            
            // ? 부드러운 이동
            cam.transform.position = Vector3.Lerp(
                cam.transform.position, 
                targetPos, 
                1f - followSmoothness
            );
        }
    }
    
    /// <summary>
    /// 플레이어 여왕벌 찾기
    /// </summary>
    void FindQueen()
    {
        if (TileManager.Instance == null)
        {
            queenBee = null;
            return;
        }
        
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit != null && unit.isQueen && unit.faction == Faction.Player)
            {
                queenBee = unit;
                Debug.Log($"[카메라] 여왕벌 발견: {unit.name}");
                return;
            }
        }
        
        queenBee = null;
        Debug.LogWarning("[카메라] 여왕벌을 찾을 수 없습니다!");
    }
    
    /// <summary>
    /// 여왕벌 추적 모드 토글
    /// </summary>
    public void ToggleFollowQueenMode()
    {
        followQueenMode = !followQueenMode;
        
        if (followQueenMode)
        {
            needsFindQueen = true;
            Debug.Log("[카메라] 여왕벌 추적 모드 활성화");
        }
        else
        {
            Debug.Log("[카메라] 여왕벌 추적 모드 비활성화");
        }
    }
    
    /// <summary>
    /// 여왕벌 추적 모드 설정
    /// </summary>
    public void SetFollowQueenMode(bool enabled)
    {
        followQueenMode = enabled;
        
        if (followQueenMode)
        {
            needsFindQueen = true;
            Debug.Log("[카메라] 여왕벌 추적 모드 활성화");
        }
        else
        {
            Debug.Log("[카메라] 여왕벌 추적 모드 비활성화");
        }
    }
    
    /// <summary>
    /// 여왕벌 참조 재설정 (게임 재시작 등)
    /// </summary>
    public void ResetQueenReference()
    {
        queenBee = null;
        needsFindQueen = true;
    }
}
