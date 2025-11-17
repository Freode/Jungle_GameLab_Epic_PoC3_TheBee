# ?? 하이브 일꾼 수 제한 및 UI 개선 가이드

## 완료된 개선사항

### 1. ? 말벌집 MaxWorkers 제한 수정
- SpawnLoop에서 null 제거 및 체크
- SpawnWorker에서 추가 안전장치
- 디버그 로그 추가

### 2. ? 하이브 선택 시 일꾼 수 표시
- UnitCommandPanel에서 하이브 일꾼 수 표시
- 여왕벌 선택 시 전체 일꾼 수
- 하이브 선택 시 해당 하이브 일꾼 수/최대 수

---

## ?? 수정된 파일

### 1. Hive.cs
```csharp
? showDebugLogs 필드 추가
? SpawnLoop() 수정
   - workers.RemoveAll(w => w == null) 추가
   - MaxWorkers 도달 로그 추가
   
? SpawnWorker() 수정
   - 추가 안전장치: workers.Count >= maxWorkers 체크
   - 스폰 로그 추가: "일꾼 생성: X/Y"
```

### 2. UnitCommandPanel.cs
```csharp
? UpdateUnitInfo() 수정
   - 하이브 선택 시 일꾼 수 표시
   - workerCountText: "일꾼 수: X/Y"
```

### 3. EnemyAI.cs
```csharp
? FindNearestPlayerUnit() 정리
   - 중복 변수 선언 제거
```

---

## ?? 시스템 작동 방식

### 1. 말벌집 MaxWorkers 제한

```
[SpawnLoop 시작]
   ↓
[10초 대기]
   ↓
[일꾼 리스트 정리] ?
workers.RemoveAll(w => w == null)
   ↓
[MaxWorkers 체크] ?
if (workers.Count < maxWorkers)
{
    SpawnWorker() ?
}
else
{
    Debug.Log("최대 일꾼 수 도달") ?
}
```

### 2. SpawnWorker 안전장치

```
[SpawnWorker 호출]
   ↓
[추가 체크] ?
if (workers.Count >= maxWorkers)
{
    Debug.LogWarning("이미 최대 일꾼 수")
    return ?
}
   ↓
[일꾼 생성]
Instantiate(workerPrefab, ...)
   ↓
[리스트 추가]
workers.Add(agent)
   ↓
[로그 출력] ?
Debug.Log($"일꾼 생성: {workers.Count}/{maxWorkers}")
```

### 3. 하이브 선택 시 일꾼 수 표시

```
[하이브 선택]
UnitCommandPanel.Show(hiveAgent)
   ↓
[UpdateUnitInfo 호출]
   ↓
[하이브 체크] ?
if (hive != null && faction == Player)
{
    int workerCount = hive.GetWorkers().Count
    workerCountText = "일꾼 수: X/Y" ?
}
   ↓
[UI 표시]
일꾼 수: 5/10 ?
```

### 4. 여왕벌 선택 시 전체 일꾼 수

```
[여왕벌 선택]
UnitCommandPanel.Show(queenAgent)
   ↓
[UpdateUnitInfo 호출]
   ↓
[여왕벌 체크] ?
if (faction == Player && isQueen)
{
    int workerCount = 전체 일꾼 카운트
    workerCountText = "일꾼 수: X" ?
}
   ↓
[UI 표시]
일꾼 수: 15 ?
```

---

## ?? 핵심 코드

### 1. SpawnLoop null 제거

```csharp
// Hive.cs
IEnumerator SpawnLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(spawnInterval);
        
        // null 제거 ?
        workers.RemoveAll(w => w == null);
        
        // MaxWorkers 체크 ?
        if (!isRelocating && workers.Count < maxWorkers)
        {
            SpawnWorker();
        }
        else if (workers.Count >= maxWorkers)
        {
            if (showDebugLogs)
                Debug.Log($"[하이브] 최대 일꾼 수 도달: {workers.Count}/{maxWorkers}");
        }
    }
}
```

### 2. SpawnWorker 안전장치

```csharp
// Hive.cs
void SpawnWorker()
{
    if (workerPrefab == null) return;
    
    // 추가 안전장치 ?
    if (workers.Count >= maxWorkers)
    {
        Debug.LogWarning($"[하이브] 이미 최대 일꾼 수: {workers.Count}/{maxWorkers}");
        return;
    }
    
    // 생성...
    workers.Add(agent);
    
    Debug.Log($"[하이브 스폰] 일꾼 생성: {workers.Count}/{maxWorkers}"); ?
}
```

### 3. 하이브 일꾼 수 표시

```csharp
// UnitCommandPanel.cs - UpdateUnitInfo()
var hive = currentAgent.GetComponent<Hive>();

if (workerCountText != null)
{
    // 플레이어 하이브 ?
    if (hive != null && currentAgent.faction == Faction.Player)
    {
        int workerCount = hive.GetWorkers().Count;
        workerCountText.text = $"일꾼 수: {workerCount}/{hive.maxWorkers}"; ?
    }
    // 여왕벌 ?
    else if (currentAgent.faction == Faction.Player && currentAgent.isQueen)
    {
        int workerCount = 전체_일꾼_카운트();
        workerCountText.text = $"일꾼 수: {workerCount}"; ?
    }
    else
    {
        workerCountText.text = ""; // 일반 유닛
    }
}
```

