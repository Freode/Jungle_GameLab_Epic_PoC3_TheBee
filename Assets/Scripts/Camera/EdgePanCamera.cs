using UnityEngine;

// Move camera when mouse near screen edges
public class EdgePanCamera : MonoBehaviour
{
    public Camera cam;
    public float panSpeed = 10f;
    public float borderThickness = 10f; // pixels
    public Vector2 panLimitsMin = new Vector2(-50, -50);
    public Vector2 panLimitsMax = new Vector2(50, 50);

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
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
}
