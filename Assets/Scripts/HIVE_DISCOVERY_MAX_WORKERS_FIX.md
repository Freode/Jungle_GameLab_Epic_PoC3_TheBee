# ?? 적 말벌집 발견 & 최대 일꾼 수 업그레이드 버그 수정 가이드

## 완료된 개선사항

### 1. ? 적 말벌집 발견 시 렌더링 활성화
- EnemyHive GameObject 직접 체크
- SetEnemyHiveVisibility() 추가
- 모든 렌더러 강제 활성화
- 발견 즉시 렌더링

### 2. ? 최대 일꾼 수 업그레이드 후 생성 문제 해결
- SpawnLoop에서 실시간 maxWorkers 업데이트
- HiveManager.GetMaxWorkers() 주기적 호출
- 업그레이드 즉시 반영

---

## ?? 수정된 파일

### 1. EnemyVisibilityController.cs
```csharp
? UpdateEnemyVisibility() 강화
   - EnemyHive GameObject 직접 체크
   - SetEnemyHiveVisibility() 호출
   - 발견 즉시 렌더링 활성화
```

### 2. Hive.cs
```csharp
? SpawnLoop() 수정
   - 실시간 maxWorkers 업데이트
   - HiveManager.GetMaxWorkers() 주기적 호출
   - 업그레이드 즉시 반영
```

---

## ?? 시스템 작동 방식

### 1. 적 말벌집 발견 시스템

#### 문제 원인

```
[플레이어 시야 진입]
?? → → → (시야)
   ↓
[EnemyHive 위치]
??? (적 말벌집)
   ↓
[UpdateEnemyVisibility()]
- UnitAgent만 체크 ?
- EnemyHive GameObject 미체크 ?
   ↓
[결과]
렌더러 활성화 안 됨 ?
말벌집 보이지 않음 ?
```

---

#### 해결 방법

```
[플레이어 시야 진입]
?? → → → (시야)
   ↓
[EnemyHive 위치]
??? (적 말벌집)
   ↓
[UpdateEnemyVisibility()]
1. UnitAgent 체크 ?
2. EnemyHive GameObject 직접 체크 ?
   
   var allEnemyHives = FindObjectsOfType<EnemyHive>()
   foreach (var enemyHive in allEnemyHives)
   {
       if (isVisible)
       {
           discoveredHives.Add(enemyHive.gameObject) ?
       }
       
       SetEnemyHiveVisibility(enemyHive, shouldBeVisible) ?
   }
   ↓
[SetEnemyHiveVisibility()]
- SpriteRenderer 활성화 ?
- Renderer 활성화 ?
- 자식 Renderer 활성화 ?
- 부모 Renderer 활성화 ?
   ↓
[결과]
??? 보임! ?
```

---

### 2. 최대 일꾼 수 업그레이드 문제

#### 문제 원인

```
[게임 시작]
maxWorkers = 10 (기본값)
   ↓
[일꾼 생성]
SpawnLoop()
{
    if (workers.Count < maxWorkers) // 10
        SpawnWorker()
}
   ↓
workers.Count = 10 (최대 도달)
   ↓
[업그레이드 수행]
HiveManager.maxWorkers = 15 ?
   ↓
[SpawnLoop 계속 실행]
if (workers.Count < maxWorkers) // 10 ?
{
    // maxWorkers가 여전히 10 ?
    // 업그레이드 반영 안 됨 ?
}
   ↓
[결과]
일꾼 생성 안 됨 ?
```

---

#### 해결 방법

```
[게임 시작]
maxWorkers = 10 (기본값)
   ↓
[일꾼 생성]
SpawnLoop()
{
    // 실시간 maxWorkers 업데이트 ?
    int currentMaxWorkers = HiveManager.Instance.GetMaxWorkers()
    
    if (currentMaxWorkers != maxWorkers)
    {
        maxWorkers = currentMaxWorkers ?
        Debug.Log($"maxWorkers 업데이트: {maxWorkers}") ?
    }
    
    if (workers.Count < maxWorkers)
        SpawnWorker()
}
   ↓
workers.Count = 10 (최대 도달)
   ↓
[업그레이드 수행]
HiveManager.maxWorkers = 15 ?
   ↓
[다음 SpawnLoop 실행]
currentMaxWorkers = GetMaxWorkers() // 15 ?
maxWorkers = 15 ? (업데이트)
   ↓
if (10 < 15) // true ?
{
    SpawnWorker() ?
}
   ↓
[결과]
일꾼 생성 재개! ?
workers.Count = 11, 12, 13, 14, 15 ?
```

