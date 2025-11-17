# ?? 경로 큐잉 & 여왕벌 활성화 시스템 가이드

## 완료된 개선사항

### ? 1. 연속 이동 명령 시 경로 큐잉
- 이동 중 재클릭 시 목적지 저장
- 현재 이동 완료 후 새 경로 자동 탐색
- 부드러운 연속 이동

### ? 2. 여왕벌 하이브 존재 시 비활성화
- 하이브 생성 시 GameObject.SetActive(false)
- 하이브 파괴 시 GameObject.SetActive(true)
- 완전한 비활성화/재활성화

---

## ?? 수정된 파일

### 1. UnitController.cs
```csharp
? queuedDestination 추가
   - 큐잉된 목적지 저장
   
? SetPath() 수정
   - 이동 중이면 목적지만 저장
   
? ProcessQueuedDestination() 추가
   - 이동 완료 후 새 경로 탐색
   
? IsMoving() 수정
   - 큐잉된 목적지도 체크
```

### 2. Hive.cs
```csharp
? Initialize() 수정
   - queenBee.gameObject.SetActive(false)
   
? DestroyHive() 수정
   - queenBee.gameObject.SetActive(true)
```

---

## ?? 시스템 작동 방식

### 1. 경로 큐잉 시스템

#### 이전 방식

```
[첫 번째 클릭]
플레이어 클릭 → 타일 A
   ↓
SetPath(경로 A)
{
    pathQueue = [A1, A2, A3]
}
   ↓
[이동 시작]
   ↓
[두 번째 클릭 (이동 중)]
플레이어 클릭 → 타일 B
   ↓
SetPath(경로 B) ?
{
    pathQueue.Clear()
    pathQueue = [B1, B2, B3] // 현재 위치에서 계산됨 ?
}
   ↓
[문제]
현재 이동 중인 위치에서 B로 가는 경로 계산 ?
부자연스러운 이동 ?
```

---

#### 새로운 방식

```
[첫 번째 클릭]
플레이어 클릭 → 타일 A
   ↓
SetPath(경로 A)
{
    if (isMoving || pathQueue.Count > 0) ?
    {
        // 큐잉만 하고 return ?
    }
    
    pathQueue = [A1, A2, A3]
    queuedDestination = null
}
   ↓
[이동 시작]
A1 → A2 → A3 이동 중...
   ↓
[두 번째 클릭 (이동 중)]
플레이어 클릭 → 타일 B
   ↓
SetPath(경로 B) ?
{
    if (isMoving || pathQueue.Count > 0) ?
    {
        queuedDestination = B ?
        Debug.Log("경로 큐잉") ?
        return ?
    }
}
   ↓
[첫 번째 이동 완료]
A3 도착
   ↓
[Update()] ?
{
    if (!isMoving && queuedDestination != null) ?
    {
        ProcessQueuedDestination() ?
    }
}
   ↓
[ProcessQueuedDestination()] ?
{
    startTile = GetTile(agent.q, agent.r) // A3 ?
    path = FindPath(A3, B) ?
    
    pathQueue = [B1, B2, B3] ? (A3에서 시작)
    queuedDestination = null
}
   ↓
[두 번째 이동 시작]
A3 → B1 → B2 → B3 이동 ?
   ↓
[결과]
자연스러운 연속 이동! ?
```

---

### 2. 여왕벌 활성화/비활성화 시스템

#### 이전 방식

```
[하이브 생성]
Hive.Initialize()
{
    queenBee.canMove = false
    
    // 렌더러만 비활성화 ?
    queenRenderer.enabled = false
    queenSprite.enabled = false
    queenCollider.enabled = false
}
   ↓
[문제점]
1. GameObject는 여전히 활성화 ?
2. 다른 컴포넌트들 계속 작동 ?
3. Update() 등 호출됨 ?
4. 메모리 낭비 ?
```

---

#### 새로운 방식

