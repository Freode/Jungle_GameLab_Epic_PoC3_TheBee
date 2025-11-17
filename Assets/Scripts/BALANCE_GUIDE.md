# ?? 게임 밸런스 조절 가이드

## 완료된 개선사항

### 1. ? 말벌 생성 시 위치 숨김
- Enemy 생성 즉시 가시성 체크
- 플레이어 시야 밖이면 즉시 숨김
- 말벌집 위치 추측 방지

### 2. ? 꿀벌 Idle 상태 자동 교전
- 0.5초마다 주변 적 감지
- 같은 타일 적 발견 시 즉시 교전
- 주변 1칸 적 발견 시 이동 후 교전

### 3. ? 업그레이드 수치 조절 위치 안내
- HiveManager.cs 파일
- 모든 기본 수치 및 업그레이드 배율
- 손쉬운 밸런스 조절

---

## ?? 수정된 파일

### 1. Hive.cs
```csharp
? HideEnemyOnSpawn() 코루틴 추가
   - Enemy 생성 즉시 가시성 체크
   
? SpawnWorker() 수정
   - Enemy 생성 시 즉시 ForceUpdateVisibility() 호출
```

### 2. EnemyVisibilityController.cs
```csharp
? ForceUpdateVisibility() 메서드 추가
   - 즉시 가시성 업데이트 실행
```

### 3. UnitBehaviorController.cs
```csharp
? DetectNearbyEnemies() 추가
   - 0.5초마다 주변 적 감지
   
? FindNearbyEnemy() 추가
   - 범위 내 적 찾기
```

---

## ?? 업그레이드 수치 조절 위치

### ?? 파일 위치
```
Assets/Scripts/Manager/HiveManager.cs
```

### ?? 기본 수치 (라인 286-308)

```csharp
// ========== 현재 값 가져오기 메서드 ==========

public int GetWorkerAttack()
{
    return 10 + workerAttackLevel;  // 기본 10, 레벨당 +1
}

public int GetWorkerMaxHealth()
{
    return 50 + (workerHealthLevel * 5);  // 기본 50, 레벨당 +5
}

public float GetWorkerSpeed()
{
    return 2.0f + workerSpeedLevel;  // 기본 2.0, 레벨당 +1.0
}

public int GetHiveMaxHealth()
{
    return 200 + (hiveHealthLevel * 30);  // 기본 200, 레벨당 +30
}

public int GetMaxWorkers()
{
    return 10 + (maxWorkersLevel * 5);  // 기본 10, 레벨당 +5
}

public int GetGatherAmount()
{
    return 1 + (gatherAmountLevel * 2);  // 기본 1, 레벨당 +2
}
```

---

## ?? 업그레이드 비용 조절

### ?? 파일 위치
```
Assets/ScriptableObjects/Commands/Upgrades/
```

각 업그레이드 에셋의 Inspector에서 `Resource Cost` 값 수정

### 현재 비용

| 업그레이드 | 파일명 | 현재 비용 |
|-----------|--------|----------|
| 활동 범위 확장 | Upgrade_HiveRange.asset | 20 |
| 날카로운 침 | Upgrade_WorkerAttack.asset | 15 |
| 강화 외골격 | Upgrade_WorkerHealth.asset | 20 |
| 빠른 날개 | Upgrade_WorkerSpeed.asset | 15 |
| 강화 성벽 | Upgrade_HiveHealth.asset | 25 |
| 확장 벌집 | Upgrade_MaxWorkers.asset | 30 |
| 효율적 채집 | Upgrade_GatherAmount.asset | 20 |

---

## ?? 밸런스 조절 가이드

### 1. 일꾼 공격력 조절

```csharp
// HiveManager.cs - Line 286
public int GetWorkerAttack()
{
    return 10 + workerAttackLevel;  // ← 여기 수정
}
```

#### 추천 설정

| 난이도 | 기본값 | 증가량 |
|--------|--------|--------|
| 쉬움 | 15 | +2 |
| 보통 | 10 | +1 ? |
| 어려움 | 8 | +1 |
| 매우 어려움 | 5 | +1 |

**예시:**
```csharp
// 쉬움
return 15 + (workerAttackLevel * 2);

// 어려움
return 8 + workerAttackLevel;
```

---

### 2. 일꾼 체력 조절

```csharp
// HiveManager.cs - Line 291
public int GetWorkerMaxHealth()
{
    return 50 + (workerHealthLevel * 5);  // ← 여기 수정
}
```

#### 추천 설정

| 난이도 | 기본 체력 | 증가량 |
|--------|----------|--------|
| 쉬움 | 80 | +10 |
| 보통 | 50 | +5 ? |
| 어려움 | 40 | +5 |
| 매우 어려움 | 30 | +3 |