---

## ?? 핵심 코드

### 1. UpdateEnemyVisibility() 강화

```csharp
// EnemyVisibilityController.cs
void UpdateEnemyVisibility()
{
    CalculatePlayerVision();

    // 1. UnitAgent 체크 (기존)
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit.faction != Faction.Enemy) continue;
        
        var enemyHive = unit.GetComponent<EnemyHive>();
        bool isHive = (enemyHive != null);
        
        // ...
        
        SetUnitVisibility(unit, shouldBeVisible);
    }
    
    // 2. EnemyHive GameObject 직접 체크 (신규) ?
    var allEnemyHives = FindObjectsOfType<EnemyHive>();
    foreach (var enemyHive in allEnemyHives)
    {
        if (enemyHive == null) continue;
        
        // 위치 확인
        Vector2Int hivePos = new Vector2Int(enemyHive.q, enemyHive.r);
        bool isVisible = currentVisibleTiles.Contains(hivePos);
        
        if (isVisible)
        {
            // 처음 발견 ?
            if (!discoveredHives.Contains(enemyHive.gameObject))
            {
                discoveredHives.Add(enemyHive.gameObject);
                Debug.Log($"[시야] 적 말벌집 발견: {enemyHive.name}");
            }
        }
        
        // 발견된 하이브는 항상 보임
        bool shouldBeVisible = discoveredHives.Contains(enemyHive.gameObject);
        
        // EnemyHive GameObject의 렌더러 활성화 ?
        SetEnemyHiveVisibility(enemyHive, shouldBeVisible);
    }
}
```

---

### 2. SetEnemyHiveVisibility() 추가

```csharp
// EnemyVisibilityController.cs
/// <summary>
/// EnemyHive의 시각화 설정 ?
/// </summary>
void SetEnemyHiveVisibility(EnemyHive hive, bool visible)
{
    if (hive == null) return;
    
    GameObject targetObject = hive.gameObject;
    
    if (showDebugLogs)
        Debug.Log($"[시야 디버그] EnemyHive 렌더러 설정: {targetObject.name}, visible={visible}");

    int rendererCount = 0;

    // 1. 자신의 SpriteRenderer ?
    var sprite = targetObject.GetComponent<SpriteRenderer>();
    if (sprite != null)
    {
        sprite.enabled = visible;
        rendererCount++;
    }

    // 2. 자신의 Renderer ?
    var renderer = targetObject.GetComponent<Renderer>();
    if (renderer != null && renderer != sprite)
    {
        renderer.enabled = visible;
        rendererCount++;
    }

    // 3. 자식 GameObject들의 모든 Renderer ?
    var childRenderers = targetObject.GetComponentsInChildren<Renderer>(true);
    foreach (var r in childRenderers)
    {
        r.enabled = visible;
        rendererCount++;
    }

    // 4. 자식 GameObject들의 모든 SpriteRenderer ?
    var childSprites = targetObject.GetComponentsInChildren<SpriteRenderer>(true);
    foreach (var s in childSprites)
    {
        // 중복 방지
        bool alreadyProcessed = false;
        foreach (var r in childRenderers)
        {
            if (r == s)
            {
                alreadyProcessed = true;
                break;
            }
        }
        
        if (!alreadyProcessed)
        {
            s.enabled = visible;
            rendererCount++;
        }
    }
    
    // 5. 부모 GameObject 체크 ?
    if (targetObject.transform.parent != null)
    {
        var parentRenderer = targetObject.transform.parent.GetComponent<Renderer>();
        if (parentRenderer != null)
        {
            parentRenderer.enabled = visible;
            rendererCount++;
        }
    }
    
    // 렌더러가 하나도 없으면 경고
    if (rendererCount == 0)
    {
        Debug.LogWarning($"[시야 경고] EnemyHive {hive.name}에 렌더러를 찾을 수 없습니다!");
    }
    
    if (showDebugLogs)
        Debug.Log($"[시야 디버그] EnemyHive 총 {rendererCount}개 렌더러 처리 완료");
}
```

---

### 3. SpawnLoop() 수정

