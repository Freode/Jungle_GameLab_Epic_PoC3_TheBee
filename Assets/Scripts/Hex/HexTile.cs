using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class HexTile : MonoBehaviour
{
    // axial coordinates (q, r)
    public int q;
    public int r;

    // 지형 정보
    public TerrainType terrain;

    // resource amount on this tile (dynamic)
    public int resourceAmount = 0;
    private int initialResource = 0;

    // Fog of war 상태
    public enum FogState { Hidden, Revealed, Visible }
    public FogState fogState = FogState.Hidden;

    // 기본 색상(지형 색상에서 가져옴)
    private Color baseColor = Color.white;

    // 현재/목표 색상 및 보간 속도
    private Color currentColor = Color.white;
    private Color targetColor = Color.white;
    [SerializeField] private float colorLerpSpeed = 6f;

    // 색 변화 시 사용할 고갈 색상
    [SerializeField] private Color depletedColor = Color.gray;

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
                targetColor = tileColor * 0.08f;
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

        // 리소스가 남아있는 경우
        if (resourceAmount > 0)
        {
            float t = (float)resourceAmount / (float)initialResource;
            // t==1 => full resource => baseColor; t==0 => depletedColor에 가까움
            return Color.Lerp(depletedColor, baseColor, t);
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
}
