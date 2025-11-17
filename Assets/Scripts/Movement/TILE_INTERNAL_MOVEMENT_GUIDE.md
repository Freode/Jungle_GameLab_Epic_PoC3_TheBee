# ?? 타일 내부 이동 시스템 가이드

## 완료된 개선사항

### ? 타일 내부 랜덤 이동
- 타일 좌표는 유지
- World 좌표만 타일 내부에서 랜덤 변경
- 유닛들이 겹치지 않고 자연스럽게 분산

### ? 회피 행동 개선
- 공격 쿨타임 시 타일 내부에서만 이동
- 다른 타일로 도망가지 않음
- 전술적 회피 구현

### ? Idle 상태 개선
- 5초마다 타일 내부 랜덤 이동
- 유닛들이 자연스럽게 움직임
- 정적이지 않은 대기 상태

---

## ?? 수정된 파일

### 1. TileHelper.cs
```csharp
? GetRandomPositionInTile()
   - 타일 내부 랜덤 World 위치 생성
   
? GetRandomPositionInCurrentTile()
   - 현재 위치와 다른 타일 내부 위치 생성
```

### 2. UnitController.cs
```csharp
? MoveWithinCurrentTile()
   - 타일 내부 랜덤 이동 메서드
   
? MoveWithinTileCoroutine()
   - 부드러운 타일 내부 이동
```

### 3. EnemyAI.cs
```csharp
? EvadeInCurrentTile()
   - 타일 내부 이동으로 수정
   - controller.MoveWithinCurrentTile() 호출
```

### 4. UnitBehaviorController.cs
```csharp
? EvadeNearby()
   - 타일 내부 이동으로 수정
   
? IdleMovementWithinTile()
   - Idle 상태 시 타일 내부 이동
   
? WaitAndGatherRoutine()
   - 자원 채취 시 타일 내부 배치
```

### 5. Hive.cs
```csharp
? SpawnWorker()
   - 유닛 생성 시 타일 내부 랜덤 위치
```

---

## ?? 시스템 작동 방식

### 타일 좌표 vs World 좌표

```
[타일 좌표]
q = 0, r = 0 (헥스 그리드)

[World 좌표]
타일 내부 여러 위치:
- (0.1, 0, 0.2)
- (-0.15, 0, 0.1)
- (0.08, 0, -0.12)
...

타일은 동일하지만 World 위치는 다름 ?
```

### 타일 내부 랜덤 위치 생성

```csharp
Vector3 GetRandomPositionInTile(int q, int r, float hexSize, float margin)
{
    // 1. 타일 중심 위치
    Vector3 center = HexToWorld(q, r, hexSize);
    
    // 2. 랜덤 오프셋 (마진 제외)
    float maxOffset = hexSize * (1f - margin);
    float randomX = Random.Range(-maxOffset, maxOffset);
    float randomZ = Random.Range(-maxOffset, maxOffset);
    
    // 3. 중심 + 오프셋
    return center + new Vector3(randomX, 0, randomZ);
}
```

---

## ?? 시나리오

### 시나리오 1: 유닛 생성

```
[하이브에서 일꾼 생성]

이전:
모든 일꾼이 (0, 0, 0) 위치에 생성
→ 겹쳐서 안 보임 ?

현재:
타일: (0, 0)
World 위치:
- 일꾼 1: (0.12, 0, -0.08)
- 일꾼 2: (-0.15, 0, 0.11)
- 일꾼 3: (0.05, 0, 0.18)
→ 분산되어 보임 ?
```

### 시나리오 2: 전투 중 회피

```
[말벌 vs 일꾼]

T=0초:
타일: (5, 3)
말벌: (5.1, 0, 3.2) ← World 좌표
일꾼: (5.0, 0, 3.0)

말벌 공격 성공 ?

T=0.5초:
말벌 쿨타임 → 회피
타일: (5, 3) ← 유지
World: (5.15, 0, 3.12) ← 변경 ?

T=1초:
말벌 쿨타임 → 회피
타일: (5, 3) ← 유지
World: (4.92, 0, 3.08) ← 변경 ?

T=2초:
말벌 쿨타임 종료
타겟 추격 시작
```

### 시나리오 3: Idle 상태

