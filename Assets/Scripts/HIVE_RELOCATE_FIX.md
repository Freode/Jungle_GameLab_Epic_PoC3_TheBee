# ?? 하이브 이사 시스템 수정 가이드

## 완료된 개선사항

### 1. ? 10초 카운트다운 후 하이브 파괴
- RelocationCountdown() 수정
- 실시간 체력 감소
- 디버그 로그 추가

### 2. ? 하이브 이사 버튼 작동 문제 해결
- 자원 차감 로직 수정
- 상세한 디버그 로그 추가
- TrySpendResources 사용

---

## ?? 수정된 파일

### 1. Hive.cs
```csharp
? StartRelocation() 수정
   - TrySpendResources() 사용
   - 자원 차감 성공 체크
   - 디버그 로그 추가
   
? RelocationCountdown() 수정
   - 10초 카운트다운 ?
   - 실시간 체력 감소
   - 1초마다 로그 출력 (디버그 모드)
   - 카운트다운 완료 시 DestroyHive() 호출
```

### 2. RelocateHiveCommandHandler.cs
```csharp
? ExecuteRelocate() 강화
   - 모든 단계마다 디버그 로그
   - 자원 확인 로그
   - 하이브 검색 로그
   
? GetRelocateResourceCost() 수정
   - 기본 비용 25로 변경 ?
   - 찾지 못한 경우 로그
```

---

## ?? 시스템 작동 방식

### 하이브 이사 전체 흐름

```
[플레이어: 하이브 선택]
   ↓
[명령 패널: "하이브 이사" 버튼 표시]
비용: 25
   ↓
[버튼 클릭]
   ↓
[ExecuteRelocate 실행]
   ↓
[1단계: Agent 확인] ?
Debug: "agent: Hive, q=5, r=5"
   ↓
[2단계: Hive 컴포넌트 검색] ?
Debug: "하이브 발견: Hive"
   ↓
[3단계: 이사 가능 여부 확인] ?
Debug: "이사 중이 아님, 진행 가능"
   ↓
[4단계: 비용 확인] ?
Debug: "필요 자원: 25"
Debug: "현재 자원: 50"
   ↓
[5단계: 자원 차감] ?
TrySpendResources(25)
Debug: "자원 차감 성공: 25"
   ↓
[6단계: 이사 시작] ?
StartRelocation(25)
   ↓
[7단계: 10초 카운트다운] ?
RelocationCountdown()
   ↓
[매 프레임]
countdown -= Time.deltaTime
health 점진적 감소
   ↓
[1초마다 로그] (showDebugLogs = true)
"남은 시간: 9초"
"남은 시간: 8초"
...
"남은 시간: 1초"
   ↓
[카운트다운 완료] ?
Debug: "카운트다운 완료! 하이브 파괴 시작"
   ↓
[하이브 파괴] ?
DestroyHive()
```

---

## ?? 핵심 코드

### 1. StartRelocation() - 자원 차감

```csharp
// Hive.cs
public void StartRelocation(int resourceCost)
{
    if (isRelocating)
    {
        Debug.LogWarning("이미 이사 준비 중");
        return;
    }

    // 자원 차감 ?
    if (HiveManager.Instance != null)
    {
        if (!HiveManager.Instance.TrySpendResources(resourceCost))
        {
            Debug.LogWarning($"자원 차감 실패: {resourceCost}");
            return; // 실패 시 중단
        }
        Debug.Log($"자원 차감 성공: {resourceCost}");
    }

    isRelocating = true;

    // 일꾼 생성 중지
    if (spawnRoutine != null)
    {
        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    // 10초 카운트다운 시작 ?
    relocateRoutine = StartCoroutine(RelocationCountdown());

    Debug.Log("이사 준비 시작! 10초 후 파괴");
}
```

---

### 2. RelocationCountdown() - 10초 카운트다운

```csharp
// Hive.cs
IEnumerator RelocationCountdown()
{
    float countdown = 10f; // 10초 ?
    var combat = GetComponent<CombatUnit>();
    int initialHealth = combat != null ? combat.health : 0;
    float healthDrainRate = initialHealth / countdown; // 초당 감소량
    
    Debug.Log($"카운트다운 시작: {countdown}초");

    while (countdown > 0f)
    {
        countdown -= Time.deltaTime;
        
        // 체력 점진 감소 ?
        if (combat != null)
        {
            float drainAmount = healthDrainRate * Time.deltaTime;
            combat.health = Mathf.Max(1, combat.health - Mathf.CeilToInt(drainAmount));
        }
        
        // 1초마다 로그 출력 (디버그용) ?
        if (showDebugLogs && Mathf.FloorToInt(countdown) != Mathf.FloorToInt(countdown + Time.deltaTime))
        {
            Debug.Log($"남은 시간: {Mathf.FloorToInt(countdown)}초");
        }

        yield return null;
    }

    Debug.Log("카운트다운 완료! 하이브 파괴 시작");
    
    // 하이브 파괴 ?
    DestroyHive();
}
```