```csharp
// Hive.cs
IEnumerator SpawnLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(spawnInterval);
        
        // 일꾼 리스트 정리
        workers.RemoveAll(w => w == null);
        
        // 실시간으로 HiveManager에서 최대 일꾼 수 가져오기 ?
        if (HiveManager.Instance != null)
        {
            int currentMaxWorkers = HiveManager.Instance.GetMaxWorkers();
            
            // maxWorkers 업데이트 (업그레이드 반영) ?
            if (currentMaxWorkers != maxWorkers)
            {
                if (showDebugLogs)
                    Debug.Log($"[하이브] maxWorkers 업데이트: {maxWorkers} → {currentMaxWorkers}");
                
                maxWorkers = currentMaxWorkers; ?
            }
        }
        
        // MaxWorkers 체크
        if (!isRelocating && workers.Count < maxWorkers)
        {
            SpawnWorker(); ?
        }
        else if (workers.Count >= maxWorkers)
        {
            if (showDebugLogs)
                Debug.Log($"[하이브] 최대 일꾼 수 도달: {workers.Count}/{maxWorkers}");
        }
    }
}
```

---

## ?? 비교표

### 적 말벌집 발견

| 항목 | 이전 | 현재 |
|------|------|------|
| **UnitAgent 체크** | 있음 | 있음 ? |
| **EnemyHive 직접 체크** | 없음 ? | 있음 ? |
| **렌더러 활성화** | 부분적 ? | 완전 ? |
| **발견 로그** | 있음 | 강화 ? |

### 최대 일꾼 수 업그레이드

| 항목 | 이전 | 현재 |
|------|------|------|
| **maxWorkers 초기화** | Initialize() | Initialize() ? |
| **실시간 업데이트** | 없음 ? | 있음 ? |
| **업그레이드 반영** | 안 됨 ? | 즉시 반영 ? |
| **생성 재개** | 안 됨 ? | 자동 재개 ? |

---

## ?? 시각화

### 적 말벌집 발견 흐름

```
[플레이어 시야 진입]
?? → → → ???
   ↓
[UpdateEnemyVisibility()]
1. UnitAgent 체크 ?
2. FindObjectsOfType<EnemyHive>() ?
   ↓
[발견 체크]
isVisible = currentVisibleTiles.Contains(hivePos)
if (isVisible)
{
    discoveredHives.Add(enemyHive.gameObject) ?
}
   ↓
[렌더러 활성화]
SetEnemyHiveVisibility(enemyHive, true)
- SpriteRenderer.enabled = true ?
- Renderer.enabled = true ?
- 자식 Renderer.enabled = true ?
   ↓
[결과]
??? 보임! ?
```

### 최대 일꾼 수 업그레이드 흐름

```
[게임 진행]
workers.Count = 10
maxWorkers = 10
   ↓
[업그레이드 수행]
HiveManager.maxWorkers = 15 ?
   ↓
[SpawnLoop 실행]
currentMaxWorkers = GetMaxWorkers() // 15 ?
   ↓
[업데이트 체크]
if (15 != 10)
{
    maxWorkers = 15 ?
    Debug.Log("maxWorkers 업데이트: 10 → 15")
}
   ↓
[생성 체크]
if (10 < 15) // true ?
{
    SpawnWorker() ?
}
   ↓
[결과]
workers.Count = 11, 12, 13, 14, 15 ?
```

---

## ?? 문제 해결

### Q: 적 말벌집이 여전히 안 보여요

**확인:**
```
[ ] showDebugLogs = true 설정
[ ] Console에 "[시야] 적 말벌집 발견" 로그
[ ] "[시야 디버그] EnemyHive 렌더러 설정" 로그
[ ] rendererCount > 0
```

**해결:**
```
1. EnemyVisibilityController.showDebugLogs = true
2. Console 로그 확인
   - "적 말벌집 발견: EnemyHive at (x, y)"
   - "EnemyHive 렌더러 설정: EnemyHive, visible=true"
   - "총 N개 렌더러 처리 완료"
3. rendererCount가 0이면 렌더러 없음 경고
4. EnemyHive Prefab에 SpriteRenderer 확인
```

---

### Q: 업그레이드 후에도 일꾼이 생성 안 돼요

**확인:**
```
[ ] showDebugLogs = true 설정
[ ] Console에 "maxWorkers 업데이트" 로그
[ ] HiveManager.GetMaxWorkers() 반환값
[ ] workers.Count < maxWorkers
```