```
[하이브 생성]
Hive.Initialize()
{
    queenBee.SetPosition(q, r)
    queenBee.transform.position = hivePos
    queenBee.canMove = false
    
    // GameObject 자체를 비활성화 ?
    queenBee.gameObject.SetActive(false) ?
    
    Debug.Log("여왕벌 비활성화") ?
}
   ↓
[하이브 존재하는 동안]
여왕벌 GameObject 비활성화 상태 ?
- 렌더링 안 됨 ?
- 컴포넌트 작동 안 함 ?
- Update() 호출 안 됨 ?
- 메모리 절약 ?
   ↓
[하이브 파괴]
Hive.DestroyHive()
{
    queenBee.SetPosition(q, r)
    queenBee.transform.position = hivePos
    queenBee.canMove = true
    
    // GameObject 재활성화 ?
    queenBee.gameObject.SetActive(true) ?
    
    Debug.Log("여왕벌 활성화 완료") ?
}
   ↓
[하이브 파괴 후]
여왕벌 GameObject 활성화 상태 ?
- 렌더링 됨 ?
- 컴포넌트 작동 ?
- Update() 호출됨 ?
- 정상 작동 ?
   ↓
[결과]
완전한 비활성화/재활성화! ?
```

---

## ?? 핵심 코드

### 1. UnitController - 경로 큐잉

```csharp
// UnitController.cs
public class UnitController : MonoBehaviour
{
    private Queue<HexTile> pathQueue = new Queue<HexTile>();
    private bool isMoving = false;
    private Coroutine moveCoroutine;
    
    // 경로 큐잉을 위한 목적지 저장 ?
    private HexTile queuedDestination = null;

    void Update()
    {
        if (!isMoving && pathQueue.Count > 0)
        {
            var next = pathQueue.Dequeue();
            moveCoroutine = StartCoroutine(MoveToTileCoroutine(next));
        }
        else if (!isMoving && queuedDestination != null) ?
        {
            // 이동이 끝나고 큐잉된 목적지가 있으면 새로운 경로 탐색 ?
            ProcessQueuedDestination();
        }
    }

    /// <summary>
    /// 경로 설정 (연속 클릭 시 큐잉) ?
    /// </summary>
    public void SetPath(List<HexTile> path)
    {
        if (path == null || path.Count == 0) return;
        
        // 이미 이동 중이거나 대기 중인 경로가 있으면 큐잉 ?
        if (isMoving || pathQueue.Count > 0)
        {
            // 마지막 목적지 저장 ?
            queuedDestination = path[path.Count - 1];
            Debug.Log($"경로 큐잉: ({queuedDestination.q}, {queuedDestination.r})");
            return; ?
        }
        
        // 새로운 경로 설정
        pathQueue.Clear();
        queuedDestination = null;
        
        // ...existing code...
    }

    /// <summary>
    /// 큐잉된 목적지 처리 ?
    /// </summary>
    void ProcessQueuedDestination()
    {
        if (queuedDestination == null || agent == null) return;
        
        // 현재 위치에서 목적지로 경로 탐색 ?
        var startTile = TileManager.Instance?.GetTile(agent.q, agent.r);
        if (startTile == null)
        {
            queuedDestination = null;
            return;
        }
        
        var path = Pathfinder.FindPath(startTile, queuedDestination); ?
        if (path != null && path.Count > 0)
        {
            Debug.Log($"큐잉된 경로 실행: ({agent.q}, {agent.r}) → ({queuedDestination.q}, {queuedDestination.r})");
            
            // 경로 설정 ?
            pathQueue.Clear();
            int startIndex = (path[0].q == agent.q && path[0].r == agent.r) ? 1 : 0;
            for (int i = startIndex; i < path.Count; i++)
            {
                pathQueue.Enqueue(path[i]);
            }
        }
        
        queuedDestination = null;
    }

    public bool IsMoving()
    {
        return isMoving || pathQueue.Count > 0 || queuedDestination != null; // 큐잉된 목적지도 체크 ?
    }

    public void ClearPath()
    {
        pathQueue.Clear();
        queuedDestination = null; // 큐잉된 목적지도 초기화 ?
        // ...existing code...
    }
}
```

---

### 2. Hive - 여왕벌 활성화/비활성화

