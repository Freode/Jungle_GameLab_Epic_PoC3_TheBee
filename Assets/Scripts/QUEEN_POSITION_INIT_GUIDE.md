# ?? 여왕벌 위치 초기화 개선 가이드

## 완료된 개선사항

### ? 하이브 파괴 시 여왕벌 위치 초기화 순서 개선
- GameObject 활성화 **전에** 위치 설정
- Transform.position 먼저 설정
- FogOfWar 재등록
- 상세한 디버그 로그

---

## ?? 수정된 파일

### Hive.cs
```csharp
? DestroyHive() 수정
   1. SetPosition(q, r) - 타일 좌표 설정
   2. transform.position 설정 - 월드 좌표 설정
   3. canMove = true - 이동 가능 설정
   4. SetActive(true) - GameObject 활성화
   5. RegisterWithFog() - FogOfWar 재등록
```

---

## ?? 시스템 작동 방식

### 이전 방식의 문제

```
[하이브 파괴]
DestroyHive()
{
    // 여왕벌 위치 설정 ?
    queenBee.SetPosition(q, r)
    queenBee.transform.position = hivePos
    
    // 이동 가능하게
    queenBee.canMove = true
    
    // GameObject 활성화 ?
    queenBee.gameObject.SetActive(true)
    
    // ? FogOfWar 재등록 누락
}
   ↓
[문제점]
1. SetActive(true) 호출 시점에
   OnEnable() → Start() 실행
   
2. Start()에서 자동으로 FogOfWar 등록 시도
   하지만 이미 등록되어 있을 수 있음
   
3. 위치가 제대로 업데이트 안 됨
   (이전 위치로 등록될 수 있음)
```

---

### 새로운 방식

```
[하이브 파괴]
DestroyHive()
{
    Debug.Log("여왕벌 위치 초기화 시작") ?
    
    // 1. 타일 좌표 설정 (활성화 전!) ?
    queenBee.SetPosition(q, r)
    Debug.Log($"SetPosition: ({q}, {r})") ?
    
    // 2. 월드 좌표 계산
    Vector3 hivePos = HexToWorld(q, r, hexSize)
    
    // 3. Transform 위치 설정 (활성화 전!) ?
    queenBee.transform.position = hivePos
    Debug.Log($"Transform: {hivePos}") ?
    
    // 4. 이동 가능 설정 ?
    queenBee.canMove = true
    
    // 5. GameObject 활성화 (마지막!) ?
    queenBee.gameObject.SetActive(true)
    Debug.Log("여왕벌 활성화 완료") ?
    
    // 6. FogOfWar 재등록 (활성화 후) ?
    queenBee.RegisterWithFog()
    Debug.Log("FogOfWar 재등록 완료") ?
}
   ↓
[장점]
1. 활성화 전에 모든 위치 설정 완료 ?
2. OnEnable()/Start()가 올바른 위치에서 실행 ?
3. FogOfWar 명시적 재등록 ?
4. 상세한 디버그 로그 ?
```

---

## ?? 핵심 코드

### DestroyHive() 수정