```
[일꾼 대기 중]

T=0초:
타일: (2, 1)
World: (2.0, 0, 1.0)
상태: Idle

T=5초:
자동 이동 ?
타일: (2, 1) ← 유지
World: (2.08, 0, 0.95) ← 변경

T=10초:
자동 이동 ?
타일: (2, 1) ← 유지
World: (1.92, 0, 1.12) ← 변경

...자연스러운 움직임
```

### 시나리오 4: 자원 채취

```
[일꾼이 자원 타일 도착]

1. 타일 (3, 2) 도착
   ↓
2. 타일 내부 랜덤 위치로 이동 ?
   World: (3.1, 0, 2.05)
   ↓
3. 자원 채취
   ↓
4. 하이브로 복귀
```

---

## ?? 코드 구조

### TileHelper.cs

#### 타일 내부 랜덤 위치
```csharp
public static Vector3 GetRandomPositionInTile(
    int q, int r, 
    float hexSize, 
    float margin = 0.2f)
{
    Vector3 center = HexToWorld(q, r, hexSize);
    
    // 마진을 두고 랜덤 오프셋
    float maxOffset = hexSize * (1f - margin);
    float randomX = Random.Range(-maxOffset, maxOffset);
    float randomZ = Random.Range(-maxOffset, maxOffset);
    
    return new Vector3(
        center.x + randomX, 
        center.y, 
        center.z + randomZ
    );
}
```

#### 현재 위치와 다른 랜덤 위치
```csharp
public static Vector3 GetRandomPositionInCurrentTile(
    Vector3 currentPos, 
    int q, int r, 
    float hexSize, 
    float margin = 0.2f)
{
    Vector3 newPos = GetRandomPositionInTile(q, r, hexSize, margin);
    
    // 너무 가까우면 다시 생성
    if (Vector3.Distance(currentPos, newPos) < hexSize * 0.1f)
    {
        newPos = GetRandomPositionInTile(q, r, hexSize, margin);
    }
    
    return newPos;
}
```

### UnitController.cs

#### 타일 내부 이동
```csharp
public void MoveWithinCurrentTile()
{
    if (agent == null) return;
    
    // 새 위치 계산
    Vector3 newPos = TileHelper.GetRandomPositionInCurrentTile(
        transform.position, 
        agent.q, 
        agent.r, 
        agent.hexSize,
        0.15f // 마진 15%
    );
    
    // 부드럽게 이동
    StartCoroutine(MoveWithinTileCoroutine(newPos));
}
```

#### 타일 내부 이동 코루틴
```csharp
IEnumerator MoveWithinTileCoroutine(Vector3 targetPos)
{
    isMoving = true;
    Vector3 startPos = transform.position;
    
    // 빠르게 이동 (moveSpeed * 2)
    float distance = Vector3.Distance(startPos, targetPos);
    float travelTime = distance / (moveSpeed * hexSize * 2f);
    
    // Lerp 이동
    float elapsed = 0f;
    while (elapsed < travelTime)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / travelTime);
        transform.position = Vector3.Lerp(startPos, targetPos, t);
        yield return null;
    }
    
    transform.position = targetPos;
    isMoving = false;
}
```

---

## ?? 시각화

### 타일 내부 분산

```
[이전: 모두 겹침]
    ??
    ??????  ← 3마리가 같은 위치

[현재: 분산 배치]
    ??
   ?? ??
    ??     ← 타일 내부 분산 ?
```

### 전투 시 회피

```
[타일 (5, 3) 내부]

    ┌─────────┐
    │  ??     │ ← 회피 이동
    │    ??   │ ← 전투 중
    │     ??  │ ← 회피 이동
    └─────────┘
    
타일은 (5, 3) 유지
World 위치만 변경 ?
```

### Idle 상태 이동

```
[타일 (2, 1) 내부]

T=0초:
    ┌─────────┐
    │    ??   │
    │         │
    └─────────┘

T=5초:
    ┌─────────┐
    │  ??     │ ← 이동
    │         │
    └─────────┘

T=10초:
    ┌─────────┐
    │         │
    │     ??  │ ← 이동
    └─────────┘
```

---

## ?? 설정 조절

### 마진 조절
```csharp
// 타일 경계에 가까이
GetRandomPositionInTile(q, r, hexSize, 0.1f); // 마진 10%

// 타일 중심 쪽
GetRandomPositionInTile(q, r, hexSize, 0.3f); // 마진 30%

// 권장 (기본)
GetRandomPositionInTile(q, r, hexSize, 0.15f); // 마진 15% ?
```

