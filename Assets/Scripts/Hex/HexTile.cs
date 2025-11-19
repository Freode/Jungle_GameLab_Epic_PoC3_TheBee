using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class HexTile : MonoBehaviour
{
    // axial coordinates (q, r)
    public int q;
    public int r;

    // 지형 타입
    public TerrainType terrain;

    // resource amount on this tile (dynamic)
    public int resourceAmount = 0;
    private int initialResource = 0;

    // Fog of war 상태
    public enum FogState { Hidden, Revealed, Visible }
    public FogState fogState = FogState.Hidden;

    // 타일 위의 EnemyHive (있으면 설정) ?
    public EnemyHive enemyHive;

    // 기본 색상(지형 타입에서 가져옴)
    private Color baseColor = Color.white;

    // 현재/목표 색상 및 보간 속도
    private Color currentColor = Color.white;
    private Color targetColor = Color.white;
    [SerializeField] private float colorLerpSpeed = 6f;

    // ✅ 2. 자원량에 따른 색상 단계 설정 (절대 수치 기반)
    [Header("자원 색상 설정")]
    [Tooltip("자원량 400 이상")]
    [SerializeField] private Color resourceVeryHighColor = new Color(1.0f, 0.9f, 0.0f); // 진한 노란색
    
    [Tooltip("자원량 300~399")]
    [SerializeField] private Color resourceHighColor = new Color(1.0f, 1.0f, 0.3f); // 밝은 노란색
    
    [Tooltip("자원량 200~299")]
    [SerializeField] private Color resourceMediumColor = new Color(1.0f, 0.9f, 0.5f); // 연한 노란색
    
    [Tooltip("자원량 100~199")]
    [SerializeField] private Color resourceLowColor = new Color(1.0f, 0.7f, 0.2f); // 주황색
    
    [Tooltip("자원량 1~99")]
    [SerializeField] private Color resourceVeryLowColor = new Color(1.0f, 0.5f, 0.1f); // 진한 주황색
    
    [Tooltip("자원 고갈 (0)")]
    [SerializeField] private Color depletedColor = new Color(0.8f, 0.4f, 0.2f); // 갈색

    // 렌더러 캐시
    private Renderer cachedRenderer;
    private SpriteRenderer cachedSprite;
    private MaterialPropertyBlock mpb;

    // 6개의 이웃 방향 (pointy-top axial)
    public static readonly Vector2Int[] NeighborDirections = new Vector2Int[]
    {
        new Vector2Int(+1, 0),
        new Vector2Int(+1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, +1),
        new Vector2Int(0, +1),
    };

    void Awake()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
        cachedSprite = GetComponentInChildren<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void SetCoords(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public Vector2Int AxialCoord()
    {
        return new Vector2Int(q, r);
    }

    public void SetTerrain(TerrainType newTerrain)
    {
        terrain = newTerrain;
        if (terrain != null)
        {
            baseColor = terrain.color;
            resourceAmount = terrain.resourceYield;
            initialResource = resourceAmount;
        }
        else
        {
            baseColor = Color.white;
            resourceAmount = 0;
            initialResource = 0;
        }
        // update combined color
        UpdateTargetColor();
        // initialize currentColor if first time
        currentColor = targetColor;
        ApplyImmediateColor();
    }

    void Update()
    {
        // Smoothly interpolate towards targetColor
        if (currentColor != targetColor)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorLerpSpeed);
            ApplyCurrentColor();
        }
    }

    // Set fog state and update visuals
    public void SetFogState(FogState state)
    {
        fogState = state;
        UpdateTargetColor();
    }

    // 통합된 색상 업데이트 메서드
    void UpdateTargetColor()
    {
        // 1단계: 기본 타일 색상 결정 (리소스 상태 반영)
        Color tileColor = GetTileBaseColor();
        
        // 2단계: Fog 상태에 따라 최종 색상 결정
        switch (fogState)
        {
            case FogState.Hidden:
                // almost black
                targetColor = Color.black;
                targetColor.a = 1f;
                break;
            case FogState.Revealed:
                // gray-ish (retain lightness but desaturated)
                targetColor = Color.Lerp(tileColor, Color.gray, 0.6f);
                targetColor.a = 1f;
                break;
            case FogState.Visible:
                targetColor = tileColor;
                targetColor.a = 1f;
                break;
        }
    }

    // 리소스 상태를 반영한 타일의 기본 색상 계산
    Color GetTileBaseColor()
    {
        if (terrain == null)
        {
            return Color.white;
        }

        // 리소스가 정의되지 않은 타일은 지형 색상 그대로 반환
        if (initialResource <= 0)
        {
            return baseColor;
        }

        // ✅ 리소스가 남아있는 경우 - 절대 수치 기반 단계별 색상
        if (resourceAmount > 0)
        {
            // 400 이상: 매우 높음 (진한 노란색)
            if (resourceAmount >= 400)
            {
                return resourceVeryHighColor;
            }
            // 300~399: 높음 (밝은 노란색)
            else if (resourceAmount >= 300)
            {
                float t = (resourceAmount - 300) / 100f;
                return Color.Lerp(resourceHighColor, resourceVeryHighColor, t);
            }
            // 200~299: 중간 (연한 노란색)
            else if (resourceAmount >= 200)
            {
                float t = (resourceAmount - 200) / 100f;
                return Color.Lerp(resourceMediumColor, resourceHighColor, t);
            }
            // 100~199: 낮음 (주황색)
            else if (resourceAmount >= 100)
            {
                float t = (resourceAmount - 100) / 100f;
                return Color.Lerp(resourceLowColor, resourceMediumColor, t);
            }
            // 1~99: 매우 낮음 (진한 주황색)
            else
            {
                float t = resourceAmount / 100f;
                return Color.Lerp(resourceVeryLowColor, resourceLowColor, t);
            }
        }
        else
        {
            // 리소스가 고갈된 경우: normalTerrain 색상 사용
            var gm = GameManager.Instance;
            if (gm != null && gm.normalTerrain != null)
            {
                return gm.normalTerrain.color;
            }
            else
            {
                return depletedColor;
            }
        }
    }

    void ApplyCurrentColor()
    {
        if (cachedRenderer != null)
        {
            cachedRenderer.GetPropertyBlock(mpb);
            // Try common property names
            if (mpb != null)
            {
                mpb.SetColor("_Color", currentColor);
                mpb.SetColor("_BaseColor", currentColor);
            }
            cachedRenderer.SetPropertyBlock(mpb);
            return;
        }

        if (cachedSprite != null)
        {
            cachedSprite.color = currentColor;
        }
    }

    void ApplyImmediateColor()
    {
        currentColor = targetColor;
        ApplyCurrentColor();
    }

    // decrease resource, return amount actually taken
    public int TakeResource(int amount)
    {
        int taken = Mathf.Min(resourceAmount, amount);
        resourceAmount -= taken;
        if (resourceAmount < 0) resourceAmount = 0;
        UpdateTargetColor();
        return taken;
    }

    /// <summary>
    /// ✅ 자원량 직접 설정 (말벌집 파괴 등)
    /// </summary>
    public void SetResourceAmount(int amount)
    {
        resourceAmount = Mathf.Max(0, amount);
        
        // initialResource도 업데이트하여 색상 계산이 정확하도록
        if (resourceAmount > initialResource)
        {
            initialResource = resourceAmount;
        }
        
        UpdateTargetColor();
    }
}