---

### 3. 이동 속도 조절

```csharp
// HiveManager.cs - Line 296
public float GetWorkerSpeed()
{
    return 2.0f + workerSpeedLevel;  // ← 여기 수정
}
```

#### 추천 설정

| 난이도 | 기본 속도 | 증가량 |
|--------|----------|--------|
| 쉬움 | 3.0f | +1.5f |
| 보통 | 2.0f | +1.0f ? |
| 어려움 | 1.5f | +0.5f |

---

### 4. 하이브 체력 조절

```csharp
// HiveManager.cs - Line 301
public int GetHiveMaxHealth()
{
    return 200 + (hiveHealthLevel * 30);  // ← 여기 수정
}
```

#### 추천 설정

| 난이도 | 기본 체력 | 증가량 |
|--------|----------|--------|
| 쉬움 | 300 | +50 |
| 보통 | 200 | +30 ? |
| 어려움 | 150 | +20 |
| 매우 어려움 | 100 | +15 |

---

### 5. 최대 일꾼 수 조절

```csharp
// HiveManager.cs - Line 306
public int GetMaxWorkers()
{
    return 10 + (maxWorkersLevel * 5);  // ← 여기 수정
}
```

#### 추천 설정

| 난이도 | 기본 인구 | 증가량 |
|--------|----------|--------|
| 쉬움 | 15 | +8 |
| 보통 | 10 | +5 ? |
| 어려움 | 8 | +3 |

---

### 6. 자원 채집량 조절

```csharp
// HiveManager.cs - Line 311
public int GetGatherAmount()
{
    return 1 + (gatherAmountLevel * 2);  // ← 여기 수정
}
```

#### 추천 설정

| 난이도 | 기본 채집 | 증가량 |
|--------|----------|--------|
| 쉬움 | 3 | +3 |
| 보통 | 1 | +2 ? |
| 어려움 | 1 | +1 |

---

## ?? 업그레이드 비용 조절

### Inspector에서 조절

```
1. Project 창에서 업그레이드 에셋 찾기
   Assets/ScriptableObjects/Commands/Upgrades/

2. 에셋 클릭

3. Inspector에서 "Resource Cost" 수정

4. 저장 (Ctrl+S)
```

### 추천 비용 (난이도별)

| 업그레이드 | 쉬움 | 보통 | 어려움 |
|-----------|------|------|--------|
| **하이브 범위** | 10 | 20 ? | 30 |
| **일꾼 공격** | 10 | 15 ? | 25 |
| **일꾼 체력** | 15 | 20 ? | 30 |
| **이동 속도** | 10 | 15 ? | 25 |
| **하이브 체력** | 15 | 25 ? | 40 |
| **최대 일꾼** | 20 | 30 ? | 50 |
| **채집량** | 15 | 20 ? | 35 |

---

## ?? 공격 쿨타임 조절

### CombatUnit.cs

```csharp
// Assets/Scripts/Units/CombatUnit.cs - Line 8
[Header("공격 쿨타임")]
public float attackCooldown = 2f; // ← 여기 수정
```

#### 추천 설정

| 난이도 | 쿨타임 | 효과 |
|--------|--------|------|
| 쉬움 | 1.0f | 빠른 공격 |
| 보통 | 2.0f ? | 표준 |
| 어려움 | 3.0f | 느린 공격 |

---

## ?? 말벌 AI 설정

### Hive.cs

```csharp
// Assets/Scripts/Hive/Hive.cs - Line 86-89
// AI 설정 (말벌 전용)
enemyAI.visionRange = 1;        // 시야 범위
enemyAI.activityRange = 3;       // 활동 범위
enemyAI.attackRange = 0;         // 공격 범위
```

#### 추천 설정

| 난이도 | 시야 | 활동 | 효과 |
|--------|------|------|------|
| 쉬움 | 1 | 2 | 방어적 |
| 보통 | 1 | 3 ? | 표준 |
| 어려움 | 2 | 5 | 공격적 |
| 매우 어려움 | 3 | 7 | 매우 공격적 |

---

## ?? 밸런스 시트

### 전체 난이도 설정

#### 쉬움
```csharp
// 일꾼
GetWorkerAttack()      → 15 + (level * 2)
GetWorkerMaxHealth()   → 80 + (level * 10)
GetWorkerSpeed()       → 3.0f + (level * 1.5f)

// 하이브
GetHiveMaxHealth()     → 300 + (level * 50)
GetMaxWorkers()        → 15 + (level * 8)
GetGatherAmount()      → 3 + (level * 3)

// 말벌
visionRange = 1
activityRange = 2
attackCooldown = 3.0f
```