### Idle 이동 주기
```csharp
// 빠른 이동
idleMovementInterval = 3f; // 3초마다

// 표준 (권장)
idleMovementInterval = 5f; // 5초마다 ?

// 느린 이동
idleMovementInterval = 10f; // 10초마다
```

### 회피 이동 속도
```csharp
// 타일 내부 이동 시
float travelTime = distance / (moveSpeed * hexSize * 2f);
                                                  // ↑
                                              빠르게 (x2)

// 더 빠르게
float travelTime = distance / (moveSpeed * hexSize * 3f); // x3

// 더 느리게
float travelTime = distance / (moveSpeed * hexSize * 1.5f); // x1.5
```

---

## ?? 비교표

### 유닛 배치

| 상황 | 이전 | 현재 |
|------|------|------|
| **생성 위치** | 타일 중심 (겹침) | 타일 내부 분산 ? |
| **Idle 상태** | 정적 | 5초마다 이동 ? |
| **자원 채취** | 타일 중심 | 타일 내부 랜덤 ? |
| **가시성** | 겹쳐서 안 보임 ? | 분산되어 보임 ? |

### 회피 행동

| 요소 | 이전 | 현재 |
|------|------|------|
| **이동 범위** | 다른 타일로 | 타일 내부만 ? |
| **타일 좌표** | 변경 | 유지 ? |
| **World 좌표** | 변경 | 변경 ? |
| **전술성** | 도망가기 | 회피하기 ? |

---

## ?? 게임 플레이 개선

### 변경 전
```
? 유닛들이 겹쳐서 안 보임
? 정적인 대기 상태
? 회피 시 다른 타일로 도망
? 전투가 지저분함
```

### 변경 후
```
? 유닛들이 분산되어 보임
? 자연스러운 움직임
? 회피 시 타일 내부에서만 이동
? 깔끔한 전투
```

---

## ?? 활용 예시

### 1. 유닛 집결 시
```csharp
// 10마리 집결해도 분산 배치
for (int i = 0; i < 10; i++)
{
    Vector3 pos = TileHelper.GetRandomPositionInTile(q, r, 0.5f);
    Instantiate(unitPrefab, pos, Quaternion.identity);
}
```

### 2. 대형 유지
```csharp
// 대형을 유지하면서 약간의 변화
public void MaintainFormation()
{
    // 5초마다 대형 내 위치 미세 조정
    if (Time.time - lastFormationAdjust > 5f)
    {
        controller.MoveWithinCurrentTile();
        lastFormationAdjust = Time.time;
    }
}
```

### 3. 자연스러운 NPC
```csharp
// NPC가 자연스럽게 움직임
void Update()
{
    if (isIdle && Random.value < 0.01f) // 1% 확률
    {
        controller.MoveWithinCurrentTile();
    }
}
```

---

## ?? 문제 해결

### Q: 유닛이 타일 밖으로 나가요

**원인:**
```
margin 값이 너무 작음
```

**해결:**
```csharp
// margin 증가
GetRandomPositionInTile(q, r, hexSize, 0.2f); // 20%
```

### Q: 유닛이 너무 빨리 움직여요

**해결:**
```csharp
// 이동 속도 조절
float travelTime = distance / (moveSpeed * hexSize * 1.5f); // 느리게
```

### Q: Idle 이동이 너무 자주 일어나요

**해결:**
```csharp
// 간격 증가
idleMovementInterval = 10f; // 10초
```

---

## ? 테스트 체크리스트

```
[ ] 유닛 생성 시 분산 배치
[ ] Idle 상태 시 5초마다 이동
[ ] 공격 쿨타임 시 타일 내부 회피
[ ] 자원 채취 시 타일 내부 배치
[ ] 타일 좌표는 변하지 않음
[ ] World 좌표만 변경됨
[ ] 유닛들이 겹치지 않음
```

---

## ?? 완료!

**핵심 기능:**
- ? 타일 내부 랜덤 위치 시스템
- ? 회피 행동 타일 내부 이동
- ? Idle 상태 자연스러운 움직임
- ? 유닛 분산 배치

**게임 플레이:**
- 유닛들이 겹치지 않음
- 자연스러운 움직임
- 전술적 회피
- 시각적 품질 향상

게임 개발 화이팅! ??????
