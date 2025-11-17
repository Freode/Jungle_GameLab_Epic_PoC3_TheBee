# ?? Order in Layer에 따른 콜리전 우선순위 아이디어

## 문제 상황
여러 스프라이트가 겹쳐있을 때, SpriteRenderer의 Order in Layer 값이 높은 것(화면 앞쪽)을 우선적으로 클릭하고 싶습니다.

---

## ?? 해결 아이디어

### 방법 1: Raycast 정렬 (추천) ?

Physics2D.RaycastAll()을 사용하여 모든 충돌을 감지한 후, Order in Layer로 정렬합니다.

#### 장점
- ? 2D 콜라이더와 완벽 호환
- ? 구현이 간단하고 직관적
- ? 성능 영향 최소

#### 구현 코드

```csharp
public class LayerOrderClickDetector : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UnitAgent clickedUnit = GetTopMostUnit();
            if (clickedUnit != null)
            {
                Debug.Log($"클릭된 유닛: {clickedUnit.name}");
                // 유닛 선택 처리
            }
        }
    }

    UnitAgent GetTopMostUnit()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // 모든 충돌 감지
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
        
        if (hits.Length == 0) return null;

        // Order in Layer가 가장 높은 것 찾기
        UnitAgent topUnit = null;
        int highestOrder = int.MinValue;

        foreach (var hit in hits)
        {
            var unit = hit.collider.GetComponentInParent<UnitAgent>();
            if (unit == null) continue;

            var spriteRenderer = unit.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) continue;

            // Order in Layer가 더 높으면 교체
            if (spriteRenderer.sortingOrder > highestOrder)
            {
                highestOrder = spriteRenderer.sortingOrder;
                topUnit = unit;
            }
        }

        return topUnit;
    }
}
```

---

### 방법 2: Z 좌표 활용

Y 좌표에 따라 Order in Layer를 자동으로 조정하고, Z 좌표도 함께 설정합니다.

#### 장점
- ? 자동으로 정렬됨
- ? 시각적으로도 자연스러움

#### 단점
- ? 추가 컴포넌트 필요
- ? 매 프레임 업데이트 필요

#### 구현 코드

```csharp
public class SpriteDepthSorter : MonoBehaviour
{
    [Header("정렬 설정")]
    public float sortingOrderMultiplier = 100f; // Y 좌표당 Order 증가량
    public float zDepthMultiplier = 0.01f;      // Y 좌표당 Z 증가량

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (spriteRenderer == null) return;

        // Y 좌표가 낮을수록 앞에 (Order 높게)
        float yPos = transform.position.y;
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-yPos * sortingOrderMultiplier);
        
        // Z 좌표도 조정 (Raycast용)
        Vector3 pos = transform.position;
        pos.z = yPos * zDepthMultiplier;
        transform.position = pos;
    }
}
```

---

### 방법 3: 커스텀 Collider 레이어

Order in Layer에 따라 Collider를 다른 레이어에 배치합니다.

#### 장점
- ? Physics 엔진이 자동 처리
- ? 복잡한 로직 불필요

#### 단점
- ? 레이어 수 제한 (32개)
- ? 레이어 관리 복잡

#### 구현 방법

```csharp
public class ColliderLayerManager : MonoBehaviour
{
    [Header("레이어 매핑")]
    public string[] orderLayerNames = { "Layer0", "Layer1", "Layer2", "Layer3" };

    void Start()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        // Order in Layer를 레이어 인덱스로 변환
        int layerIndex = Mathf.Clamp(spriteRenderer.sortingOrder, 0, orderLayerNames.Length - 1);
        gameObject.layer = LayerMask.NameToLayer(orderLayerNames[layerIndex]);
    }
}
```

그리고 Raycast 시:

```csharp
// 높은 레이어부터 순서대로 Raycast
for (int i = orderLayerNames.Length - 1; i >= 0; i--)
{
    int layerMask = 1 << LayerMask.NameToLayer(orderLayerNames[i]);
    var hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, layerMask);
    if (hit.collider != null)
    {
        return hit.collider.GetComponent<UnitAgent>();
    }
}
```

---

### 방법 4: BoxCollider2D Z 오프셋 (Unity 2D 특화)

BoxCollider2D의 Z 위치를 Order in Layer와 연동시킵니다.

#### 구현 코드

```csharp
public class ColliderDepthSetter : MonoBehaviour
{
    void Start()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        var boxCollider = GetComponent<BoxCollider2D>();
        
        if (spriteRenderer != null && boxCollider != null)
        {
            // Order in Layer를 Z 오프셋으로 변환
            Vector2 offset = boxCollider.offset;
            Vector3 pos = transform.position;
            pos.z = -spriteRenderer.sortingOrder * 0.001f; // 작은 오프셋
            transform.position = pos;
        }
    }
}
```

---

## ?? 추천 방법 비교표

| 방법 | 난이도 | 성능 | 유연성 | 추천도 |
|------|--------|------|--------|--------|
| RaycastAll 정렬 | 쉬움 | 높음 | 높음 | ????? |
| Z 좌표 활용 | 중간 | 중간 | 중간 | ??? |
| 커스텀 레이어 | 어려움 | 높음 | 낮음 | ?? |
| Z 오프셋 | 쉬움 | 높음 | 낮음 | ??? |

---

## ?? 실전 통합 예제 (방법 1 기반)

TileClickMover에 통합하는 방법:

