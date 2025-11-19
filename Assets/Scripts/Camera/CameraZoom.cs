using UnityEngine;

/// <summary>
/// 마우스 휠을 사용한 카메라 줌인/줌아웃
/// </summary>
public class CameraZoom : MonoBehaviour
{
    [Header("줌 설정")]
    [Tooltip("줌 속도")]
    public float zoomSpeed = 2f;
    
    [Tooltip("최소 줌 (카메라 크기 최소값)")]
    public float minZoom = 2f;
    
    [Tooltip("최대 줌 (카메라 크기 최대값)")]
    public float maxZoom = 10f;
    
    [Tooltip("줌 부드러움 정도 (0 = 즉시, 1 = 매우 부드러움)")]
    [Range(0f, 1f)]
    public float zoomSmoothness = 0.1f;

    private Camera cam;
    private float targetZoom;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        // 2D 카메라인 경우 orthographicSize 사용
        if (cam != null && cam.orthographic)
        {
            targetZoom = cam.orthographicSize;
        }
        // 3D 카메라인 경우 현재 Z 위치 사용
        else if (cam != null)
        {
            targetZoom = -cam.transform.position.z;
        }
    }

    void Update()
    {
        if (cam == null) return;

        // 마우스 휠 입력
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // 줌 방향 (휠 위로 = 줌인 = 값 감소)
            targetZoom -= scrollInput * zoomSpeed;
            
            // 줌 범위 제한
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // 부드러운 줌 적용
        if (cam.orthographic)
        {
            // 2D 카메라: orthographicSize 조정
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, 1f - zoomSmoothness);
        }
        else
        {
            // 3D 카메라: Z 위치 조정
            Vector3 pos = cam.transform.position;
            pos.z = Mathf.Lerp(pos.z, -targetZoom, 1f - zoomSmoothness);
            cam.transform.position = pos;
        }
    }

    /// <summary>
    /// 특정 줌 레벨로 즉시 설정
    /// </summary>
    public void SetZoom(float zoom)
    {
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        
        if (cam != null)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize = targetZoom;
            }
            else
            {
                Vector3 pos = cam.transform.position;
                pos.z = -targetZoom;
                cam.transform.position = pos;
            }
        }
    }

    /// <summary>
    /// 특정 줌 레벨로 부드럽게 전환
    /// </summary>
    public void SetZoomSmooth(float zoom)
    {
        targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    /// <summary>
    /// 현재 줌 레벨 가져오기
    /// </summary>
    public float GetCurrentZoom()
    {
        if (cam == null) return 0f;
        
        if (cam.orthographic)
        {
            return cam.orthographicSize;
        }
        else
        {
            return -cam.transform.position.z;
        }
    }
}