---

## ?? 비교표

| 기능 | 이전 | 현재 |
|------|------|------|
| **MaxWorkers 체크** | SpawnLoop만 | SpawnLoop + SpawnWorker ? |
| **null 제거** | 없음 | RemoveAll ? |
| **디버그 로그** | 없음 | 상세 로그 ? |
| **하이브 일꾼 수** | 표시 없음 | X/Y 표시 ? |
| **여왕벌 일꾼 수** | 전체 수만 | 전체 수 ? |

---

## ?? UI 표시 예시

### 하이브 선택 시

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  꿀벌집
  HP: 200/200
  일꾼 수: 5/10  ?
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 여왕벌 선택 시

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  여왕벌
  HP: 100/100
  공격력: 15
  일꾼 수: 15  ?
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 일반 유닛 선택 시

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  일꾼 꿀벌
  HP: 45/50
  공격력: 10
━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 일꾼 수가 MaxWorkers를 넘지 않음
[ ] SpawnLoop에서 null 제거 작동
[ ] SpawnWorker 안전장치 작동
[ ] 하이브 선택 시 "일꾼 수: X/Y" 표시
[ ] 여왕벌 선택 시 "일꾼 수: X" 표시
[ ] 일반 유닛 선택 시 일꾼 수 표시 안 됨
[ ] 디버그 로그 정상 출력 (showDebugLogs = true)
```

---

## ?? 문제 해결

### Q: 일꾼 수가 여전히 MaxWorkers를 넘어요

**확인:**
```
[ ] SpawnLoop의 RemoveAll 작동
[ ] SpawnWorker의 안전장치 작동
[ ] maxWorkers 값 확인 (Inspector)
[ ] showDebugLogs = true로 로그 확인
```

**해결:**
```
1. Console에서 "일꾼 생성: X/Y" 로그 확인
2. "최대 일꾼 수 도달" 로그 확인
3. workers 리스트에 null이 있는지 확인
```

### Q: 하이브 선택 시 일꾼 수가 안 나와요

**확인:**
```
[ ] workerCountText != null
[ ] hive.GetWorkers() 작동
[ ] UpdateUnitInfo() 호출
[ ] workerCountText Inspector 연결
```

**해결:**
```
1. UnitCommandPanel Inspector에서 WorkerCountText 연결 확인
2. UpdateUnitInfo() 호출 확인
3. Console에서 에러 확인
```

### Q: 여왕벌 선택 시 일꾼 수가 이상해요

**확인:**
```
[ ] FindObjectsOfType<UnitAgent>() 작동
[ ] faction == Player && !isQueen 조건
[ ] 여왕벌이 일꾼으로 카운트 안 되는지
```

---

## ?? 추가 개선 아이디어

### 1. 하이브별 일꾼 수 제한

```csharp
// 각 하이브마다 독립적인 MaxWorkers
public class Hive
{
    [Header("하이브 설정")]
    public int maxWorkers = 10;
    
    // 스폰 시 체크
    if (workers.Count < maxWorkers) // 하이브별 ?
    {
        SpawnWorker();
    }
}
```

### 2. 일꾼 수 색상 표시

```csharp
// 일꾼 수에 따라 색상 변경
if (workerCount >= maxWorkers * 0.8f)
    workerCountText.color = Color.green; // 80% 이상
else if (workerCount >= maxWorkers * 0.5f)
    workerCountText.color = Color.yellow; // 50% 이상
else
    workerCountText.color = Color.red; // 50% 미만
```

### 3. 일꾼 수 변경 애니메이션

```csharp
// 일꾼 수 변경 시 애니메이션
int previousWorkerCount = 0;

void UpdateUnitInfo()
{
    if (workerCount != previousWorkerCount)
    {
        workerCountText.GetComponent<Animator>().SetTrigger("Change");
        previousWorkerCount = workerCount;
    }
}
```

### 4. 하이브 상태 표시

```csharp
// 하이브 상태 추가
if (hive != null)
{
    string status = "";
    if (hive.isRelocating)
        status = " (이사 중)";
    else if (workerCount >= maxWorkers)
        status = " (만원)";
    
    workerCountText.text = $"일꾼 수: {workerCount}/{maxWorkers}{status}";
}
```

---

## ?? 완료!

**핵심 수정:**
- ? MaxWorkers 제한 강화 (2중 체크)
- ? null 제거 (RemoveAll)
- ? 하이브 일꾼 수 표시 (X/Y)
- ? 여왕벌 전체 일꾼 수 표시 (X)
- ? 디버그 로그 추가

**Unity 설정:**
- Hive Inspector: showDebugLogs 체크 가능

**게임 플레이:**
- 정확한 일꾼 수 관리
- 직관적인 UI 표시
- 하이브별 일꾼 수 확인

게임 개발 화이팅! ??????
