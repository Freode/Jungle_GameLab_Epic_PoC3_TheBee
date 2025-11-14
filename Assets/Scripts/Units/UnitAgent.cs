using UnityEngine;

public class UnitAgent : MonoBehaviour
{
    public int id;
    public int q;
    public int r;
    public int visionRange = 3;
    public float hexSize = 0.5f;

    // Whether this unit can be moved by player clicking tiles
    public bool canMove = true;

    private bool isRegistered = false;

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
    }

    private void OnDestroy()
    {
        Unregister();
    }

    // Explicit registration method if needed
    public void RegisterWithFog()
    {
        if (FogOfWarManager.Instance == null) return;
        FogOfWarManager.Instance.RegisterUnit(id, q, r, visionRange);
        isRegistered = true;
    }

    public void Unregister()
    {
        FogOfWarManager.Instance?.UnregisterUnit(id);
        isRegistered = false;
    }

    // update logical tile position; do not change transform here to avoid jitter
    // ensures FogOfWarManager gets the unit's visionRange applied
    public void SetPosition(int nq, int nr)
    {
        q = nq; r = nr;
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
        Color target = selected ? Color.yellow : originalColor;
        if (cachedSprite != null)
        {
            cachedSprite.color = target;
            return;
        }
        if (cachedRenderer != null)
        {
            mpb.Clear();
            mpb.SetColor("_Color", target);
            cachedRenderer.SetPropertyBlock(mpb);
        }
    }
}
