# ?? 하이브 이사 & 웨이브 공격 시스템 가이드

## 완료된 개선사항

### 1. ? 이사 준비 시 하이브 체력 감소
- 10초 이사 준비 기간 동안 체력 지속 감소
- 체력이 1까지 감소 (최소 보장)
- 긴장감 있는 이사 메커니즘

### 2. ? 일반 말벌 웨이브 공격
- 가장 가까운 말벌집에서 80초마다 1마리 공격
- 말벌집 파괴 시 누적 공격 (1마리씩 추가)
- 자동 타겟팅 및 경로 찾기

### 3. ? 장수말벌 보스 웨이브
- 180초마다 웨이브 공격
- 웨이브 1: 2마리, 웨이브 2: 4마리, 웨이브 3: 6마리...
- 강화된 능력치 (체력 2배, 공격력 1.5배)

---

## ?? 생성/수정된 파일

### 1. WaspWaveManager.cs (신규)
```csharp
? 일반 말벌 웨이브 관리
? 장수말벌 보스 웨이브 관리
? 적 하이브 등록/해제
? 자동 공격 시스템
```

### 2. Hive.cs
```csharp
? RelocationCountdown() 수정
   - 체력 지속 감소 추가
   
? OnEnable/OnDisable 수정
   - WaspWaveManager 연동
```

---

## ?? 시스템 작동 방식

### 1. 이사 준비 시 체력 감소

```
[이사 시작]
현재 체력: 200 HP
   ↓
[10초 동안]
초당 감소: 200 / 10 = 20 HP/초
   ↓
T=0초:  200 HP
T=1초:  180 HP ↓
T=2초:  160 HP ↓
T=3초:  140 HP ↓
T=4초:  120 HP ↓
T=5초:  100 HP ↓
T=6초:  80 HP ↓
T=7초:  60 HP ↓
T=8초:  40 HP ↓
T=9초:  20 HP ↓
T=10초: 1 HP (최소 보장) ?
   ↓
[하이브 파괴]
```

### 2. 일반 말벌 웨이브

```
[게임 시작]
   ↓
80초 대기
   ↓
[1차 웨이브]
가장 가까운 말벌집 → 1마리 공격 ?
   ↓
80초 대기
   ↓
[2차 웨이브]
같은 말벌집 → 1마리 공격
   ↓
말벌집 A 파괴! ?
destroyedHivesCount = 1
   ↓
80초 대기
   ↓
[3차 웨이브]
다음 가까운 말벌집 B → 2마리 공격 ? (1 + 1)
   ↓
80초 대기
   ↓
[4차 웨이브]
말벌집 B → 2마리 공격
   ↓
말벌집 B 파괴! ?
destroyedHivesCount = 2
   ↓
80초 대기
   ↓
[5차 웨이브]
다음 가까운 말벌집 C → 3마리 공격 ? (1 + 2)
   ↓
...계속 증가
```

### 3. 장수말벌 보스 웨이브

```
[게임 시작]
   ↓
180초 대기
   ↓
[보스 웨이브 1]
장수말벌 2마리 공격 ?
- 체력 2배
- 공격력 1.5배
- 크기 1.3배
   ↓
180초 대기
   ↓
[보스 웨이브 2]
장수말벌 4마리 공격 ?
   ↓
180초 대기
   ↓
[보스 웨이브 3]
장수말벌 6마리 공격 ?
   ↓
180초 대기
   ↓
[보스 웨이브 4]
장수말벌 8마리 공격 ?
   ↓
...무한 증가
```

---

## ?? 시나리오

### 시나리오 1: 이사 중 공격받음

```
[플레이어 하이브 이사 시작]
체력: 200 HP
   ↓
T=0초: 이사 시작
       체력: 200 HP
   ↓
T=2초: 말벌 웨이브 도착!
       체력: 160 HP (감소 중)
       말벌 공격: -12 HP
       체력: 148 HP ↓
   ↓
T=5초: 체력: 100 HP (감소 중)
       말벌 공격: -12 HP
       체력: 88 HP ↓
   ↓
T=8초: 체력: 40 HP (감소 중)
       플레이어: 긴급 방어!
   ↓
T=10초: 이사 완료
        체력: 1 HP ?
        새 위치에서 재건축
```

### 시나리오 2: 말벌집 연속 파괴

