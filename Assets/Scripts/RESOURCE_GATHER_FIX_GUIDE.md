# ?? 자원 채취 순간이동 문제 수정 가이드

## 완료된 개선사항

### ? 자원 채취 로직 완전 수정
- 매번 새로운 타일 내 위치로 이동
- 순간이동 문제 해결
- 단계별 명확한 이동

---

## ?? 이전 문제

### 순간이동 현상

```
[1차 채취]
하이브 → 자원 타일 → 하이브 (정상)

[2차 채취]
하이브 → 자원 타일 (순간이동!) ?

원인:
- targetTile 위치를 기억
- 이전 workPosition으로 바로 이동
- 경로 없이 순간이동
```

---

## ? 수정된 로직

### 새로운 채취 흐름

```
1. 타일 좌표 입력 (명령)
   ↓
2. 타일 중심까지 경로 이동
   ↓
3. 타일 도착 후 타일 내 랜덤 위치로 이동
   (margin 25% 적용)
   ↓
4. 자원 채취
   ↓
5. 하이브까지 경로 이동
   ↓
6. 하이브 도착 후 타일 내 랜덤 위치로 이동
   ↓
7. 자원 전달
   ↓
8. 자원 남았으면 다시 1번부터 반복 ?
```

---

## ?? 핵심 코드 변경

### MoveAndGather() - 변경 없음

```csharp
void MoveAndGather(HexTile tile)
{
    // 경로 찾기
    var path = Pathfinder.FindPath(start, dest);
    
    // 경로 설정
    mover.SetPath(path);
    
    // targetTile 저장 (반복용)
    targetTile = dest;
    
    // 도착 후 채취
    StartCoroutine(WaitAndGatherRoutine(dest));
}
```

### WaitAndGatherRoutine() - 완전 수정

```csharp
IEnumerator WaitAndGatherRoutine(HexTile dest)
{
    // 1단계: 타일 중심까지 이동 대기
    Vector3 tileCenter = HexToWorld(dest.q, dest.r, hexSize);
    
    while (Distance(transform.position, tileCenter) > 0.15f)
    {
        yield return null;
    }
    
    // 2단계: 타일 내부 랜덤 위치로 이동 (작업 위치)
    Vector3 workPosition = GetRandomPositionInTile(
        dest.q, dest.r, hexSize, 0.25f  // margin 25%
    );
    
    // 부드럽게 이동 (0.3초)
    float moveTime = 0.3f;
    float elapsed = 0f;
    Vector3 startPos = transform.position;
    
    while (elapsed < moveTime)
    {
        elapsed += Time.deltaTime;
        float t = Clamp01(elapsed / moveTime);
        transform.position = Lerp(startPos, workPosition, t);
        yield return null;
    }
    
    transform.position = workPosition;
    
    // 3단계: 자원 채취
    int taken = dest.TakeResource(gatherAmount);
    
    // 4단계: 하이브로 복귀 (매번 새 경로)
    var pathBack = Pathfinder.FindPath(currentTile, hiveTile);
    mover.SetPath(pathBack); ?
    
    StartCoroutine(DeliverResourcesRoutine(hive, taken, dest));
}
```

### DeliverResourcesRoutine() - 수정

```csharp
IEnumerator DeliverResourcesRoutine(Hive hive, int amount, HexTile sourceTile)
{
    // 하이브 중심까지 이동 대기
    Vector3 hiveCenter = HexToWorld(hive.q, hive.r, hexSize);
    
    float timeout = 0f;
    while (Distance(position, hiveCenter) > 0.2f && timeout < 15f)
    {
        timeout += Time.deltaTime;
        yield return null;
    }
    
    // 하이브 내부 랜덤 위치로 이동
    Vector3 deliverPosition = GetRandomPositionInTile(
        hive.q, hive.r, hexSize, 0.25f
    );
    
    // 부드럽게 이동 (0.2초)
    // ... Lerp 이동
    
    // 자원 전달
    HiveManager.Instance.AddResources(amount);
    
    // 자원 남았으면 다시 채취
    if (sourceTile.resourceAmount > 0)
    {
        yield return new WaitForSeconds(gatherCooldown);
        MoveAndGather(sourceTile); ? // 매번 새 경로
    }
    else
    {
        currentTask = Idle;
        targetTile = null; ? // 초기화
    }
}
```

---

## ?? 단계별 상세 설명

