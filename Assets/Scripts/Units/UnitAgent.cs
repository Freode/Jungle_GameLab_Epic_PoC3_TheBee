using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    public int id;
    public int q;
    public int r;
    
    [Header("유닛 정보")]
    [Tooltip("유닛 이름 (비어있으면 자동 생성)")]
    public string unitName = "";
    
    public int visionRange = 3;
    public float hexSize = 0.5f;
    public bool useSeqRenderLayerOrder = false;

    // faction
    public Faction faction = Faction.Player;

    // home hive reference (if spawned by a hive)
    public Hive homeHive;

    // Whether this unit can be moved by player clicking tiles
    public bool canMove = true;

    // Queen bee flag
    public bool isQueen = false;

    // ✅ 타일 이동 시 랜덤 위치 사용 여부
    [Tooltip("타일 이동 시 정중앙 근처 랜덤 위치로 이동 (false면 정중앙으로 이동)")]
    public bool useRandomPosition = true;

    // Worker behavior flags for hive relocation
    public bool isFollowingQueen = false; // Worker is following queen during relocation
    public bool hasManualOrder = false; // Worker received manual move order
    
    // ✅ 자원 보유 상태
    [HideInInspector]
    public bool isCarryingResource = false; // 자원을 들고 있는지 여부
    [HideInInspector]
    public Color gatherColor = Color.yellow; // 자원 채취 시 색상

    private bool isRegistered = false;
    private bool isSelected = false; // ✅ 선택 상태 추적

    // selection visuals
    private Renderer cachedRenderer;
    private SpriteRenderer cachedSprite;
    private UnityEngine.Color originalColor = UnityEngine.Color.white;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (id == 0) id = GetInstanceID();
        cachedRenderer = GetComponentInChildren<Renderer>();
        cachedSprite = GetComponentInChildren<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        if (cachedSprite != null)
        {
            originalColor = cachedSprite.color;
        }
        else if (cachedRenderer != null)
        {
            // leave originalColor as white if renderer used
        }
    }

    private void Start()
    {
        // Do not auto-register here. Registration should happen after the unit's initial placement via SetPosition or explicit RegisterWithFog().
        TileManager.Instance?.RegisterUnit(this);
        
        // ✅ 2. Sorting Order 자동 할당
        if (SortingOrderManager.Instance != null && useSeqRenderLayerOrder)
        {
            SortingOrderManager.Instance.ApplySortingOrder(this);
        }
    }

    private void OnEnable()
    {
        TileManager.Instance?.RegisterUnit(this);
    }

    private void OnDisable()
    {
        TileManager.Instance?.UnregisterUnit(this);
        Unregister();
    }

    private void OnDestroy()
    {
        TileManager.Instance?.UnregisterUnit(this);
        Unregister();
    }

    // Explicit registration method if needed
    public void RegisterWithFog()
    {
        // Enemy 유닛은 전장의 안개에 영향을 주지 않음
        if (faction == Faction.Enemy)
            return;

        if (FogOfWarManager.Instance == null) return;
        FogOfWarManager.Instance.RegisterUnit(id, q, r, visionRange);
        isRegistered = true;
    }

    public void Unregister()
    {
        // Enemy 유닛은 등록되지 않았으므로 해제도 필요 없음
        if (faction == Faction.Enemy)
            return;

        FogOfWarManager.Instance?.UnregisterUnit(id);
        isRegistered = false;
    }

    // update logical tile position; do not change transform here to avoid jitter
    // ensures FogOfWarManager gets the unit's visionRange applied
    public void SetPosition(int nq, int nr)
    {
        q = nq; r = nr;

        // Enemy 유닛은 전장의 안개에 영향을 주지 않음
        if (faction == Faction.Enemy)
            return;

        if (FogOfWarManager.Instance == null) return;
        if (isRegistered)
        {
            FogOfWarManager.Instance.UpdateUnitPosition(id, q, r, visionRange);
        }
        else
        {
            FogOfWarManager.Instance.RegisterUnit(id, q, r, visionRange);
            isRegistered = true;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected; // ✅ 선택 상태 저장
        
        Color targetColor;
        
        if (selected)
        {
            // 선택된 경우: 항상 선택 색상 (연한 연두색)
            targetColor = new Color(144f / 255f, 238f / 255f, 144f / 255f, 1f);
        }
        else
        {
            // 선택 해제된 경우
            if (isCarryingResource)
            {
                // 자원을 들고 있으면 gatherColor
                targetColor = gatherColor;
            }
            else
            {
                // 자원이 없으면 원래 색상
                targetColor = originalColor;
            }
        }
        
        // 색상 적용
        if (cachedSprite != null)
        {
            cachedSprite.color = targetColor;
            return;
        }
        if (cachedRenderer != null)
        {
            mpb.Clear();
            mpb.SetColor("_Color", targetColor);
            cachedRenderer.SetPropertyBlock(mpb);
        }
    }
    
    /// <summary>
    /// 자원 보유 상태 설정 ✅
    /// </summary>
    public void SetCarryingResource(bool carrying)
    {
        isCarryingResource = carrying;
        
        // 선택되지 않은 상태에서만 색상 변경
        if (!isSelected)
        {
            UpdateColor();
        }
    }
    
    /// <summary>
    /// 색상 업데이트 (내부용) ✅
    /// </summary>
    private void UpdateColor()
    {
        Color targetColor = isCarryingResource ? gatherColor : originalColor;
        
        if (cachedSprite != null)
        {
            cachedSprite.color = targetColor;
        }
        else if (cachedRenderer != null)
        {
            mpb.Clear();
            mpb.SetColor("_Color", targetColor);
            cachedRenderer.SetPropertyBlock(mpb);
        }
    }

    // Check if worker can move to a tile based on hive presence
    public bool CanMoveToTile(int targetQ, int targetR)
    {
        // If has manual order, ignore restrictions
        if (hasManualOrder) return true;

        // If no home hive (relocated), can move anywhere
        if (homeHive == null) return true;

        // If home hive exists, check if target is within activity radius
        int radius = 5;
        if (HiveManager.Instance != null) radius = HiveManager.Instance.hiveActivityRadius;

        int dq = targetQ - homeHive.q;
        int dr = targetR - homeHive.r;
        int distance = (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;

        return distance <= radius;
    }
}
