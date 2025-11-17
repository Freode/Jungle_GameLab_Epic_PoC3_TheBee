# ?? 말벌 순찰 시스템 가이드

## 완료된 개선사항

### ? 하이브 근처 자동 순찰
- 타겟 없을 시 하이브 1칸 이내에서 랜덤 순찰
- 3초마다 새로운 위치로 이동
- 자연스러운 방어 행동

---

## ?? 수정된 파일

### 1. EnemyAI.cs
```csharp
? patrolInterval 추가 (순찰 주기)
? isPatrolling 상태 추가
? PatrolAroundHive() - 순찰 메서드
? GetRandomTileAroundHive() - 랜덤 타일 선택
? IsPatrolling() - 순찰 상태 확인
```

### 2. UnitController.cs
```csharp
? IsMoving() - 이동 중 확인 메서드
```

---

## ?? 작동 방식

### 말벌 행동 흐름

```
[말벌 생성]
   ↓
[하이브 1칸 내 대기]
   ↓
[3초 경과]
   ↓
[타겟 확인]
   ├─ 타겟 있음 → 추격 ?
   └─ 타겟 없음 → 순찰 ?
        ↓
   [하이브 1칸 내 랜덤 타일 선택]
        ↓
   [해당 타일로 이동]
        ↓
   [3초 대기]
        ↓
   [반복...]
```

### 상세 로직

#### 1. 순찰 조건 체크
```csharp
void Update()
{
    // 타겟 스캔 (1초마다)
    if (Time.time - lastScanTime >= scanInterval)
    {
        UpdateBehavior();
    }
    
    // 순찰 (3초마다)
    if (currentTarget == null && 
        !isChasing && 
        Time.time - lastPatrolTime >= patrolInterval)
    {
        PatrolAroundHive(); ?
    }
}
```

#### 2. 순찰 위치 선택
```csharp
void PatrolAroundHive()
{
    // 이미 이동 중이면 순찰 안 함
    if (controller.IsMoving()) return;
    
    // 하이브 1칸 밖이면 복귀
    if (currentDistance > 1)
    {
        ReturnToHive();
        return;
    }
    
    // 랜덤 타일로 이동
    var patrolTile = GetRandomTileAroundHive();
    // 이동 경로 설정...
}
```

#### 3. 랜덤 타일 선택
```csharp
HexTile GetRandomTileAroundHive()
{
    // 하이브 중심으로 1칸 이내 모든 타일 수집
    for (int dq = -1; dq <= 1; dq++)
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            // 큐브 좌표 제약: dq + dr + ds = 0
            int ds = -dq - dr;
            if (ds < -1 || ds > 1) continue;
            
            // 타일 추가...
        }
    }
    
    // 랜덤 선택
    return possibleTiles[Random.Range(0, count)];
}
```

---

## ?? 시나리오

### 시나리오 1: 평시 순찰

```
[말벌집] (0, 0)
[말벌 A] (0, 0) - 하이브 위

[3초 경과]
   ↓
순찰 타일 선택: (1, 0)
   ↓
이동: (0, 0) → (1, 0)
   ↓
[3초 대기]
   ↓
순찰 타일 선택: (0, 1)
   ↓
이동: (1, 0) → (0, 1)
   ↓
[3초 대기]
   ↓
순찰 타일 선택: (-1, 1)
   ↓
이동: (0, 1) → (-1, 1)
   ↓
[반복...]
```

### 시나리오 2: 적 감지 시

```
[말벌] (1, 0) - 순찰 중

[일꾼 접근]
위치: (2, 0)
   ↓
말벌 시야(1칸) 내 감지 ?
   ↓
순찰 중단, 추격 시작
   ↓
일꾼 도망 (시야 밖)
   ↓
추격 중단
   ↓
하이브 1칸 내 복귀
   ↓
순찰 재개 ?
```

### 시나리오 3: 하이브 방어

```
[말벌집] (0, 0)
[말벌 3마리] 순찰 중
- A: (0, 0)
- B: (1, 0)
- C: (0, 1)

[평시]
각자 3초마다 하이브 1칸 내에서 랜덤 이동 ?

[일꾼 접근]
   ↓
말벌 A, B, C 모두 감지
   ↓
순찰 중단, 집중 추격
   ↓
일꾼 처치 or 도망
   ↓
순찰 재개 ?
```

---

## ?? 순찰 패턴

### 하이브 1칸 이내 타일

```
      (-1, 0)
        ?
(-1, 1) ? (0, -1)
        ?
      ?? (0, 0) 하이브
        ?
 (0, 1) ? (1, -1)
        ?
      (1, 0)
```

### 순찰 예시
```
T=0초:  말벌 @ 하이브 (0, 0)
T=3초:  이동 → (1, 0)
T=6초:  이동 → (0, 1)
T=9초:  이동 → (-1, 1)
T=12초: 이동 → (0, 0)
T=15초: 이동 → (1, -1)
...
```

---

## ?? Inspector 설정

### EnemyAI 설정

```yaml
Enemy AI (Script)
├─ AI 설정
│  ├─ Vision Range: 1 (시야 범위)
│  ├─ Activity Range: 3 (활동 범위)
│  ├─ Scan Interval: 1 (타겟 탐색 주기)
│  ├─ Attack Range: 0 (공격 범위)
│  └─ Patrol Interval: 3 ? (순찰 주기)
└─ 디버그
   └─ Show Debug Logs: false
```

### 순찰 주기 조절

```csharp
// 빠른 순찰 (활발)
patrolInterval = 2f;

// 표준 순찰 (권장)
patrolInterval = 3f; ?

// 느린 순찰 (방어적)
patrolInterval = 5f;
```

---

## ?? 코드 구조

### 핵심 메서드