### 1단계: 타일 중심까지 이동

```
[일꾼] 하이브 (0, 0)
   ↓
[명령] 자원 타일 (3, 2)로 이동
   ↓
[경로 생성]
Path: (0,0) → (1,0) → (2,1) → (3,2)
   ↓
[이동 시작]
mover.SetPath(path) ?
   ↓
[타일 중심 도착 대기]
while (Distance > 0.15f)
```

### 2단계: 타일 내 작업 위치 이동

```
[타일 중심 도착]
위치: (3.0, 2.0, 0)
   ↓
[랜덤 작업 위치 생성]
workPosition = GetRandomPositionInTile(
    3, 2, hexSize, 0.25f
)
→ 예: (3.12, 2.08, 0)
   ↓
[부드러운 이동 0.3초]
Lerp(중심, 작업위치, t)
   ↓
[작업 위치 도착]
transform.position = workPosition ?
```

### 3단계: 자원 채취

```
[작업 위치]
위치: (3.12, 2.08, 0)
   ↓
[자원 채취]
int taken = dest.TakeResource(gatherAmount)
→ taken = 5
   ↓
[로그]
"[자원 채취] Worker이(가) (3, 2)에서 5 자원 채취"
```

### 4단계: 하이브 복귀

```
[현재 위치]
타일: (3, 2)
위치: (3.12, 2.08, 0)
   ↓
[경로 생성 (매번 새로 계산)]
var currentTile = GetTile(3, 2)
var hiveTile = GetTile(0, 0)
var pathBack = FindPath(currentTile, hiveTile)
→ Path: (3,2) → (2,1) → (1,0) → (0,0) ?
   ↓
[경로 설정]
mover.SetPath(pathBack) ?
   ↓
[이동 시작]
정상 속도로 이동
```

### 5단계: 하이브 도착

```
[하이브 중심 도착 대기]
Vector3 hiveCenter = (0.0, 0.0, 0)
while (Distance > 0.2f)
   ↓
[하이브 내 전달 위치 생성]
deliverPosition = GetRandomPositionInTile(
    0, 0, hexSize, 0.25f
)
→ 예: (-0.08, 0.15, 0)
   ↓
[부드러운 이동 0.2초]
Lerp(중심, 전달위치, t)
   ↓
[전달 위치 도착]
transform.position = deliverPosition ?
```

### 6단계: 자원 전달

```
[전달 위치]
위치: (-0.08, 0.15, 0)
   ↓
[자원 전달]
HiveManager.Instance.AddResources(5)
   ↓
[로그]
"[자원 전달] Worker이(가) 5 자원 전달 완료"
   ↓
[자원 추가]
playerStoredResources += 5 ?
```

### 7단계: 반복 또는 종료

```
[자원 남았는지 확인]
if (sourceTile.resourceAmount > 0)
   ↓
[쿨다운 대기]
yield return WaitForSeconds(gatherCooldown)
   ↓
[다시 채취]
MoveAndGather(sourceTile) ?
   ↓
다시 1단계부터 반복
(매번 새로운 경로로 이동)
```

---

## ?? 시각화

### 이전 (순간이동 문제)

```
[1차]
?? → → → ?? (자원)
         ↓ 채취
?? ← ← ← ??
   전달

[2차]
?? ......... ?? (순간이동!) ?
         ↓ 채취
?? ← ← ← ??
```

### 현재 (정상 이동)

```
[1차]
?? → → → ?? 중심
         ↓
       ?? 작업위치
         ↓ 채취
?? ← ← ← ??
   전달

[2차]
?? → → → ?? 중심 ?
         ↓
       ?? 작업위치 (새 위치)
         ↓ 채취
?? ← ← ← ??
   전달 ?
```

---

## ?? 비교표

| 요소 | 이전 | 현재 |
|------|------|------|
| **1차 채취** | 정상 | 정상 ? |
| **2차 채취** | 순간이동 ? | 정상 이동 ? |
| **경로 생성** | 1차만 | 매번 ? |
| **작업 위치** | 고정 | 매번 랜덤 ? |
| **전달 위치** | 고정 | 매번 랜덤 ? |
| **targetTile** | 유지 | 소진 시 초기화 ? |

---

## ?? 설정 조절

### 작업 위치 마진

