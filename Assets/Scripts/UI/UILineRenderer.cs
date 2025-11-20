using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Canvas에서 선을 그리는 UI Line Renderer
/// - MaskableGraphic 클래스를 상속하여 Canvas에서 렌더링 및 Mask 적용
/// - 동적으로 다각형(Polygon) 생성
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    [Header("선 설정")]
    [Tooltip("선의 두께")]
    public float lineWidth = 2f;
    
    [Tooltip("선의 색상")]
    public Color lineColor = Color.white;
    
    [Tooltip("선을 구성하는 점들")]
    private List<Vector2> points = new List<Vector2>();
    
    /// <summary>
    /// 선의 점들을 설정
    /// </summary>
    public void SetPoints(List<Vector2> newPoints)
    {
        points = newPoints;
        SetVerticesDirty(); // UI를 다시 그리도록 표시
    }
    
    /// <summary>
    /// 선의 점들을 가져옴
    /// </summary>
    public List<Vector2> GetPoints()
    {
        return points;
    }
    
    /// <summary>
    /// UI 메쉬 생성
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        
        if (points == null || points.Count < 2)
            return;
        
        // 각 선분에 대해 사각형(Quad) 생성
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 point1 = points[i];
            Vector2 point2 = points[i + 1];
            
            // 선의 방향
            Vector2 direction = (point2 - point1).normalized;
            
            // 선에 수직인 방향 (법선 벡터)
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (lineWidth * 0.5f);
            
            // 사각형의 4개 꼭지점
            Vector2 v1 = point1 - perpendicular;
            Vector2 v2 = point1 + perpendicular;
            Vector2 v3 = point2 + perpendicular;
            Vector2 v4 = point2 - perpendicular;
            
            // 현재 정점 인덱스
            int vertexIndex = vh.currentVertCount;
            
            // UIVertex 생성 및 추가
            vh.AddVert(CreateVertex(v1, lineColor));
            vh.AddVert(CreateVertex(v2, lineColor));
            vh.AddVert(CreateVertex(v3, lineColor));
            vh.AddVert(CreateVertex(v4, lineColor));
            
            // 삼각형 2개로 사각형 구성
            vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vh.AddTriangle(vertexIndex + 2, vertexIndex + 3, vertexIndex);
        }
    }
    
    /// <summary>
    /// UIVertex 생성 헬퍼 함수
    /// </summary>
    private UIVertex CreateVertex(Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        return vertex;
    }
    
    /// <summary>
    /// 색상이 변경되었을 때 호출
    /// </summary>
    public override Color color
    {
        get { return lineColor; }
        set
        {
            lineColor = value;
            SetVerticesDirty();
        }
    }
}