```
[초기]
말벌집 A, B, C 존재
플레이어 하이브에서 거리:
- A: 5칸
- B: 8칸
- C: 12칸

[80초]
1차 웨이브: 말벌집 A → 1마리 ?

[160초]
2차 웨이브: 말벌집 A → 1마리

[플레이어가 말벌집 A 파괴]
destroyedHivesCount = 1

[240초]
3차 웨이브: 말벌집 B (다음 가까운) → 2마리 ?

[320초]
4차 웨이브: 말벌집 B → 2마리

[플레이어가 말벌집 B 파괴]
destroyedHivesCount = 2

[400초]
5차 웨이브: 말벌집 C → 3마리 ?

→ 시간이 지날수록 압박 증가!
```

### 시나리오 3: 보스 + 일반 웨이브 동시

```
[180초]
보스 웨이브 1: 장수말벌 2마리
일반 웨이브: 일반 말벌 1마리
→ 총 3마리 동시 공격! ?

[240초]
일반 웨이브: 일반 말벌 1마리

[320초]
일반 웨이브: 일반 말벌 1마리

[360초]
보스 웨이브 2: 장수말벌 4마리
일반 웨이브: 일반 말벌 1마리
→ 총 5마리 동시 공격! ?

[400초]
일반 웨이브: 일반 말벌 1마리

...점점 더 어려워짐
```

---

## ?? 코드 구조

### Hive.cs - 체력 감소

```csharp
IEnumerator RelocationCountdown()
{
    float countdown = 10f;
    var combat = GetComponent<CombatUnit>();
    int initialHealth = combat != null ? combat.health : 0;
    float healthDrainRate = initialHealth / countdown; // 초당 감소량

    while (countdown > 0f)
    {
        countdown -= Time.deltaTime;
        
        // 체력 지속 감소
        if (combat != null)
        {
            float drainAmount = healthDrainRate * Time.deltaTime;
            combat.health = Mathf.Max(1, combat.health - Mathf.CeilToInt(drainAmount));
        }

        yield return null;
    }

    DestroyHive();
}
```

### WaspWaveManager.cs - 일반 웨이브

```csharp
void LaunchNormalWaspWave()
{
    // 플레이어 하이브
    Hive playerHive = FindNearestPlayerHive();
    
    // 가장 가까운 적 하이브
    Hive nearestEnemyHive = FindNearestEnemyHive(playerHive);
    
    // 공격 수 = 기본 1 + 파괴된 수
    int waspCount = baseWaspCount + destroyedHivesCount;
    
    // 말벌 생성 및 공격
    for (int i = 0; i < waspCount; i++)
    {
        SpawnAndAttackWasp(nearestEnemyHive, playerHive, false);
    }
}
```

### WaspWaveManager.cs - 보스 웨이브

```csharp
public void LaunchBossWaspWave()
{
    Hive playerHive = FindNearestPlayerHive();
    Hive bossHive = FindBossHive();
    
    bossWaveNumber++;
    int bossCount = bossWaveNumber * 2; // 2, 4, 6, 8...
    
    for (int i = 0; i < bossCount; i++)
    {
        SpawnAndAttackWasp(bossHive, playerHive, true);
    }
}

// 장수말벌 강화
if (isBoss)
{
    combat.maxHealth *= 2; // 체력 2배
    combat.health = combat.maxHealth;
    combat.attack = Mathf.RoundToInt(combat.attack * 1.5f); // 공격력 1.5배
    transform.localScale *= 1.3f; // 크기 1.3배
}
```

---

## ?? 설정 조절

### 웨이브 간격

```csharp
// WaspWaveManager.cs
public float normalWaveInterval = 80f; // 일반 말벌
public float bossWaveInterval = 180f;  // 장수말벌

// 더 빠르게
normalWaveInterval = 60f;  // 60초
bossWaveInterval = 120f;   // 120초

// 더 느리게
normalWaveInterval = 120f; // 120초
bossWaveInterval = 300f;   // 300초
```

### 이사 시간 및 체력 감소

```csharp
// Hive.cs - RelocationCountdown()
float countdown = 10f; // 이사 시간

// 더 길게
float countdown = 15f; // 15초

// 더 짧게
float countdown = 5f;  // 5초
```

### 보스 능력치

```csharp
// WaspWaveManager.cs - SpawnAndAttackWasp()
if (isBoss)
{
    combat.maxHealth *= 2;   // 체력 배율
    combat.attack *= 1.5f;   // 공격력 배율
    transform.localScale *= 1.3f; // 크기 배율
}

// 더 강하게
combat.maxHealth *= 3;   // 3배
combat.attack *= 2f;     // 2배

// 더 약하게
combat.maxHealth *= 1.5f; // 1.5배
combat.attack *= 1.2f;    // 1.2배
```

---

## ?? Unity 설정

### 1. WaspWaveManager 생성