```csharp
// Hive.cs
public class Hive : MonoBehaviour
{
    public UnitAgent queenBee;
    
    public void Initialize(int q, int r)
    {
        this.q = q; 
        this.r = r;
        
        // 여왕벌 비활성화 (하이브 안으로 들어감) ?
        if (queenBee != null)
        {
            // 여왕벌을 하이브 위치로 이동
            queenBee.SetPosition(q, r);
            Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
            queenBee.transform.position = hivePos;
            
            // 이동 불가능하게
            queenBee.canMove = false;
            
            // GameObject 자체를 비활성화 ?
            queenBee.gameObject.SetActive(false);
            
            if (showDebugLogs)
                Debug.Log("[하이브 초기화] 여왕벌 비활성화");
        }
        
        // ...existing code...
    }

    void DestroyHive()
    {
        Debug.Log("[하이브 파괴] 하이브가 파괴됩니다.");
        
        // 여왕벌 재활성화 (하이브 위치에서) ?
        if (queenBee != null)
        {
            // 여왕벌을 하이브 위치에 배치
            queenBee.SetPosition(q, r);
            Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
            queenBee.transform.position = hivePos;
            
            // 이동 가능하게
            queenBee.canMove = true;
            
            // GameObject 재활성화 ?
            queenBee.gameObject.SetActive(true);
            
            Debug.Log("[하이브 파괴] 여왕벌 활성화 완료");
        }
        
        // ...existing code...
    }
}
```

---

## ?? 비교표

### 경로 큐잉

| 항목 | 이전 | 현재 |
|------|------|------|
| **이동 중 재클릭** | 경로 덮어씀 ? | 목적지 저장 ? |
| **경로 재계산** | 현재 위치 기준 ? | 도착지 기준 ? |
| **연속 이동** | 부자연스러움 ? | 자연스러움 ? |
| **로그** | 없음 ? | 상세 ? |

### 여왕벌 활성화

| 항목 | 이전 | 현재 |
|------|------|------|
| **비활성화 방식** | 렌더러만 ? | GameObject ? |
| **컴포넌트 작동** | 계속 작동 ? | 완전 중지 ? |
| **메모리** | 낭비 ? | 절약 ? |
| **재활성화** | 수동 ? | 자동 ? |

---

## ?? 시각화

### 경로 큐잉 흐름

```
[플레이어 행동]
클릭 A → 이동 시작 → (이동 중) 클릭 B
   ↓
[SetPath(A)]
pathQueue = [A1, A2, A3]
queuedDestination = null
   ↓
[이동 중...]
현재 위치 → A1 → A2 → ...
   ↓
[SetPath(B)] (이동 중)
if (isMoving) ?
{
    queuedDestination = B ?
    return
}
   ↓
[A3 도착]
isMoving = false
   ↓
[Update()]
if (!isMoving && queuedDestination != null) ?
{
    ProcessQueuedDestination()
    {
        path = FindPath(A3, B) ?
        pathQueue = [B1, B2, B3]
    }
}
   ↓
[B로 이동 시작]
A3 → B1 → B2 → B3 ?
```

### 여왕벌 상태 변화

```
[게임 시작]
여왕벌 GameObject.activeInHierarchy = true ?
   ↓
[하이브 건설]
Hive.Initialize()
{
    queenBee.gameObject.SetActive(false) ?
}
   ↓
[하이브 존재]
여왕벌 GameObject.activeInHierarchy = false ?
- 렌더링 안 됨
- 컴포넌트 작동 안 함
- Update() 호출 안 됨
   ↓
[하이브 파괴]
Hive.DestroyHive()
{
    queenBee.gameObject.SetActive(true) ?
}
   ↓
[하이브 파괴 후]
여왕벌 GameObject.activeInHierarchy = true ?
- 렌더링 됨
- 컴포넌트 작동
- Update() 호출됨
```

---

## ?? 문제 해결

### Q: 연속 클릭해도 큐잉이 안 돼요

**확인:**
```
[ ] Console에 "경로 큐잉" 로그
[ ] IsMoving() == true
[ ] queuedDestination != null
```