#### 보통 (현재) ?
```csharp
// 일꾼
GetWorkerAttack()      → 10 + level
GetWorkerMaxHealth()   → 50 + (level * 5)
GetWorkerSpeed()       → 2.0f + level

// 하이브
GetHiveMaxHealth()     → 200 + (level * 30)
GetMaxWorkers()        → 10 + (level * 5)
GetGatherAmount()      → 1 + (level * 2)

// 말벌
visionRange = 1
activityRange = 3
attackCooldown = 2.0f
```

#### 어려움
```csharp
// 일꾼
GetWorkerAttack()      → 8 + level
GetWorkerMaxHealth()   → 40 + (level * 5)
GetWorkerSpeed()       → 1.5f + (level * 0.5f)

// 하이브
GetHiveMaxHealth()     → 150 + (level * 20)
GetMaxWorkers()        → 8 + (level * 3)
GetGatherAmount()      → 1 + level

// 말벌
visionRange = 2
activityRange = 5
attackCooldown = 1.5f
```

---

## ?? 빠른 수정 가이드

### 게임이 너무 어려울 때

```csharp
// HiveManager.cs
GetWorkerAttack()      → 기본값 ↑ (10 → 15)
GetWorkerMaxHealth()   → 기본값 ↑ (50 → 80)
GetGatherAmount()      → 기본값 ↑ (1 → 3)

// Hive.cs
enemyAI.visionRange    → 감소 (1 유지 or 유지)
enemyAI.activityRange  → 감소 (3 → 2)
```

### 게임이 너무 쉬울 때

```csharp
// HiveManager.cs
GetWorkerAttack()      → 기본값 ↓ (10 → 8)
GetWorkerMaxHealth()   → 기본값 ↓ (50 → 40)
GetGatherAmount()      → 기본값 ↓ (1 유지)

// Hive.cs
enemyAI.visionRange    → 증가 (1 → 2)
enemyAI.activityRange  → 증가 (3 → 5)
```

---

## ?? 테스트 팁

### 빠른 테스트

```csharp
// HiveManager.cs - Start()에 추가
void Start()
{
    // 테스트용 자원 지급
    playerStoredResources = 1000;
}
```

### 치트 키 (디버그용)

```csharp
// HiveManager.cs - Update()에 추가
void Update()
{
    // F1: 자원 +100
    if (Input.GetKeyDown(KeyCode.F1))
    {
        AddResources(100);
    }
    
    // F2: 모든 업그레이드 1레벨씩
    if (Input.GetKeyDown(KeyCode.F2))
    {
        UpgradeHiveRange(0);
        UpgradeWorkerAttack(0);
        // ... 나머지도
    }
}
```

---

## ?? 문제 해결

### Q: 수치 변경했는데 반영 안 돼요

**해결:**
```
1. Unity 에디터 저장 (Ctrl+S)
2. Play 모드 종료 후 재시작
3. 기존 유닛 삭제 후 새로 생성
```

### Q: 업그레이드 비용 변경 안 돼요

**해결:**
```
1. Project 창에서 에셋 직접 클릭
2. Inspector에서 Resource Cost 수정
3. Apply 버튼 (있다면) 클릭
4. 저장 (Ctrl+S)
```

### Q: 말벌이 너무 강해요

**해결:**
```
// Hive.cs - Line 12
public float spawnInterval = 10f; // ← 증가 (10 → 20)

// CombatUnit.cs (말벌 프리팹)
public int attack = 2; // ← 감소 (2 → 1)
public int maxHealth = 10; // ← 감소 (10 → 8)
```

---

## ? 밸런스 체크리스트

```
[ ] 일꾼이 말벌 1마리를 잡을 수 있는가?
[ ] 초반 자원 채집이 원활한가?
[ ] 업그레이드 비용이 적절한가?
[ ] 하이브가 쉽게 파괴되지 않는가?
[ ] 게임 진행 속도가 적절한가?
[ ] 말벌이 너무 약하거나 강하지 않은가?
[ ] 업그레이드 효과가 체감되는가?
```

---

## ?? 완료!

**수정된 내용:**
- ? 말벌 생성 시 즉시 숨김
- ? 꿀벌 Idle 시 자동 교전
- ? 업그레이드 수치 조절 위치 안내

**밸런스 조절 파일:**
- `Assets/Scripts/Manager/HiveManager.cs` - 모든 기본 수치
- `Assets/ScriptableObjects/Commands/Upgrades/*.asset` - 업그레이드 비용
- `Assets/Scripts/Hive/Hive.cs` - 말벌 AI 설정
- `Assets/Scripts/Units/CombatUnit.cs` - 공격 쿨타임

게임 개발 화이팅! ??????
