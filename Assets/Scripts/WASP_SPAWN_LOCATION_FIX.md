# ?? 말벌 생성 위치 수정 가이드

## 완료된 개선사항

### ? 말벌이 말벌집 근처에서 생성되도록 수정
- hexSize 명시적 설정
- 타일 좌표 기반 거리 계산
- 위치 보정 로직 추가
- 디버그 로그 강화

---

## ?? 수정된 파일

### 1. EnemyHive.cs
```csharp
? SpawnWasp() 수정
   - hexSize 명시적 설정
   - 위치 보정 로직 추가
   - 디버그 로그 강화
```

### 2. EnemyAI.cs
```csharp
? FindNearestEnemyHive() 수정
   - 타일 좌표 기반 거리 계산
   - World 좌표 → Axial 좌표
   - 디버그 로그 추가
```

---

## ?? 시스템 작동 방식

### 문제 원인

```
[말벌 생성]
EnemyHive at (5, 3)
   ↓
SpawnWasp()
{
    Vector3 spawnPos = GetRandomPositionInTile(q, r, 0.5f, 0.15f)
    // spawnPos = (2.5, 0, 1.5) 정상 ?
    
    Instantiate(waspPrefab, spawnPos, ...)
    waspAgent.SetPosition(q, r) // (5, 3) 설정
}
   ↓
[EnemyAI.Start()]
FindNearestEnemyHive()
{
    Vector3 myPos = transform.position ?
    // myPos = (0, 0, 0) ← 문제!
    
    foreach (var hive in allHives)
    {
        Vector3 hivePos = HexToWorld(hive.q, hive.r, 0.5f)
        float distance = Vector3.Distance(myPos, hivePos) ?
        // (0,0,0)에서 거리 계산 ?
    }
}
   ↓
[결과]
말벌이 (0,0)에서 생성된 것처럼 보임 ?
```

---

### 해결 방법

```
[말벌 생성]
EnemyHive at (5, 3)
   ↓
SpawnWasp()
{
    Vector3 spawnPos = GetRandomPositionInTile(5, 3, 0.5f, 0.15f)
    // spawnPos = (2.5, 0, 1.5) ?
    
    Instantiate(waspPrefab, spawnPos, ...)
    
    waspAgent.hexSize = 0.5f ? (명시적 설정)
    waspAgent.SetPosition(5, 3) ?
    
    // 위치 보정 ?
    if (Distance > 1f)
    {
        transform.position = GetRandomPositionInTile(5, 3, ...)
    }
    
    if (showDebugLogs)
        Debug.Log($"말?벌 생성: ({5}, {3}) → World: {spawnPos}") ?
}
   ↓
[EnemyAI.Start()]
FindNearestEnemyHive()
{
    int myQ = agent.q // 5 ?
    int myR = agent.r // 3 ?
    
    foreach (var hive in allHives)
    {
        int distance = AxialDistance(myQ, myR, hive.q, hive.r) ?
        // AxialDistance(5, 3, 5, 3) = 0 ?
        
        if (showDebugLogs)
            Debug.Log($"하이브 거리: ({hive.q}, {hive.r}) → {distance}") ?
    }
    
    // nearest = 현재 하이브 (거리 0) ?
}
   ↓
[결과]
말벌이 말벌집(5, 3) 근처에서 생성 ?
```

---

## ?? 핵심 코드

### 1. SpawnWasp() 수정

```csharp
// EnemyHive.cs
void SpawnWasp()
{
    if (waspPrefab == null)
    {
        Debug.LogError("[적 하이브] 말벌 프리팹이 없습니다!");
        return;
    }

    // 타일 내부 랜덤 위치 ?
    Vector3 spawnPos = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.15f);

    if (showDebugLogs)
        Debug.Log($"[적 하이브] 말벌 생성 위치: ({q}, {r}) → World: {spawnPos}"); ?

    // 말벌 생성
    GameObject waspObj = Instantiate(waspPrefab, spawnPos, Quaternion.identity);
    var waspAgent = waspObj.GetComponent<UnitAgent>();
    
    if (waspAgent == null)
    {
        waspAgent = waspObj.AddComponent<UnitAgent>();
    }

    // UnitAgent 설정 ?
    waspAgent.hexSize = 0.5f; // hexSize 명시적 설정 ?
    waspAgent.SetPosition(q, r); // 타일 좌표 설정
    waspAgent.faction = Faction.Enemy;
    waspAgent.canMove = true;

    // 월드 위치 확인 및 보정 ?
    Vector3 expectedWorldPos = TileHelper.HexToWorld(q, r, 0.5f);
    if (Vector3.Distance(waspObj.transform.position, expectedWorldPos) > 1f)
    {
        Debug.LogWarning($"[적 하이브] 말벌 위치 보정: {waspObj.transform.position} → {expectedWorldPos}");
        waspObj.transform.position = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.15f); ?
    }

    // 말벌 리스트에 추가
    wasps.Add(waspAgent);

    // EnemyAI 설정
    var enemyAI = waspObj.GetComponent<EnemyAI>();
    if (enemyAI == null)
    {
        enemyAI = waspObj.AddComponent<EnemyAI>();
    }

    enemyAI.visionRange = 3;
    enemyAI.activityRange = activityRange;
    enemyAI.attackRange = 0;
    enemyAI.showDebugLogs = showDebugLogs; // 디버그 로그 동기화 ?

    if (showDebugLogs)
        Debug.Log($"[적 하이브] 말벌 생성 완료: {wasps.Count}/{maxWasps} at ({q}, {r})"); ?
}
```

