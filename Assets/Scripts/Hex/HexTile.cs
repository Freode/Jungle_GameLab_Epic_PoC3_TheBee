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

    // Fog of war 상태
    public enum FogState { Hidden, Revealed, Visible }
    public FogState fogState = FogState.Hidden;

    // 기본 색상(지형 색상에서 가져옴)
    private Color baseColor = Color.white;

    // 현재/목표 색상 및 보간 속도
    private Color currentColor = Color.white;
    private Color targetColor = Color.white;
    [SerializeField] private float colorLerpSpeed = 6f;

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
        if (terrain != null) baseColor = terrain.color;
        else baseColor = Color.white;
        // update target depending on fog state
        UpdateFogTargetColor();
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
        UpdateFogTargetColor();
    }

    void UpdateFogTargetColor()
    {
        if (terrain == null)
        {
            baseColor = Color.white;
        }

        switch (fogState)
        {
            case FogState.Hidden:
                // almost black
                targetColor = baseColor * 0.08f;
                targetColor.a = 1f;
                break;
            case FogState.Revealed:
                // gray-ish (retain lightness but desaturated)
                var gray = Color.Lerp(baseColor, Color.gray, 0.6f);
                targetColor = gray;
                targetColor.a = 1f;
                break;
            case FogState.Visible:
                targetColor = baseColor;
                targetColor.a = 1f;
                break;
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
}