**해결:**
```
1. Console 로그 확인
   - "[UnitController] 경로 큐잉: (5, 3)"
2. IsMoving() 확인
   - isMoving == true 또는
   - pathQueue.Count > 0
3. 안 되면 SetPath() 로직 확인
```

---

### Q: 큐잉된 경로가 실행 안 돼요

**확인:**
```
[ ] Console에 "큐잉된 경로 실행" 로그
[ ] ProcessQueuedDestination() 호출
[ ] FindPath() 성공
```

**해결:**
```
1. Console 로그 확인
   - "[UnitController] 큐잉된 경로 실행: (3, 3) → (5, 3)"
2. FindPath() 결과 확인
   - path != null
   - path.Count > 0
3. 안 되면 경로 탐색 가능한지 확인
```

---

### Q: 여왕벌이 여전히 보여요

**확인:**
```
[ ] queenBee != null
[ ] SetActive(false) 호출
[ ] GameObject.activeInHierarchy == false
```

**해결:**
```
1. Inspector에서 확인
   - 여왕벌 GameObject 체크박스 해제됨
   - activeInHierarchy = false
2. Console 로그 확인
   - "[하이브 초기화] 여왕벌 비활성화"
3. 안 되면 queenBee 참조 확인
```

---

## ?? 추가 개선 아이디어

### 1. 경로 큐 시각화

```csharp
// UnitController.cs
void OnDrawGizmos()
{
    if (queuedDestination != null && agent != null)
    {
        // 현재 위치
        Vector3 currentPos = TileHelper.HexToWorld(agent.q, agent.r, 0.5f);
        
        // 큐잉된 목적지
        Vector3 queuedPos = TileHelper.HexToWorld(
            queuedDestination.q, 
            queuedDestination.r, 
            0.5f
        );
        
        // 점선으로 표시 ?
        Gizmos.color = Color.cyan;
        DrawDashedLine(currentPos, queuedPos);
        
        // 목적지 마커
        Gizmos.DrawWireSphere(queuedPos, 0.3f);
    }
}
```

---

### 2. 여왕벌 이사 애니메이션

```csharp
// Hive.cs
IEnumerator AnimateQueenEnterHive()
{
    if (queenBee == null) yield break;
    
    Vector3 startPos = queenBee.transform.position;
    Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
    
    // 하이브로 이동 애니메이션 ?
    float duration = 1f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        queenBee.transform.position = Vector3.Lerp(startPos, hivePos, t);
        
        // 크기 축소 ?
        queenBee.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
        
        yield return null;
    }
    
    // 비활성화
    queenBee.gameObject.SetActive(false);
}
```

---

### 3. 경로 취소 단축키

```csharp
// UnitController.cs
void Update()
{
    // ESC 키로 경로 취소 ?
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        if (queuedDestination != null || pathQueue.Count > 0)
        {
            ClearPath();
            Debug.Log("[UnitController] 경로 취소됨");
        }
    }
    
    // ...existing code...
}
```

---

## ? 테스트 체크리스트

```
[ ] 유닛 클릭
[ ] 타일 A 클릭
[ ] 유닛 이동 시작 ?
[ ] (이동 중) 타일 B 클릭
[ ] Console에 "경로 큐잉" 로그 ?
[ ] A 도착
[ ] Console에 "큐잉된 경로 실행" 로그 ?
[ ] B로 이동 시작 ?
[ ] B 도착 ?
[ ] 하이브 건설
[ ] 여왕벌 사라짐 ?
[ ] Console에 "여왕벌 비활성화" 로그 ?
[ ] 하이브 파괴
[ ] 여왕벌 다시 나타남 ?
[ ] Console에 "여왕벌 활성화 완료" 로그 ?
```

---

## ?? 완료!

**핵심 개선:**
- ? 경로 큐잉 시스템
- ? 연속 이동 명령 지원
- ? 여왕벌 GameObject 비활성화/재활성화
- ? 자연스러운 게임 플레이

**결과:**
- 부드러운 연속 이동
- 메모리 효율 향상
- 명확한 게임 로직
- 완벽한 UX

게임 개발 화이팅! ?????????