---

### 2. FindNearestEnemyHive() 수정

```csharp
// EnemyAI.cs
/// <summary>
/// 가장 가까운 EnemyHive 찾기 ?
/// </summary>
EnemyHive FindNearestEnemyHive()
{
    var allHives = FindObjectsOfType<EnemyHive>();
    EnemyHive nearest = null;
    int minDistance = int.MaxValue; // 타일 거리로 변경 ?

    // 현재 말벌의 타일 좌표 사용 ?
    int myQ = agent.q;
    int myR = agent.r;

    if (showDebugLogs)
        Debug.Log($"[Enemy AI] {agent.name} 홈 하이브 검색 시작: 현재 위치 ({myQ}, {myR})");

    foreach (var hive in allHives)
    {
        if (hive == null) continue;

        // 타일 좌표 기반 거리 계산 ?
        int distance = Pathfinder.AxialDistance(myQ, myR, hive.q, hive.r);

        if (showDebugLogs)
            Debug.Log($"[Enemy AI] 하이브 거리 체크: ({hive.q}, {hive.r}) → 거리: {distance}");

        if (distance < minDistance)
        {
            minDistance = distance;
            nearest = hive;
        }
    }

    if (showDebugLogs && nearest != null)
        Debug.Log($"[Enemy AI] 가장 가까운 하이브: ({nearest.q}, {nearest.r}), 거리: {minDistance}");

    return nearest;
}
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **생성 위치** | (0, 0) 근처 ? | 말벌집 근처 ? |
| **hexSize 설정** | 없음 ? | 명시적 설정 ? |
| **거리 계산** | World 좌표 ? | Axial 좌표 ? |
| **위치 보정** | 없음 ? | 있음 ? |
| **디버그 로그** | 부족 ? | 강화 ? |

---

## ?? 시각화

### 말벌 생성 흐름

```
[말벌집 위치]
??? (5, 3)
   ↓
[SpawnWasp()]
spawnPos = GetRandomPositionInTile(5, 3, 0.5f, 0.15f)
spawnPos = (2.5, 0, 1.5) ?
   ↓
[Instantiate]
waspObj.position = (2.5, 0, 1.5) ?
   ↓
[UnitAgent 설정]
waspAgent.hexSize = 0.5f ?
waspAgent.SetPosition(5, 3) ?
   ↓
[위치 보정]
if (Distance > 1f)
{
    waspObj.position = GetRandomPositionInTile(5, 3, ...) ?
}
   ↓
[EnemyAI.Start()]
FindNearestEnemyHive()
myQ = 5, myR = 3 ?
distance = AxialDistance(5, 3, 5, 3) = 0 ?
homeHive = 현재 하이브 ?
   ↓
[결과]
?? 말벌이 ??? (5, 3) 근처에서 생성! ?
```

---

## ?? 문제 해결

### Q: 말벌이 여전히 (0,0)에서 생성돼요

**확인:**
```
[ ] showDebugLogs = true 설정
[ ] Console에 "말벌 생성 위치" 로그
[ ] "하이브 거리 체크" 로그
[ ] spawnPos 값 확인
```

**해결:**
```
1. EnemyHive.showDebugLogs = true
2. EnemyAI.showDebugLogs = true
3. Console 로그 확인
   - "[적 하이브] 말벌 생성 위치: (5, 3) → World: (2.5, 0, 1.5)"
   - "[Enemy AI] 홈 하이브 검색 시작: 현재 위치 (5, 3)"
   - "[Enemy AI] 하이브 거리 체크: (5, 3) → 거리: 0"
