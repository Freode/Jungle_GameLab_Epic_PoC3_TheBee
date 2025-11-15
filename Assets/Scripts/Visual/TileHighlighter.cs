using UnityEngine;

// Visual indicator for hovered tile and activity radius
public class TileHighlighter : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject highlightPrefab; // simple ring sprite or mesh (optional)
    public Color hoverColor = Color.green;
    public Color radiusColor = Color.green;

    public HexBoundaryHighlighter boundaryHighlighter; // optional, used when no prefab

    private GameObject hoverInstance;
    private GameObject radiusInstance;

    // LineRenderer used to draw hex outline when boundaryHighlighter is not available
    private LineRenderer hoverLine;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        // ensure prefab is not null
        if (highlightPrefab == null && boundaryHighlighter == null)
        {
            Debug.LogWarning("TileHighlighter: highlightPrefab and boundaryHighlighter are both null. No hover visuals will be shown.");
        }
    }

    void Update()
    {
        // hover effect
        Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        // ensure z for 2D raycast
        wp.z = 0f;
        var hit2 = Physics2D.Raycast(new Vector2(wp.x, wp.y), Vector2.zero);
        if (hit2.collider != null)
        {
            var tile = hit2.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                ShowHoverAt(tile);
                return;
            }
        }

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            var tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                ShowHoverAt(tile);
                return;
            }
        }

        HideHover();
    }

    void ShowHoverAt(HexTile tile)
    {
        if (boundaryHighlighter != null)
        {
            // draw outer edges of this single tile
            boundaryHighlighter.ShowBoundaryAt(tile.q, tile.r, 0);
            // also hide sprite-based hover
            if (hoverInstance != null) hoverInstance.SetActive(false);
            if (hoverLine != null) hoverLine.gameObject.SetActive(false);
            return;
        }

        // When there's no HexBoundaryHighlighter, draw a LineRenderer hex outline around the tile
        float size = 0.5f;
        if (GameManager.Instance != null) size = GameManager.Instance.hexSize;

        if (hoverLine == null)
        {
            var go = new GameObject("TileHoverOutline");
            go.transform.SetParent(transform, false);
            hoverLine = go.AddComponent<LineRenderer>();
            hoverLine.loop = true;
            hoverLine.numCapVertices = 2;
            hoverLine.numCornerVertices = 2;
            hoverLine.useWorldSpace = true;
            hoverLine.alignment = LineAlignment.TransformZ;
            hoverLine.widthMultiplier = 0.05f;
            // default material
            hoverLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        // compute hex corners
        Vector3 center = TileHelper.HexToWorld(tile.q, tile.r, size);
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            corners[i] = center + GetHexCorner(size, i);
        }

        hoverLine.positionCount = corners.Length;
        hoverLine.SetPositions(corners);
        hoverLine.startWidth = 0.05f;
        hoverLine.endWidth = 0.05f;
        hoverLine.startColor = hoverColor;
        hoverLine.endColor = hoverColor;
        hoverLine.gameObject.SetActive(true);

        // hide sprite hover if present
        if (hoverInstance != null) hoverInstance.SetActive(false);
    }

    void HideHover()
    {
        if (hoverInstance != null) hoverInstance.SetActive(false);
        if (hoverLine != null) hoverLine.gameObject.SetActive(false);
        if (boundaryHighlighter != null) boundaryHighlighter.Clear();
    }

    Vector3 GetHexCorner(float size, int i)
    {
        float angleDeg = 60f * i + 30f;
        float angleRad = Mathf.Deg2Rad * angleDeg;
        return new Vector3(size * Mathf.Cos(angleRad), size * Mathf.Sin(angleRad), 0f);
    }

    // Show radius around hive - creates a scaled circle sprite centered on hive
    public void ShowRadius(Hive hive, int radius)
    {
        if (hive == null) return;
        if (boundaryHighlighter != null)
        {
            boundaryHighlighter.ShowBoundary(hive, radius);
            return;
        }

        if (highlightPrefab == null) return;
        if (radiusInstance == null) radiusInstance = Instantiate(highlightPrefab, transform);
        radiusInstance.transform.position = TileHelper.HexToWorld(hive.q, hive.r, 0.5f);
        // scale: approximate hex spacing: width = sqrt(3)*size, height = 1.5*size per row
        float size = (radius * 2 + 1) * 0.5f; // rough
        radiusInstance.transform.localScale = new Vector3(size, size, 1f);
        var sr = radiusInstance.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = radiusColor;
        radiusInstance.SetActive(true);
    }

    public void HideRadius()
    {
        if (radiusInstance != null) radiusInstance.SetActive(false);
        if (boundaryHighlighter != null) boundaryHighlighter.Clear();
    }
}
