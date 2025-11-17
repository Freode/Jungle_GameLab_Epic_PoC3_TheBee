# ?? 하이브 건설 시 여왕벌 비활성화 수정 가이드

## 완료된 개선사항

### ? ConstructHiveHandler 수정
- Hive.Initialize() 전에 여왕벌 참조 설정
- Initialize()에서 여왕벌 GameObject 비활성화
- 중복 렌더러 비활성화 코드 제거
- 일꾼 할당 로직 개선

---

## ?? 수정된 파일

### ConstructHiveHandler.cs
```csharp
? ExecuteConstruct() 수정
   1. hive.queenBee = agent 설정 (Initialize 전!)
   2. hive.Initialize(q, r) 호출
      → 내부에서 queenBee.gameObject.SetActive(false)
   3. 중복 렌더러 비활성화 코드 제거
   4. 일꾼 할당 로직 간소화
```

---

## ?? 시스템 작동 방식

### 이전 방식의 문제

```
[하이브 건설 명령]
ConstructHiveHandler.ExecuteConstruct()
   ↓
[1. 하이브 생성]
Instantiate(hivePrefab)
   ↓
[2. Initialize 호출]
hive.Initialize(q, r)
{
    // queenBee가 null! ?
    if (queenBee != null)
    {
        queenBee.gameObject.SetActive(false)
    }
}
   ↓
[3. 여왕벌 참조 설정]
hive.queenBee = agent ? (너무 늦음)
   ↓
[4. 렌더러 비활성화 시도]
queenRenderer.enabled = false ?
queenSprite.enabled = false ?
queenCollider.enabled = false ?
   ↓
[문제점]
1. Initialize() 시점에 queenBee == null ?
2. GameObject는 활성화 상태 ?
3. 렌더러만 비활성화 (불완전) ?
4. Update() 등 계속 실행됨 ?
   ↓
[결과]
여왕벌이 비활성화 안 됨! ?
```

---

### 새로운 방식

```
[하이브 건설 명령]
ConstructHiveHandler.ExecuteConstruct()
   ↓
[1. 하이브 생성]
Instantiate(hivePrefab)
hive = go.GetComponent<Hive>()
   ↓
[2. 여왕벌 참조 설정 (Initialize 전!)] ?
if (agent.isQueen)
{
    agent.homeHive = hive ?
    hive.queenBee = agent ?
    
    Debug.Log("여왕벌 참조 설정 완료") ?
}
   ↓
[3. Initialize 호출]
hive.Initialize(q, r)
{
    // queenBee가 이미 설정됨! ?
    if (queenBee != null)
    {
        // 1. 위치 설정
        queenBee.SetPosition(q, r)
        queenBee.transform.position = hivePos
        
        // 2. 이동 불가
        queenBee.canMove = false
        
        // 3. GameObject 비활성화 ?
        queenBee.gameObject.SetActive(false)
        
        Debug.Log("여왕벌 비활성화") ?
    }
}
   ↓
[4. 일꾼 할당]
AssignHomelessWorkersToHive(hive, q, r)
   ↓
[결과]
여왕벌 완전 비활성화! ?
GameObject.activeInHierarchy = false ?
렌더링 안 됨 ?
컴포넌트 작동 안 함 ?
Update() 호출 안 됨 ?
```

---

## ?? 핵심 코드

### ConstructHiveHandler.ExecuteConstruct() 수정

```csharp
// ConstructHiveHandler.cs
public static void ExecuteConstruct(UnitAgent agent, CommandTarget target)
{
    if (agent == null) return;

    // ...기존 코드 (하이브 생성)...
    
    Vector3 pos = TileHelper.HexToWorld(q, r, gm.hexSize);
    var go = GameObject.Instantiate(hivePrefab, pos, Quaternion.identity);

    // Hive 컴포넌트
    Hive hive = go.GetComponent<Hive>();
    if (hive == null) hive = go.AddComponent<Hive>();
    
    // UnitAgent 설정
    var hiveAgent = go.GetComponent<UnitAgent>();
    if (hiveAgent == null) hiveAgent = go.AddComponent<UnitAgent>();
    
    hiveAgent.q = q;
    hiveAgent.r = r;
    hiveAgent.canMove = false;
    hiveAgent.faction = Faction.Player;
    hiveAgent.SetPosition(q, r);
    
    // 여왕벌 참조 설정 (Initialize 전에!) ?
    if (agent.isQueen)
    {
        agent.homeHive = hive;
        hive.queenBee = agent; ?
        
        Debug.Log($"[하이브 건설] 여왕벌 참조 설정 완료");
    }
    
    // Initialize 호출 (여왕벌 비활성화 포함) ?
    hive.Initialize(q, r);
    
    // 일꾼 할당
    if (agent.isQueen)
    {
        AssignHomelessWorkersToHive(hive, q, r);
    }
    
    Debug.Log($"[하이브 건설] 하이브 건설 완료: ({q}, {r})");
}
```