#### 1. PatrolAroundHive()
```csharp
void PatrolAroundHive()
{
    // 이동 중 체크
    if (controller.IsMoving()) return;
    
    // 위치 확인
    if (currentDistance > 1)
    {
        ReturnToHive();
        return;
    }
    
    // 랜덤 타일 선택
    var patrolTile = GetRandomTileAroundHive();
    
    // 이동 실행
    controller.SetPath(path);
    isPatrolling = true;
}
```

#### 2. GetRandomTileAroundHive()
```csharp
HexTile GetRandomTileAroundHive()
{
    var possibleTiles = new List<HexTile>();
    
    // 하이브 1칸 내 모든 타일 수집
    for (int dq = -1; dq <= 1; dq++)
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            // 큐브 좌표 제약
            int ds = -dq - dr;
            if (ds < -1 || ds > 1) continue;
            
            // 현재 위치 제외
            if (tq == agent.q && tr == agent.r) continue;
            
            // 타일 추가
            possibleTiles.Add(tile);
        }
    }
    
    // 랜덤 선택
    return possibleTiles[Random.Range(0, count)];
}
```

#### 3. IsMoving() (UnitController)
```csharp
public bool IsMoving()
{
    // 이동 중이거나 대기 중인 경로가 있으면 true
    return isMoving || pathQueue.Count > 0;
}
```

---

## ?? 비교표

### 이전 vs 현재

| 상황 | 이전 | 현재 |
|------|------|------|
| **타겟 없음** | 제자리 대기 | 순찰 ? |
| **하이브 근처** | 가만히 있음 | 1칸 내 이동 ? |
| **적 감지** | 추격 | 추격 (동일) |
| **추격 중단** | 하이브 복귀 | 복귀 후 순찰 ? |
| **자연스러움** | 정적 | 동적 ? |

### 행동 패턴

| 조건 | 이전 행동 | 현재 행동 |
|------|----------|----------|
| **평시** | 대기만 | 순찰 ? |
| **1칸 내** | 움직임 없음 | 랜덤 이동 ? |
| **타겟 발견** | 추격 | 순찰 중단 → 추격 |
| **추격 완료** | 제자리 | 순찰 재개 ? |

---

## ?? 게임 플레이 개선

### 변경 전
```
? 말벌이 가만히 있음
? 하이브 방어가 정적
? 예측 가능한 위치
? 지루한 전투
```

### 변경 후
```
? 말벌이 활발히 움직임
? 하이브 방어가 동적
? 예측 불가능한 위치
? 긴장감 있는 전투
```

---

## ?? 디버그 로그

### 순찰 로그
```
[Enemy AI] 하이브 근처 순찰: (1, 0)
[Enemy AI] 하이브 근처 순찰: (0, 1)
[Enemy AI] 하이브 근처 순찰: (-1, 1)
```

### 전투 전환 로그
```
[Enemy AI] 하이브 근처 순찰: (1, 0)
[Enemy AI] 새 타겟 발견: Worker_Bee_1
[Enemy AI] 타겟 추격 중: Worker_Bee_1
[Enemy AI] 타겟 처치!
[Enemy AI] 추격 중단
[Enemy AI] 하이브 근처 순찰: (0, 1)
```

---

## ?? 추가 개선 아이디어

### 1. 순찰 우선순위
```csharp
// 특정 타일 우선 순찰
HexTile GetStrategicPatrolTile()
{
    // 하이브 입구 방향
    // 최근 적 출현 위치
    // 자원 근처
}
```

### 2. 편대 순찰
```csharp
// 여러 말벌이 대형 유지
void FormationPatrol()
{
    // 리더 지정
    // 대형 유지
    // 동시 이동
}
```

### 3. 경계 태세
```csharp
// 적 감지 시 경계 태세
void AlertMode()
{
    patrolInterval = 1f; // 빠른 순찰
    visionRange = 2; // 시야 확대
}
```

---

## ?? 문제 해결

### Q: 말벌이 순찰을 안 해요

**확인사항:**
```
? patrolInterval > 0 확인
? homeHive 참조 확인
? controller.IsMoving() 작동 확인
? GetRandomTileAroundHive() 타일 반환 확인
```

### Q: 말벌이 계속 같은 곳만 가요

**원인:**
```
Random.Range()가 같은 값 반복
```

**해결:**
```csharp
// 이전 위치 제외
if (tile == lastPatrolTile) continue;
```

### Q: 말벌이 하이브 밖으로 나가요

**확인사항:**
```
? IsWithinHiveRange() 확인
? currentDistance <= 1 체크
? ReturnToHive() 작동 확인
```

---

## ? 테스트 체크리스트

### 순찰 기능
```
[ ] 말벌 생성 확인
[ ] 3초마다 순찰 시작
[ ] 하이브 1칸 내에서만 이동
[ ] 랜덤 위치로 이동
[ ] 타겟 발견 시 순찰 중단
[ ] 추격 완료 후 순찰 재개
```

### 행동 전환
```
[ ] 순찰 → 추격 전환
[ ] 추격 → 순찰 전환
[ ] 하이브 밖 → 복귀 → 순찰
[ ] 이동 중에는 새 순찰 안 함
```

---

## ?? 완료!

말벌의 자연스러운 순찰 시스템이 완성되었습니다!

**핵심 기능:**
- ? 하이브 1칸 내 랜덤 순찰
- ? 3초마다 새로운 위치 이동
- ? 타겟 감지 시 순찰 중단
- ? 추격 완료 후 순찰 재개
- ? 자연스러운 방어 행동

**게임 플레이:**
- 말벌이 활발히 움직임
- 예측 불가능한 위치
- 긴장감 있는 전투
- 하이브 방어 강화

게임 개발 화이팅! ??????