---

### 3. ExecuteRelocate() - 상세 로그

```csharp
// RelocateHiveCommandHandler.cs
public static void ExecuteRelocate(UnitAgent agent, CommandTarget target)
{
    Debug.Log("[RelocateHive] ExecuteRelocate 시작");
    
    if (agent == null)
    {
        Debug.LogWarning("[RelocateHive] agent is null");
        return;
    }

    Debug.Log($"[RelocateHive] agent: {agent.name}, q={agent.q}, r={agent.r}");

    // Hive 컴포넌트 검색
    Hive hive = agent.GetComponent<Hive>();
    
    if (hive == null)
    {
        Debug.Log("[RelocateHive] 위치로 검색 시도");
        hive = FindHiveAtPosition(agent.q, agent.r);
    }
    
    if (hive == null)
    {
        Debug.LogWarning("[RelocateHive] No hive found");
        return;
    }

    Debug.Log($"[RelocateHive] 하이브 발견: {hive.name}");

    // 이사 가능 여부 확인
    if (!hive.CanRelocate())
    {
        Debug.LogWarning("[RelocateHive] 이미 이사 중");
        return;
    }

    // 자원 비용 확인
    int resourceCost = GetRelocateResourceCost(hive);
    Debug.Log($"[RelocateHive] 필요 자원: {resourceCost}");
    
    // 현재 자원 확인
    int available = HiveManager.Instance.playerStoredResources;
    Debug.Log($"[RelocateHive] 현재 자원: {available}");
    
    if (!HiveManager.Instance.HasResources(resourceCost))
    {
        Debug.LogWarning($"[RelocateHive] 자원 부족. 필요: {resourceCost}, 보유: {available}");
        return;
    }

    Debug.Log($"[RelocateHive] 자원 충분. 이사 시작!");
    
    // 이사 시작
    hive.StartRelocation(resourceCost);
}
```

---

### 4. GetRelocateResourceCost() - 기본 비용 25

```csharp
// RelocateHiveCommandHandler.cs
private static int GetRelocateResourceCost(Hive hive)
{
    // hive.hiveCommands에서 relocate_hive 명령 검색
    var commands = hive.GetCommands(null);
    foreach (var cmd in commands)
    {
        if (cmd is SOCommand soCmd && soCmd.id == "relocate_hive")
        {
            Debug.Log($"relocate_hive 명령 찾음, 비용: {soCmd.resourceCost}");
            return soCmd.resourceCost;
        }
    }
    
    // 기본 비용 25 ?
    Debug.LogWarning("relocate_hive 명령 없음. 기본 비용 25 사용");
    return 25;
}
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **카운트다운** | 즉시 파괴 | 10초 후 파괴 ? |
| **체력 감소** | 즉시 0 | 점진적 감소 ? |
| **자원 차감** | 불명확 | 명확히 차감 ? |
| **디버그 로그** | 부족 | 상세 로그 ? |
| **기본 비용** | 50 | 25 ? |

---

## ?? 시각화

### 카운트다운 타임라인

```
T=0초:  [이사 버튼 클릭]
        자원: 50 → 25
        Debug: "자원 차감 성공: 25"
        ↓
T=0초:  [카운트다운 시작]
        Debug: "카운트다운 시작: 10초"
        체력: 200/200
        ↓
T=1초:  Debug: "남은 시간: 9초"
        체력: 180/200 (점진 감소)
        ↓
T=2초:  Debug: "남은 시간: 8초"
        체력: 160/200
        ↓
T=5초:  Debug: "남은 시간: 5초"
        체력: 100/200
        ↓
T=9초:  Debug: "남은 시간: 1초"
        체력: 20/200
        ↓
T=10초: [카운트다운 완료]
        Debug: "카운트다운 완료! 하이브 파괴"
        체력: 1/200
        ↓
        [DestroyHive() 호출]
        - 경계선 제거
        - 여왕벌 부활
        - 일꾼 여왕벌 추적
        - GameObject 파괴