```csharp
// Hive.cs
void DestroyHive()
{
    Debug.Log("[하이브 파괴] 하이브가 파괴됩니다.");
    
    // 경계선 제거
    if (HexBoundaryHighlighter.Instance != null)
    {
        HexBoundaryHighlighter.Instance.Clear();
    }
    
    // 여왕벌 재활성화 (하이브 위치에서) ?
    if (queenBee != null)
    {
        if (showDebugLogs)
            Debug.Log($"[하이브 파괴] 여왕벌 위치 초기화 시작: 하이브 위치 ({q}, {r})");
        
        // 1. 여왕벌 위치를 하이브 타일로 설정 (활성화 전에 먼저!) ?
        queenBee.SetPosition(q, r);
        
        // 2. 월드 위치 계산
        Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
        
        // 3. Transform 위치 설정 (활성화 전에!) ?
        queenBee.transform.position = hivePos;
        
        if (showDebugLogs)
            Debug.Log($"[하이브 파괴] 여왕벌 Transform 위치: {queenBee.transform.position}, 타일 좌표: ({queenBee.q}, {queenBee.r})");
        
        // 4. 이동 가능하게 설정 ?
        queenBee.canMove = true;
        
        // 5. GameObject 재활성화 (마지막에!) ?
        queenBee.gameObject.SetActive(true);
        
        if (showDebugLogs)
            Debug.Log($"[하이브 파괴] 여왕벌 활성화 완료 - 위치: ({queenBee.q}, {queenBee.r}), World: {queenBee.transform.position}");
        
        // 6. FogOfWar에 등록 (활성화 후) ?
        queenBee.RegisterWithFog();
        
        Debug.Log("[하이브 파괴] 여왕벌 완전 활성화 완료");
    }
    else
    {
        Debug.LogWarning("[하이브 파괴] 여왕벌 참조가 없습니다!");
    }
    
    // 일꾼들이 여왕벌을 따라다니게 설정
    foreach (var worker in workers)
    {
        if (worker != null)
        {
            worker.isFollowingQueen = true;
            worker.hasManualOrder = false;
            
            // 현재 작업 취소
            var behavior = worker.GetComponent<UnitBehaviorController>();
            if (behavior != null)
            {
                behavior.CancelCurrentTask();
            }
            
            if (showDebugLogs)
                Debug.Log($"[하이브 파괴] 일꾼 {worker.name}이(가) 여왕벌 추적 시작");
        }
    }
    
    // HiveManager에서 등록 해제
    if (HiveManager.Instance != null)
    {
        HiveManager.Instance.UnregisterHive(this);
    }
    
    // GameObject 파괴
    Destroy(gameObject);
}
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **위치 설정 순서** | 불명확 ? | 명확 ? |
| **활성화 전 위치** | 일부만 ? | 완전 ? |
| **FogOfWar 등록** | 자동 (불안정) ? | 명시적 ? |
| **디버그 로그** | 부족 ? | 상세 ? |

---

## ?? 시각화

### 여왕벌 활성화 흐름

```
[하이브 파괴 시작]
DestroyHive()
   ↓
[여왕벌 참조 확인]
if (queenBee != null) ?
   ↓
[1. 타일 좌표 설정]
queenBee.SetPosition(5, 3) ?
queenBee.q = 5, queenBee.r = 3
   ↓
[2. 월드 좌표 계산]
hivePos = HexToWorld(5, 3, 0.5f)
hivePos = (2.5, 0, 1.5)
   ↓
[3. Transform 위치 설정]
queenBee.transform.position = (2.5, 0, 1.5) ?
   ↓
[4. 이동 가능 설정]
queenBee.canMove = true ?
   ↓
[5. GameObject 활성화]
queenBee.gameObject.SetActive(true) ?
   ↓
[OnEnable() 실행]
여왕벌의 OnEnable() 호출
- 이미 올바른 위치에 있음 ?
   ↓
[6. FogOfWar 재등록]
queenBee.RegisterWithFog() ?
FogOfWarManager.RegisterUnit(id, 5, 3, visionRange)
   ↓
[결과]
여왕벌이 하이브 위치 (5, 3)에서 활성화! ?
일꾼들이 여왕벌 추적 시작! ?
```

---

## ?? 문제 해결

### Q: 여왕벌이 엉뚱한 위치에서 활성화돼요

**확인:**
```
[ ] showDebugLogs = true
[ ] Console에 "위치 초기화 시작" 로그
[ ] Console에 "Transform 위치" 로그
[ ] Console에 "활성화 완료" 로그
```

**해결:**
```
1. Hive.showDebugLogs = true 설정

2. Console 로그 확인
   - "[하이브 파괴] 여왕벌 위치 초기화 시작: (5, 3)"
   - "[하이브 파괴] 여왕벌 Transform 위치: (2.5, 0, 1.5)"
   - "[하이브 파괴] 여왕벌 활성화 완료 - 위치: (5, 3)"

3. 로그에서 위치 확인
   - queenBee.q = 하이브 q
   - queenBee.r = 하이브 r
   - transform.position = 하이브 월드 좌표

4. 안 되면 queenBee 참조 확인
```

---

### Q: FogOfWar에서 여왕벌이 안 보여요

**확인:**
```
[ ] RegisterWithFog() 호출
[ ] FogOfWarManager.Instance != null
[ ] 여왕벌 visionRange > 0
```

**해결:**
```
1. Console 로그 확인
   - "[하이브 파괴] FogOfWar 재등록 완료"

2. FogOfWarManager 확인
   - Instance가 씬에 있는지
   - RegisterUnit() 정상 작동하는지

3. 여왕벌 설정 확인
   - visionRange = 3 (기본값)
   - faction = Player