---

### Hive.Initialize() (참고)

```csharp
// Hive.cs
public void Initialize(int q, int r)
{
    this.q = q; 
    this.r = r;
    
    // 여왕벌 비활성화 (하이브 안으로 들어감) ?
    if (queenBee != null) ?
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
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **여왕벌 참조** | Initialize 후 ? | Initialize 전 ? |
| **비활성화 방식** | 렌더러만 ? | GameObject ? |
| **Initialize 시점** | queenBee == null ? | queenBee != null ? |
| **중복 코드** | 있음 ? | 없음 ? |

---

## ?? 시각화

### 실행 순서

```
[하이브 건설 시작]
ExecuteConstruct(queen, target)
   ↓
[하이브 Instantiate]
go = Instantiate(hivePrefab)
hive = go.GetComponent<Hive>()
   ↓
[여왕벌 참조 설정] ?
agent.homeHive = hive
hive.queenBee = agent ?
Debug.Log("여왕벌 참조 설정 완료")
   ↓
[Initialize 호출]
hive.Initialize(q, r)
{
    if (queenBee != null) ?
    {
        queenBee.SetPosition(q, r)
        queenBee.transform.position = hivePos
        queenBee.canMove = false
        
        queenBee.gameObject.SetActive(false) ?
        
        Debug.Log("여왕벌 비활성화")
    }
}
   ↓
[일꾼 할당]
AssignHomelessWorkersToHive(hive, q, r)
   ↓
[결과]
여왕벌 GameObject.activeInHierarchy = false ?
하이브 건설 완료 ?
```

---

## ?? 문제 해결

### Q: 여왕벌이 여전히 보여요

**확인:**
```
[ ] hive.queenBee != null
[ ] Initialize() 전에 설정됨
[ ] Console에 "여왕벌 참조 설정 완료"
[ ] Console에 "여왕벌 비활성화"
```

**해결:**
```
1. Console 로그 확인
   - "[하이브 건설] 여왕벌 참조 설정 완료"
   - "[하이브 초기화] 여왕벌 비활성화"

2. 로그가 없으면
   - ConstructHiveHandler.ExecuteConstruct() 확인
   - agent.isQueen == true 확인
   - hive.queenBee 설정 확인

3. Unity Inspector 확인
   - 여왕벌 GameObject 체크박스 해제됨
   - activeInHierarchy = false
```

---

### Q: Initialize에서 queenBee가 null이에요

**확인:**
```
[ ] ExecuteConstruct()에서 hive.queenBee 설정
[ ] Initialize() 전에 설정
[ ] agent.isQueen == true
```

**해결:**
```
1. ExecuteConstruct() 코드 확인
   if (agent.isQueen)
   {
       hive.queenBee = agent; ? (Initialize 전!)
   }

2. 순서 확인
   a. hive.queenBee = agent
   b. hive.Initialize(q, r)
   
3. agent.isQueen 확인
   - Inspector에서 isQueen = true
```

---

### Q: 렌더러는 꺼지는데 GameObject는 활성화돼요

**확인:**
```
[ ] Initialize()에서 SetActive(false) 호출
[ ] ConstructHiveHandler에 중복 코드 없음
[ ] GameObject.activeInHierarchy 확인
```

**해결:**
```
1. Hive.Initialize() 확인
   queenBee.gameObject.SetActive(false); ?

2. ConstructHiveHandler 확인
   - 렌더러 비활성화 코드 제거됨 ?
   - SetActive(false)만 Initialize()에서 실행

3. Inspector 확인
   - 여왕벌 GameObject 완전 비활성화