**해결:**
```
1. Hive.showDebugLogs = true
2. Console 로그 확인
   - "[하이브] maxWorkers 업데이트: 10 → 15"
3. HiveManager에서 업그레이드 확인
4. SpawnLoop가 실행 중인지 확인
```

---

### Q: maxWorkers 업데이트가 너무 느려요

**확인:**
```
[ ] spawnInterval 설정 (기본 10초)
[ ] SpawnLoop while 루프 실행
```

**해결:**
```
1. spawnInterval 값 줄이기 (예: 5초)
2. Update()에서 실시간 체크 추가 (선택)
```

---

## ?? 추가 개선 아이디어

### 1. 즉시 업데이트 메서드

```csharp
// Hive.cs
/// <summary>
/// 업그레이드 즉시 maxWorkers 업데이트 ?
/// </summary>
public void UpdateMaxWorkers()
{
    if (HiveManager.Instance != null)
    {
        int newMaxWorkers = HiveManager.Instance.GetMaxWorkers();
        
        if (newMaxWorkers != maxWorkers)
        {
            Debug.Log($"[하이브] maxWorkers 즉시 업데이트: {maxWorkers} → {newMaxWorkers}");
            maxWorkers = newMaxWorkers;
            
            // 즉시 일꾼 생성 시도
            if (workers.Count < maxWorkers && !isRelocating)
            {
                SpawnWorker();
            }
        }
    }
}

// UpgradeCommandHandler.cs
public static void ExecuteUpgrade(...)
{
    // 업그레이드 적용
    HiveManager.Instance.maxWorkers = newMaxWorkers;
    
    // 모든 하이브에 즉시 적용 ?
    foreach (var hive in HiveManager.Instance.GetAllHives())
    {
        if (hive != null)
        {
            hive.UpdateMaxWorkers();
        }
    }
}
```

---

### 2. EnemyHive 발견 이펙트

```csharp
// EnemyVisibilityController.cs
if (isVisible)
{
    if (!discoveredHives.Contains(enemyHive.gameObject))
    {
        discoveredHives.Add(enemyHive.gameObject);
        
        // 발견 이펙트 ?
        if (discoveryParticles != null)
        {
            var particles = Instantiate(discoveryParticles, 
                enemyHive.transform.position, 
                Quaternion.identity);
            Destroy(particles, 2f);
        }
        
        // 발견 사운드 ?
        if (discoverySound != null)
        {
            AudioSource.PlayClipAtPoint(discoverySound, 
                enemyHive.transform.position);
        }
        
        // UI 알림 ?
        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.ShowMessage(
                "적 말벌집을 발견했습니다!",
                3f
            );
        }
        
        Debug.Log($"[시야] 적 말벌집 발견: {enemyHive.name}");
    }
}
```

---

### 3. 실시간 maxWorkers UI 표시

```csharp
// HiveUI.cs
void Update()
{
    if (currentHive != null && HiveManager.Instance != null)
    {
        int currentMaxWorkers = HiveManager.Instance.GetMaxWorkers();
        
        // UI 업데이트
        if (workerCountText != null)
        {
            int workerCount = currentHive.GetWorkers().Count;
            workerCountText.text = $"일꾼: {workerCount}/{currentMaxWorkers}";
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] showDebugLogs = true 설정 ?
[ ] 플레이어 유닛 이동 → 적 말벌집 시야 진입
[ ] Console에 "적 말벌집 발견" 로그 ?
[ ] 말벌집이 보임 ?
[ ] 말벌집 렌더러 활성화 ?
[ ] 최대 일꾼 수 10 도달
[ ] 최대 일꾼 수 업그레이드 (10 → 15)
[ ] Console에 "maxWorkers 업데이트" 로그 ?
[ ] 일꾼 생성 재개 ?
[ ] workers.Count = 11, 12, 13, 14, 15 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? EnemyHive GameObject 직접 체크
- ? SetEnemyHiveVisibility() 추가
- ? SpawnLoop 실시간 maxWorkers 업데이트

**결과:**
- 적 말벌집 발견 즉시 렌더링
- 최대 일꾼 수 업그레이드 즉시 반영
- 안정적인 게임 플레이

**게임 플레이:**
- 적 말벌집 발견 → 보임
- 최대 일꾼 수 업그레이드 → 즉시 생성 재개
- 자연스러운 게임 흐름

게임 개발 화이팅! ????????