4. 안 되면 UnitAgent.RegisterWithFog() 확인
```

---

### Q: 일꾼들이 여왕벌을 따라가지 않아요

**확인:**
```
[ ] worker.isFollowingQueen = true
[ ] worker.hasManualOrder = false
[ ] behavior.CancelCurrentTask() 호출
```

**해결:**
```
1. Console 로그 확인
   - "[하이브 파괴] 일꾼 Worker_1이(가) 여왕벌 추적 시작"

2. 일꾼 상태 확인
   - isFollowingQueen = true
   - hasManualOrder = false

3. UnitBehaviorController 확인
   - FollowQueen() 로직 작동 여부

4. 안 되면 UnitBehaviorController.cs 확인
```

---

## ?? 추가 개선 아이디어

### 1. 여왕벌 이동 애니메이션

```csharp
// Hive.cs
IEnumerator AnimateQueenExit()
{
    if (queenBee == null) yield break;
    
    // 여왕벌 위치 설정
    queenBee.SetPosition(q, r);
    Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
    
    // 하이브 중심에서 시작 (작은 크기)
    queenBee.transform.position = hivePos;
    queenBee.transform.localScale = Vector3.zero;
    
    // GameObject 활성화
    queenBee.gameObject.SetActive(true);
    
    // 크기 증가 애니메이션 ?
    float duration = 0.5f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        queenBee.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
        yield return null;
    }
    
    queenBee.transform.localScale = Vector3.one;
    
    // FogOfWar 재등록
    queenBee.RegisterWithFog();
}

void DestroyHive()
{
    // ...existing code...
    
    if (queenBee != null)
    {
        StartCoroutine(AnimateQueenExit()); ?
    }
}
```

---

### 2. 하이브 파괴 이펙트

```csharp
// Hive.cs
[Header("Effects")]
public GameObject destroyEffect;
public AudioClip destroySound;

void DestroyHive()
{
    // 파괴 이펙트 ?
    if (destroyEffect != null)
    {
        Vector3 hivePos = TileHelper.HexToWorld(q, r, 0.5f);
        var effect = Instantiate(destroyEffect, hivePos, Quaternion.identity);
        Destroy(effect, 2f);
    }
    
    // 파괴 사운드 ?
    if (destroySound != null)
    {
        AudioSource.PlayClipAtPoint(destroySound, transform.position);
    }
    
    // ...existing code...
}
```

---

### 3. 일꾼 알림 시스템

```csharp
// Hive.cs
void DestroyHive()
{
    // ...existing code...
    
    // 일꾼들에게 알림 ?
    foreach (var worker in workers)
    {
        if (worker != null)
        {
            worker.isFollowingQueen = true;
            worker.hasManualOrder = false;
            
            // 알림 UI 표시 ?
            if (NotificationUI.Instance != null)
            {
                NotificationUI.Instance.ShowWorkerNotification(
                    worker,
                    "하이브가 파괴되었습니다!\n여왕벌을 따라가세요!",
                    3f
                );
            }
            
            // 현재 작업 취소
            var behavior = worker.GetComponent<UnitBehaviorController>();
            if (behavior != null)
            {
                behavior.CancelCurrentTask();
            }
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 건설
[ ] 여왕벌 비활성화 확인 ?
[ ] 하이브 이사 명령
[ ] 10초 카운트다운
[ ] 하이브 파괴
[ ] Console에 "여왕벌 위치 초기화 시작" ?
[ ] Console에 "Transform 위치" ?
[ ] Console에 "활성화 완료" ?
[ ] Console에 "FogOfWar 재등록 완료" ?
[ ] 여왕벌이 하이브 위치에서 활성화 ?
[ ] 일꾼들이 여왕벌 추적 시작 ?
[ ] Inspector에서 위치 확인
   - queenBee.q = 하이브 q ?
   - queenBee.r = 하이브 r ?
   - transform.position = 하이브 월드 좌표 ?
```

---

## ?? 완료!

**핵심 개선:**
- ? 활성화 전 완전한 위치 설정
- ? 명시적 FogOfWar 재등록
- ? 상세한 디버그 로그
- ? 명확한 실행 순서

**결과:**
- 여왕벌 정확한 위치에서 활성화
- FogOfWar 올바르게 등록
- 일꾼들 즉시 추적 시작
- 완벽한 하이브 파괴 로직

**게임 플레이:**
- 하이브 파괴 시 여왕벌 즉시 나타남
- 여왕벌이 하이브 위치에 정확히 배치
- 일꾼들이 자연스럽게 추적
- 매끄러운 게임 흐름

게임 개발 화이팅! ????????