```csharp
// WaitAndGatherRoutine()
Vector3 workPosition = GetRandomPositionInTile(
    dest.q, dest.r, hexSize, 0.25f  // 마진 25%
);

// 더 넓게 (타일 경계 가까이)
margin = 0.15f; // 15%

// 더 좁게 (타일 중심 가까이)
margin = 0.35f; // 35%
```

### 이동 시간

```csharp
// WaitAndGatherRoutine() - 작업 위치 이동
float moveTime = 0.3f; // 0.3초

// 더 빠르게
moveTime = 0.2f;

// 더 느리게
moveTime = 0.5f;
```

### 타임아웃

```csharp
// DeliverResourcesRoutine()
float maxWaitTime = 15f; // 15초

// 더 길게
maxWaitTime = 20f;

// 더 짧게
maxWaitTime = 10f;
```

---

## ?? 문제 해결

### Q: 여전히 순간이동해요

**확인:**
```
[ ] mover.SetPath(pathBack) 호출 확인
[ ] pathBack이 null이 아닌지 확인
[ ] Pathfinder.FindPath() 성공 확인
[ ] Console에서 경로 로그 확인
```

**해결:**
```csharp
// 경로 로그 추가
var pathBack = Pathfinder.FindPath(currentTile, hiveTile);
Debug.Log($"경로 개수: {pathBack?.Count ?? 0}");

if (pathBack != null && pathBack.Count > 0)
{
    mover.SetPath(pathBack);
}
```

### Q: 자원이 전달 안 돼요

**확인:**
```
[ ] HiveManager.Instance != null
[ ] amount > 0
[ ] "[자원 전달]" 로그 확인
[ ] playerStoredResources 값 확인
```

**해결:**
```csharp
// 전달 로그 상세화
Debug.Log($"자원 전달 시도: {amount}");
HiveManager.Instance.AddResources(amount);
Debug.Log($"현재 자원: {HiveManager.Instance.playerStoredResources}");
```

### Q: 타일 중심에서 안 움직여요

**확인:**
```
[ ] GetRandomPositionInTile() 호출 확인
[ ] workPosition 값 확인
[ ] Lerp 코루틴 실행 확인
```

---

## ? 테스트 체크리스트

```
[ ] 1차 채취: 정상 이동
[ ] 1차 전달: 자원 증가
[ ] 2차 채취: 정상 이동 (순간이동 없음)
[ ] 2차 전달: 자원 증가
[ ] 3차 채취: 정상 이동
[ ] 작업 위치 매번 다름
[ ] 전달 위치 매번 다름
[ ] 자원 소진 시 Idle
[ ] targetTile 초기화
```

---

## ?? 추가 개선 아이디어

### 1. 애니메이션 추가

```csharp
// 자원 채취 시 애니메이션
IEnumerator WaitAndGatherRoutine(HexTile dest)
{
    // ... 작업 위치 이동
    
    // 채취 애니메이션
    var animator = GetComponent<Animator>();
    if (animator != null)
    {
        animator.SetTrigger("Gather");
        yield return new WaitForSeconds(0.5f);
    }
    
    // 자원 채취
    int taken = dest.TakeResource(gatherAmount);
}
```

### 2. 파티클 효과

```csharp
// 자원 채취 시 파티클
public ParticleSystem gatherParticle;

int taken = dest.TakeResource(gatherAmount);

if (taken > 0 && gatherParticle != null)
{
    gatherParticle.transform.position = transform.position;
    gatherParticle.Play();
}
```

### 3. 사운드 추가

```csharp
public AudioClip gatherSound;
private AudioSource audioSource;

// 자원 채취
int taken = dest.TakeResource(gatherAmount);

if (taken > 0 && gatherSound != null)
{
    audioSource.PlayOneShot(gatherSound);
}
```

---

## ?? 완료!

**핵심 수정:**
- ? 순간이동 문제 해결
- ? 매번 새 경로 생성
- ? 매번 새 작업/전달 위치
- ? 단계별 명확한 이동
- ? targetTile 초기화

**로직 흐름:**
1. 타일 중심까지 경로 이동
2. 타일 내 랜덤 작업 위치 이동
3. 자원 채취
4. 하이브까지 경로 이동
5. 하이브 내 랜덤 전달 위치 이동
6. 자원 전달
7. 반복 or 종료

**결과:**
- 자연스러운 이동
- 순간이동 없음
- 매번 다른 위치
- 정확한 자원 전달

게임 개발 화이팅! ??????
