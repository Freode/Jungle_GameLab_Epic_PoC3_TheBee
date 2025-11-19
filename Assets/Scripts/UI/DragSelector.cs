using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마우스 드래그로 여러 유닛을 선택하는 기능
/// </summary>
public class DragSelector : MonoBehaviour
{
    public static DragSelector Instance { get; private set; }

    [Header("드래그 설정")]
    [Tooltip("드래그 선택 활성화")]
    public bool enableDragSelection = true;

    [Tooltip("최소 드래그 거리 (픽셀)")]
    public float minDragDistance = 10f;

    [Header("선택 박스 비주얼")]
    public Color selectionBoxColor = new Color(0, 1, 0, 0.2f); // 반투명 초록색
    public Color selectionBoxBorderColor = Color.green;
    public float borderWidth = 2f;

    private Vector2 dragStartPos;
    private Vector2 currentMousePos;
    private bool isDragging = false;
    private bool wasDragging = false; // 드래그 완료 여부 추적
    private List<UnitAgent> selectedUnits = new List<UnitAgent>();
    
    // UI 렌더링용
    private Texture2D boxTexture;
    private Texture2D borderTexture;

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
        // 선택 박스용 텍스처 생성
        CreateTextures();
    }

    void Update()
    {
        if (!enableDragSelection) return;

        HandleDragSelection();
    }

    void HandleDragSelection()
    {
        // 왼쪽 마우스 버튼 누름
        if (Input.GetMouseButtonDown(0))
        {
            // UI 위에서 클릭하면 무시 ?
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // UI 클릭은 무시하고 드래그 시작 안 함 ?
            }

            dragStartPos = Input.mousePosition;
            isDragging = false;
            wasDragging = false;
        }

        // 드래그 중
        if (Input.GetMouseButton(0))
        {
            // UI 위에서 드래그 중이면 무시 ?
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            currentMousePos = Input.mousePosition;
            
            // 최소 거리 이상 드래그했는지 확인
            float dragDistance = Vector2.Distance(dragStartPos, currentMousePos);
            if (!isDragging && dragDistance > minDragDistance)
            {
                isDragging = true;
                wasDragging = true;
                
                // 드래그 시작 시 모든 선택 해제 (하이브 포함)
                DeselectAll();
                
                // TileClickMover의 선택도 해제
                if (TileClickMover.Instance != null)
                {
                    TileClickMover.Instance.DeselectUnit();
                }
            }
        }

        // 마우스 버튼 뗌
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                // 드래그 영역 내의 유닛 선택
                SelectUnitsInDragArea();
                isDragging = false;
            }
            else if (!wasDragging)
            {
                // 드래그 없이 단순 클릭인 경우
                HandleSingleClick();
            }
            
            wasDragging = false;
        }
    }

    void HandleSingleClick()
    {
        // UI 위에서 클릭하면 무시 ?
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return; // UI 클릭은 무시 ?
        }

        // Raycast로 유닛을 클릭했는지 확인
        bool clickedOnUnit = false;
        UnitAgent clickedUnit = null;
        
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            var unit = hit.collider.GetComponentInParent<UnitAgent>();
            if (unit != null)
            {
                clickedUnit = unit;
                // 플레이어 소속 일꾼만 드래그 선택 가능
                if (unit.faction == Faction.Player && !unit.isQueen)
                {
                    clickedOnUnit = true;
                }
            }
        }
        else
        {
            // 2D Raycast도 시도
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit2 = Physics2D.Raycast(wp, Vector2.zero);
            if (hit2.collider != null)
            {
                var unit = hit2.collider.GetComponentInParent<UnitAgent>();
                if (unit != null)
                {
                    clickedUnit = unit;
                    if (unit.faction == Faction.Player && !unit.isQueen)
                    {
                        clickedOnUnit = true;
                    }
                }
            }
        }

        // 다른 것(일꾼 아닌 것)을 클릭하거나 빈 곳 클릭 시 드래그 선택 해제
        if (selectedUnits.Count > 0)
        {
            // 일꾼을 클릭한 경우가 아니면 모두 해제
            if (!clickedOnUnit)
            {
                DeselectAll();
                Debug.Log("[드래그 선택] 모든 유닛 해제 (다른 것 또는 빈 곳 클릭)");
            }
        }
    }

    void SelectUnitsInDragArea()
    {
        if (TileManager.Instance == null) return;

        // 드래그 시작/끝 좌표로 선택 영역 생성 (Y축 변환 없이)
        Vector2 startPos = dragStartPos;
        Vector2 endPos = currentMousePos;
        
        // 최소/최대 좌표 계산
        float minX = Mathf.Min(startPos.x, endPos.x);
        float maxX = Mathf.Max(startPos.x, endPos.x);
        float minY = Mathf.Min(startPos.y, endPos.y);
        float maxY = Mathf.Max(startPos.y, endPos.y);
        
        Rect selectionRect = new Rect(minX, minY, maxX - minX, maxY - minY);
        List<UnitAgent> unitsInRect = new List<UnitAgent>();

        // 모든 유닛을 확인
        foreach (var unit in TileManager.Instance.GetAllUnits())
        {
            if (unit == null) continue;

            // 플레이어 소속 일꾼만 선택 가능
            if (unit.faction != Faction.Player) continue;
            if (unit.isQueen) continue; // 여왕벌은 제외
            
            // 하이브(벌집)도 제외
            var hive = unit.GetComponent<Hive>();
            if (hive != null) continue;

            // 유닛의 화면 좌표 계산
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            
            // 화면 밖이거나 카메라 뒤에 있으면 제외
            if (screenPos.z < 0) continue;

            // 선택 영역 안에 있는지 확인 (Y축 그대로 사용)
            Vector2 screenPos2D = new Vector2(screenPos.x, screenPos.y);
            if (selectionRect.Contains(screenPos2D))
            {
                unitsInRect.Add(unit);
            }
        }

        // 선택된 유닛들 업데이트
        if (unitsInRect.Count > 0)
        {
            selectedUnits.Clear();
            selectedUnits.AddRange(unitsInRect);

            // 각 유닛에 선택 표시
            foreach (var unit in selectedUnits)
            {
                unit.SetSelected(true);
            }

            Debug.Log($"[드래그 선택] {selectedUnits.Count}개의 일꾼 선택됨");
        }
    }

    /// <summary>
    /// 모든 선택 해제
    /// </summary>
    public void DeselectAll()
    {
        foreach (var unit in selectedUnits)
        {
            if (unit != null)
                unit.SetSelected(false);
        }
        selectedUnits.Clear();
    }

    // 외부에서 선택 해제 호출 가능
    public void ClearSelection()
    {
        DeselectAll();
    }

    void OnGUI()
    {
        if (!isDragging) return;

        // 선택 박스 그리기
        Rect rect = GetScreenRect(dragStartPos, currentMousePos);
        DrawScreenRect(rect, selectionBoxColor, borderTexture);
        DrawScreenRectBorder(rect, borderWidth, selectionBoxBorderColor);
    }

    // 두 점으로 화면 Rect 생성 (Y축 반전 처리)
    Rect GetScreenRect(Vector2 screenPos1, Vector2 screenPos2)
    {
        // Y축 반전 (Unity 스크린 좌표계는 좌하단이 원점)
        screenPos1.y = Screen.height - screenPos1.y;
        screenPos2.y = Screen.height - screenPos2.y;

        // 두 점으로 사각형 만들기
        Vector2 topLeft = Vector2.Min(screenPos1, screenPos2);
        Vector2 bottomRight = Vector2.Max(screenPos1, screenPos2);

        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }

    // 채워진 사각형 그리기
    void DrawScreenRect(Rect rect, Color color, Texture2D texture)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, texture);
        GUI.color = Color.white;
    }

    // 사각형 테두리 그리기
    void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        GUI.color = color;
        
        // 상단
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), borderTexture);
        // 하단
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), borderTexture);
        // 좌측
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), borderTexture);
        // 우측
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), borderTexture);
        
        GUI.color = Color.white;
    }

    // 텍스처 생성
    void CreateTextures()
    {
        boxTexture = new Texture2D(1, 1);
        boxTexture.SetPixel(0, 0, Color.white);
        boxTexture.Apply();

        borderTexture = new Texture2D(1, 1);
        borderTexture.SetPixel(0, 0, Color.white);
        borderTexture.Apply();
    }

    // 현재 선택된 유닛들 가져오기
    public List<UnitAgent> GetSelectedUnits()
    {
        return new List<UnitAgent>(selectedUnits);
    }

    // 특정 유닛이 선택되었는지 확인
    public bool IsUnitSelected(UnitAgent unit)
    {
        return selectedUnits.Contains(unit);
    }

    // 선택된 유닛 수
    public int GetSelectedCount()
    {
        return selectedUnits.Count;
    }

    void OnDestroy()
    {
        if (boxTexture != null) Destroy(boxTexture);
        if (borderTexture != null) Destroy(borderTexture);
    }
}