```

---

## ?? 추가 개선 아이디어

### 1. 하이브 건설 애니메이션

```csharp
// ConstructHiveHandler.cs
public static void ExecuteConstruct(UnitAgent agent, CommandTarget target)
{
    // ...existing code...
    
    if (agent.isQueen)
    {
        // 여왕벌 → 하이브 변신 애니메이션 ?
        StartCoroutine(AnimateQueenToHive(agent, hive, q, r));
    }
    else
    {
        hive.Initialize(q, r);
    }
}

static IEnumerator AnimateQueenToHive(UnitAgent queen, Hive hive, int q, int r)
{
    // 1. 여왕벌 크기 축소
    Vector3 originalScale = queen.transform.localScale;
    float duration = 1f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        queen.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
        yield return null;
    }
    
    // 2. Initialize (여왕벌 비활성화)
    hive.Initialize(q, r);
    
    // 3. 하이브 크기 증가
    hive.transform.localScale = Vector3.zero;
    elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        hive.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
        yield return null;
    }
    
    hive.transform.localScale = Vector3.one;
}
```

---

### 2. 건설 이펙트

```csharp
// ConstructHiveHandler.cs
[Header("Effects")]
public GameObject constructEffect;
public AudioClip constructSound;

public static void ExecuteConstruct(UnitAgent agent, CommandTarget target)
{
    // ...existing code...
    
    // 건설 이펙트 ?
    if (constructEffect != null)
    {
        Vector3 pos = TileHelper.HexToWorld(q, r, gm.hexSize);
        var effect = Instantiate(constructEffect, pos, Quaternion.identity);
        Destroy(effect, 2f);
    }
    
    // 건설 사운드 ?
    if (constructSound != null)
    {
        AudioSource.PlayClipAtPoint(constructSound, pos);
    }
    
    hive.Initialize(q, r);
}
```

---

### 3. 일꾼 알림

```csharp
// ConstructHiveHandler.cs
private static void AssignHomelessWorkersToHive(Hive hive, int q, int r)
{
    if (TileManager.Instance == null) return;

    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit == null) continue;
        if (unit.faction != Faction.Player) continue;
        if (unit.isQueen) continue;
        
        if (unit.homeHive == null)
        {
            unit.homeHive = hive;
            unit.isFollowingQueen = false;
            unit.hasManualOrder = false;
            
            // 알림 표시 ?
            if (NotificationUI.Instance != null)
            {
                NotificationUI.Instance.ShowWorkerNotification(
                    unit,
                    "새 하이브가 건설되었습니다!",
                    3f
                );
            }
            
            // 하이브로 이동
            var start = TileManager.Instance.GetTile(unit.q, unit.r);
            var dest = TileManager.Instance.GetTile(q, r);
            if (start != null && dest != null)
            {
                var path = Pathfinder.FindPath(start, dest);
                if (path != null && path.Count > 0)
                {
                    var ctrl = unit.GetComponent<UnitController>();
                    if (ctrl == null) ctrl = unit.gameObject.AddComponent<UnitController>();
                    ctrl.agent = unit;
                    ctrl.SetPath(path);
                }
            }
            
            Debug.Log($"[하이브 건설] {unit.name}이(가) 새 하이브에 할당됨");
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 여왕벌 선택
[ ] 하이브 건설 명령
[ ] Console: "여왕벌 참조 설정 완료" ?
[ ] Console: "[하이브 초기화] 여왕벌 비활성화" ?
[ ] 여왕벌 사라짐 ?
[ ] Inspector: queenBee GameObject 비활성화 ?
[ ] Inspector: activeInHierarchy = false ?
[ ] 하이브 나타남 ?
[ ] 일꾼들이 하이브로 이동 ?
[ ] 하이브에서 일꾼 생성 시작 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? Initialize 전에 여왕벌 참조 설정
- ? Initialize에서 GameObject 비활성화
- ? 중복 렌더러 비활성화 코드 제거
- ? 명확한 실행 순서

**결과:**
- 여왕벌 완전 비활성화
- GameObject.activeInHierarchy = false
- 렌더링 안 됨
- 컴포넌트 작동 안 함

**게임 플레이:**
- 하이브 건설 시 여왕벌 사라짐
- 하이브 나타남
- 일꾼들 자동 할당
- 자연스러운 게임 흐름

게임 개발 화이팅! ???????