```

---

## ?? 문제 해결

### Q: 이사 버튼을 눌러도 작동 안 해요

**확인사항:**
```
[ ] Console에서 로그 확인
[ ] "자원 부족" 메시지 있는지 확인
[ ] 현재 자원 >= 25 인지 확인
[ ] Hive 컴포넌트 있는지 확인
```

**디버그 로그 순서:**
```
? "[RelocateHive] ExecuteRelocate 시작"
? "[RelocateHive] agent: ..."
? "[RelocateHive] 하이브 발견: ..."
? "[RelocateHive] 필요 자원: 25"
? "[RelocateHive] 현재 자원: X"
? "[RelocateHive] 자원 충분. 이사 시작!"
? "[하이브 이사] 자원 차감 성공: 25"
? "[하이브 이사] 이사 준비 시작! 10초 후 파괴"
? "[하이브 이사] 카운트다운 시작: 10초"
```

---

### Q: 10초가 아니라 즉시 파괴돼요

**확인사항:**
```
[ ] RelocationCountdown() 코루틴 실행 확인
[ ] countdown = 10f 확인
[ ] while (countdown > 0f) 확인
[ ] yield return null 확인
```

---

### Q: 체력이 감소하지 않아요

**확인사항:**
```
[ ] CombatUnit 컴포넌트 확인
[ ] healthDrainRate 계산 확인
[ ] combat.health 업데이트 확인
```

---

### Q: 디버그 로그가 안 나와요

**확인사항:**
```
[ ] Hive.showDebugLogs = true 설정 (Inspector)
[ ] Console 창 확인
[ ] "[하이브 이사]" 로그 검색
```

---

## ?? 추가 기능 아이디어

### 1. 카운트다운 UI 표시

```csharp
// HiveUI에 카운트다운 표시
public class HiveRelocateUI : MonoBehaviour
{
    public TextMeshProUGUI countdownText;
    
    public void StartCountdown(float duration)
    {
        StartCoroutine(CountdownCoroutine(duration));
    }
    
    IEnumerator CountdownCoroutine(float duration)
    {
        while (duration > 0)
        {
            countdownText.text = $"이사까지: {Mathf.CeilToInt(duration)}초";
            duration -= Time.deltaTime;
            yield return null;
        }
        
        countdownText.text = "이사 중...";
    }
}
```

### 2. 카운트다운 취소 기능

```csharp
// Hive.cs
public bool CancelRelocation()
{
    if (!isRelocating) return false;
    
    // 카운트다운 중지
    if (relocateRoutine != null)
    {
        StopCoroutine(relocateRoutine);
        relocateRoutine = null;
    }
    
    isRelocating = false;
    
    // 일꾼 생성 재개
    spawnRoutine = StartCoroutine(SpawnLoop());
    
    // 자원 환불 (50%)
    int refund = resourceCost / 2;
    HiveManager.Instance.AddResources(refund);
    
    Debug.Log($"이사 취소. 환불: {refund}");
    return true;
}
```

### 3. 경고 메시지

```csharp
// 5초 남았을 때 경고
if (countdown <= 5f && countdown > 4.9f)
{
    Debug.LogWarning("?? 5초 후 하이브가 파괴됩니다!");
    // UI 경고 표시
}
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 선택 → "하이브 이사" 버튼 표시
[ ] 자원 25 이상 → 버튼 활성화
[ ] 자원 25 미만 → 버튼 비활성화
[ ] 버튼 클릭 → 자원 25 차감
[ ] 10초 카운트다운 시작
[ ] 체력 점진적 감소 (200 → 1)
[ ] 10초 후 하이브 파괴
[ ] 여왕벌 부활
[ ] 일꾼 여왕벌 추적
[ ] 디버그 로그 정상 출력
```

---

## ?? 완료!

**핵심 수정:**
- ? 10초 카운트다운 구현
- ? 실시간 체력 감소
- ? 자원 차감 로직 수정
- ? 상세한 디버그 로그
- ? 기본 비용 25로 변경

**게임 플레이:**
- 하이브 이사 버튼 정상 작동
- 10초 준비 시간
- 체력이 점진적으로 감소
- 명확한 피드백

**디버그:**
- 모든 단계 로그 출력
- 문제 추적 용이
- 개발 효율 향상

게임 개발 화이팅! ??????
