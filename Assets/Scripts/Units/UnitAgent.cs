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
        if (id == 0) id = GetInstanceID();
        Register();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    public void Register()
    {
        FogOfWarManager.Instance?.RegisterUnit(id, q, r);
        isRegistered = true;
    }

    public void Unregister()
    {
        FogOfWarManager.Instance?.UnregisterUnit(id);
        isRegistered = false;
    }

    // update logical tile position; do not change transform here to avoid jitter
    public void SetPosition(int nq, int nr)
    {
        q = nq; r = nr;
        if (FogOfWarManager.Instance == null) return;
        if (isRegistered)
        {
            FogOfWarManager.Instance.UpdateUnitPosition(id, q, r);
        }
        else
        {
            FogOfWarManager.Instance.RegisterUnit(id, q, r);
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