4. spawnPos가 (0, 0, 0)이면 TileHelper 문제
```

---

### Q: 말벌이 하이브에서 너무 멀리 생성돼요

**확인:**
```
[ ] GetRandomPositionInTile의 radius 파라미터
[ ] 0.15f → 0.1f로 줄이기
```

**해결:**
```csharp
// EnemyHive.cs
Vector3 spawnPos = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.1f); // 0.15f → 0.1f
```

---

### Q: 여러 말벌집이 있을 때 잘못된 하이브를 찾아요

**확인:**
```
[ ] FindNearestEnemyHive의 거리 계산
[ ] AxialDistance 사용 확인
[ ] 디버그 로그에서 거리 확인
```

**해결:**
```
1. Console 로그 확인
   - "[Enemy AI] 하이브 거리 체크: (5, 3) → 거리: 0"
   - "[Enemy AI] 하이브 거리 체크: (8, 7) → 거리: 5"
2. 가장 가까운 하이브가 선택되는지 확인
3. minDistance 값 확인
```

---

## ?? 추가 개선 아이디어

### 1. 말벌 생성 이펙트

```csharp
// EnemyHive.cs
void SpawnWasp()
{
    Vector3 spawnPos = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.15f);
    
    // 생성 이펙트 ?
    if (spawnParticles != null)
    {
        var particles = Instantiate(spawnParticles, spawnPos, Quaternion.identity);
        Destroy(particles, 2f);
    }
    
    // 생성 사운드 ?
    if (spawnSound != null)
    {
        AudioSource.PlayClipAtPoint(spawnSound, spawnPos);
    }
    
    GameObject waspObj = Instantiate(waspPrefab, spawnPos, Quaternion.identity);
    // ...
}
```

---

### 2. 말벌 생성 애니메이션

```csharp
// EnemyHive.cs
IEnumerator SpawnWaspWithAnimation(Vector3 spawnPos)
{
    GameObject waspObj = Instantiate(waspPrefab, spawnPos + Vector3.down * 0.5f, Quaternion.identity);
    
    // 위로 떠오르는 애니메이션 ?
    float duration = 0.5f;
    float elapsed = 0f;
    Vector3 startPos = waspObj.transform.position;
    Vector3 endPos = spawnPos;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        waspObj.transform.position = Vector3.Lerp(startPos, endPos, t);
        yield return null;
    }
    
    waspObj.transform.position = endPos;
}
```

---

### 3. 하이브 직접 참조

```csharp
// EnemyHive.cs
void SpawnWasp()
{
    // ...
    
    // EnemyAI에 하이브 직접 전달 ?
    var enemyAI = waspObj.GetComponent<EnemyAI>();
    if (enemyAI != null)
    {
        // private 필드 접근 (리플렉션 또는 public setter 추가)
        var homeHiveField = typeof(EnemyAI).GetField("homeHive", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (homeHiveField != null)
        {
            homeHiveField.SetValue(enemyAI, this);
            Debug.Log($"[적 하이브] 말벌에게 홈 하이브 직접 할당");
        }
    }
}

// 또는 EnemyAI에 public setter 추가
// EnemyAI.cs
public void SetHomeHive(EnemyHive hive)
{
    homeHive = hive;
}
```

---

## ? 테스트 체크리스트

```
[ ] showDebugLogs = true 설정 ?
[ ] 말벌집 생성
[ ] 말벌 자동 생성 대기
[ ] Console에 "말벌 생성 위치" 로그 ?
[ ] 말벌이 말벌집 근처에서 생성 ?
[ ] Console에 "홈 하이브 검색" 로그 ?
[ ] Console에 "하이브 거리 체크" 로그 ?
[ ] homeHive 올바르게 설정 ?
[ ] 말벌이 하이브 주변 순찰 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? hexSize 명시적 설정
- ? 타일 좌표 기반 거리 계산
- ? 위치 보정 로직 추가
- ? 디버그 로그 강화

**결과:**
- 말벌이 말벌집 근처에서 생성
- 정확한 홈 하이브 찾기
- 안정적인 AI 작동

**게임 플레이:**
- 말벌집 (5, 3) 생성
- 말벌이 (5, 3) 근처에서 생성
- 말벌이 (5, 3) 주변 순찰
- 자연스러운 게임 흐름

게임 개발 화이팅! ????????