```
1. Hierarchy에서 빈 GameObject 생성
2. 이름: "WaspWaveManager"
3. WaspWaveManager.cs 컴포넌트 추가
4. Inspector 설정:
   - Normal Wave Interval: 80
   - Base Wasp Count: 1
   - Boss Wave Interval: 180
```

### 2. 장수말벌 하이브 설정

```
1. 적 하이브 GameObject 선택
2. 이름에 "Boss" 또는 "장수" 포함
   예: "EnemyHive_Boss"
3. 장수말벌 프리팹 설정:
   - Worker Prefab에 강화된 말벌 할당
```

### 3. 보스 웨이브 시작

```
// GameManager.cs 또는 씬 시작 시
void Start()
{
    if (WaspWaveManager.Instance != null)
    {
        WaspWaveManager.Instance.StartBossWaveRoutine();
    }
}
```

---

## ?? 난이도 조절

### 쉬움
```csharp
// WaspWaveManager.cs
normalWaveInterval = 120f;  // 120초
bossWaveInterval = 300f;    // 300초
baseWaspCount = 1;          // 1마리

// Hive.cs
float countdown = 15f;      // 15초
```

### 보통 (현재) ?
```csharp
normalWaveInterval = 80f;   // 80초
bossWaveInterval = 180f;    // 180초
baseWaspCount = 1;          // 1마리
float countdown = 10f;      // 10초
```

### 어려움
```csharp
normalWaveInterval = 60f;   // 60초
bossWaveInterval = 120f;    // 120초
baseWaspCount = 2;          // 2마리
float countdown = 7f;       // 7초
```

### 매우 어려움
```csharp
normalWaveInterval = 40f;   // 40초
bossWaveInterval = 90f;     // 90초
baseWaspCount = 3;          // 3마리
float countdown = 5f;       // 5초
```

---

## ?? 전략 팁

### 이사 준비
```
1. 주변 정리
   → 이사 전 주변 말벌 제거
   
2. 타이밍
   → 웨이브 직후가 안전
   
3. 방어 준비
   → 일꾼 배치 후 이사 시작
```

### 말벌집 파괴 순서
```
1. 가까운 것부터
   → 가장 가까운 말벌집 우선 제거
   
2. 보스 하이브 보존
   → 장수말벌은 나중에 처리
   
3. 방어선 구축
   → 파괴 전 방어 태세 준비
```

### 웨이브 대응
```
1. 일반 웨이브 (80초)
   → 소규모 방어 병력
   
2. 보스 웨이브 (180초)
   → 전체 병력 집결
   
3. 동시 웨이브
   → 우선순위: 보스 > 일반
```

---

## ?? 문제 해결

### Q: 웨이브가 안 나와요

**확인:**
```
[ ] WaspWaveManager GameObject 존재
[ ] WaspWaveManager.Instance != null
[ ] 적 하이브 존재
[ ] 플레이어 하이브 존재
[ ] StartBossWaveRoutine() 호출
```

### Q: 보스가 안 나와요

**확인:**
```
[ ] 하이브 이름에 "Boss" 또는 "장수" 포함
[ ] StartBossWaveRoutine() 호출됨
[ ] bossWaveInterval 경과
```

### Q: 말벌이 공격 안 해요

**확인:**
```
[ ] EnemyAI 컴포넌트 존재
[ ] visionRange, activityRange 설정
[ ] Pathfinder 작동
[ ] TileManager 존재
```

---

## ? 테스트 체크리스트

### 이사 시스템
```
[ ] 이사 시작 시 체력 감소 시작
[ ] 10초 동안 지속 감소
[ ] 최소 1 HP 보장
[ ] 10초 후 하이브 파괴
```

### 일반 웨이브
```
[ ] 80초마다 1마리 공격
[ ] 가장 가까운 말벌집에서 출발
[ ] 말벌집 파괴 시 누적 증가
[ ] 플레이어 하이브로 자동 이동
```

### 보스 웨이브
```
[ ] 180초마다 공격
[ ] 웨이브 1: 2마리
[ ] 웨이브 2: 4마리
[ ] 웨이브 3: 6마리
[ ] 강화된 능력치 (2배/1.5배)
```

---

## ?? 완료!

**핵심 기능:**
- ? 이사 준비 시 체력 지속 감소
- ? 일반 말벌 80초 웨이브 (누적)
- ? 장수말벌 180초 웨이브 (증가)
- ? 자동 타겟팅 및 공격

**게임 플레이:**
- 긴장감 있는 이사
- 지속적인 압박
- 전략적 방어
- 점점 어려워지는 난이도

게임 개발 화이팅! ??????