```csharp
void HandleLeftClick()
{
    // 3D Raycast
    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
    {
        var unit = hit.collider.GetComponentInParent<UnitAgent>();
        if (unit != null)
        {
            SelectUnit(unit);
            return;
        }
    }

    // 2D Raycast - Order in Layer 정렬 적용
    Vector3 wp = mainCamera.ScreenToWorldPoint(Input.mousePosition);
    RaycastHit2D[] hits = Physics2D.RaycastAll(wp, Vector2.zero);
    
    if (hits.Length > 0)
    {
        // Order in Layer가 가장 높은 유닛 찾기
        UnitAgent topUnit = null;
        int highestOrder = int.MinValue;

        foreach (var hit2D in hits)
        {
            var unit = hit2D.collider.GetComponentInParent<UnitAgent>();
            if (unit == null) continue;

            var spriteRenderer = unit.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) continue;

            if (spriteRenderer.sortingOrder > highestOrder)
            {
                highestOrder = spriteRenderer.sortingOrder;
                topUnit = unit;
            }
        }

        if (topUnit != null)
        {
            SelectUnit(topUnit);
            return;
        }
    }

    // 타일 체크 등...
}
```

---

## ?? 자동 Order 관리 시스템

유닛의 Y 좌표에 따라 자동으로 Order in Layer를 업데이트:

```csharp
public class AutoSortingOrder : MonoBehaviour
{
    [Header("정렬 설정")]
    public bool autoUpdate = true;
    public float updateInterval = 0.1f; // 0.1초마다 업데이트
    
    [Header("정렬 공식")]
    public int baseSortingOrder = 0;
    public float yMultiplier = -100f; // Y가 낮을수록 앞에

    private SpriteRenderer spriteRenderer;
    private float lastUpdateTime;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        UpdateSortingOrder();
    }

    void Update()
    {
        if (!autoUpdate) return;
        
        // 일정 시간마다만 업데이트 (최적화)
        if (Time.time - lastUpdateTime < updateInterval)
            return;

        UpdateSortingOrder();
        lastUpdateTime = Time.time;
    }

    void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;

        float yPos = transform.position.y;
        int newOrder = baseSortingOrder + Mathf.RoundToInt(yPos * yMultiplier);
        spriteRenderer.sortingOrder = newOrder;
    }

    // 외부에서 강제 업데이트
    public void ForceUpdate()
    {
        UpdateSortingOrder();
    }
}
```

---

## ?? 성능 최적화 팁

### 1. Raycast 캐싱
```csharp
private RaycastHit2D[] cachedHits = new RaycastHit2D[10];

UnitAgent GetTopMostUnit()
{
    Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    
    // GetRaycastNonAlloc 사용 (GC 최적화)
    int hitCount = Physics2D.RaycastNonAlloc(worldPos, Vector2.zero, cachedHits);
    
    // ...정렬 로직...
}
```

### 2. 정렬 최적화
```csharp
// LINQ 대신 단순 루프 사용
UnitAgent topUnit = null;
int highestOrder = int.MinValue;

for (int i = 0; i < hitCount; i++)
{
    // ...체크 로직...
}
```

### 3. 컴포넌트 캐싱
```csharp
// UnitAgent에 SpriteRenderer 캐싱
public class UnitAgent : MonoBehaviour
{
    private SpriteRenderer cachedSpriteRenderer;
    
    public int GetSortingOrder()
    {
        if (cachedSpriteRenderer == null)
            cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        
        return cachedSpriteRenderer != null ? cachedSpriteRenderer.sortingOrder : 0;
    }
}
```

---

## ?? 사용 예시

### 시나리오: 여러 일꾼이 겹쳐있을 때

```
일꾼 A: Order in Layer = 100, Y = 5.0
일꾼 B: Order in Layer = 150, Y = 3.0  ← 화면에서 더 앞쪽
일꾼 C: Order in Layer = 50,  Y = 7.0

클릭 시: 일꾼 B가 선택됨 (Order 가장 높음)
```

### 코드로 구현
```csharp
void OnMouseClick()
{
    // RaycastAll로 3개 모두 감지
    // → 일꾼 B의 Order가 150으로 가장 높음
    // → 일꾼 B 선택
}
```

---

## ? 최종 추천

### 대부분의 경우: **방법 1 (RaycastAll 정렬)**
```csharp
// 간단하고 효과적
// 성능도 우수
// 유지보수 쉬움
```

### 대규모 유닛 (100개 이상): **방법 2 (Z 좌표 활용)**
```csharp
// 자동 정렬
// Raycast 한 번만 필요
// 초기 설정만 하면 끝
```

### 특수한 경우: **방법 3 (레이어 분리)**
```csharp
// 레이어별로 완전히 분리하고 싶을 때
// 물리 충돌도 계층별로 제어하고 싶을 때
```

---

## ?? 구현 체크리스트

```
[ ] 방법 선택 (RaycastAll 추천)
[ ] 코드 통합 (TileClickMover 수정)
[ ] 모든 유닛에 SpriteRenderer 확인
[ ] Order in Layer 값 설정 (Y 좌표 기반 권장)
[ ] 테스트: 여러 유닛 겹치기
[ ] 테스트: 클릭 시 앞쪽 유닛 선택되는지
[ ] 성능 테스트 (100개 이상 유닛)
[ ] 최적화 적용 (필요시)
```

이 아이디어들 중에서 **방법 1 (RaycastAll 정렬)**을 가장 추천합니다! ??
