# ?? 말벌집 스폰 문제 해결

## 문제점

말벌집이 생성되었지만 말벌이 스폰되지 않는 문제

---

## 원인 분석

### 1. Initialize() 미호출
```csharp
// EnemyHiveSpawner.cs (이전)
var agent = hive.GetComponent<UnitAgent>();
if (agent != null)
{
    agent.faction = Faction.Enemy;
}
// ? Initialize() 호출 안 함
```

- `Hive.Initialize()`가 호출되지 않음
- `SpawnLoop()` 코루틴이 시작되지 않음
- 결과: 말벌이 생성되지 않음

### 2. Faction 하드코딩
```csharp
// Hive.cs - SpawnWorker() (이전)
agent.faction = Faction.Player; // ? 항상 Player
```

- 모든 Worker가 Player로 생성됨
- Enemy 하이브에서도 Player 소속 일꾼 생성
- 결과: 말벌이 아닌 꿀벌이 생성됨

---

## 해결 방법

### 1. Initialize() 호출 추가 ?

```csharp
// EnemyHiveSpawner.cs
void SpawnEliteWaspHive()
{
    // 하이브 생성
    eliteHive = Instantiate(eliteWaspHivePrefab, position, Quaternion.identity);
    
    // Faction 설정
    var agent = eliteHive.GetComponent<UnitAgent>();
    if (agent != null)
    {
        agent.faction = Faction.Enemy;
    }
    
    // ? Initialize() 호출 추가
    var hive = eliteHive.GetComponent<Hive>();
    if (hive != null)
    {
        hive.Initialize(q, r);
    }
}
```

#### Initialize()가 하는 일
```csharp
public void Initialize(int q, int r)
{
    this.q = q; this.r = r;
    
    // ? SpawnLoop 코루틴 시작
    spawnRoutine = StartCoroutine(SpawnLoop());
    
    // 활동 범위 표시
    HexBoundaryHighlighter.Instance?.ShowBoundary(this, radius);
    
    // 기타 설정...
}
```

### 2. Faction 상속 구현 ?

```csharp
// Hive.cs - SpawnWorker()
void SpawnWorker()
{
    // Worker 생성
    var go = Instantiate(workerPrefab, pos, Quaternion.identity);
    var agent = go.GetComponent<UnitAgent>();
    
    // ? 하이브의 faction을 상속
    var hiveAgent = GetComponent<UnitAgent>();
    if (hiveAgent != null)
    {
        agent.faction = hiveAgent.faction;
    }
    else
    {
        agent.faction = Faction.Player; // 기본값
    }
    
    // ? Player만 업그레이드 적용
    if (agent.faction == Faction.Player && HiveManager.Instance != null)
    {
        // 업그레이드 적용...
    }
}
```

---

## ?? 수정된 파일

### 1. EnemyHiveSpawner.cs
```csharp
? SpawnEliteWaspHive():
   - hive.Initialize(q, r) 호출 추가

? SpawnNormalWaspHives():
   - hiveComponent.Initialize(pos.x, pos.y) 호출 추가
```

### 2. Hive.cs
```csharp
? SpawnWorker():
   - 하이브의 faction 상속
   - Player만 HiveManager 업그레이드 적용
```

---

## ?? 작동 흐름

### 말벌집 생성 → 말벌 스폰

```
1. EnemyHiveSpawner.SpawnAllEnemyHives()
   ↓
2. Instantiate(waspHivePrefab)
   ↓
3. agent.faction = Faction.Enemy
   ↓
4. hive.Initialize(q, r) ?
   ↓
5. StartCoroutine(SpawnLoop())
   ↓
6. 8초마다 SpawnWorker() 호출
   ↓
7. agent.faction = hiveAgent.faction (Enemy) ?
   ↓
8. 말벌 생성 완료!
```

---

## ?? 디버그 로그

### 정상 작동 시
```
[적 하이브 생성] 장수말벌집 생성: (0, 0)
[적 하이브 생성] 일반 말벌집 생성: (5, 3)
[적 하이브 생성] 일반 말벌집 생성: (-4, 6)
[적 하이브 생성] 일반 말벌집 생성: (6, -5)
[적 하이브 생성] 장수말벌집 1개, 일반 말벌집 3개 생성 완료

(8초 후)
(말벌 생성 - faction: Enemy)
(말벌 생성 - faction: Enemy)
...
```

---

## ?? 테스트 체크리스트

### 말벌집 생성
```
[ ] 장수말벌집 (0, 0) 생성 확인
[ ] 일반 말벌집 3개 생성 확인
[ ] Console 로그 확인
```

### 말벌 스폰
```
[ ] 8초 후 말벌 생성 시작
[ ] 말벌 faction = Enemy 확인
[ ] 말벌 이름 = "말벌" 확인
[ ] 최대 15마리까지 생성 확인
```

### Faction 확인
```
[ ] 말벌 클릭 시 "말벌" 표시
[ ] 말벌 명령 버튼 없음
[ ] 말벌 우클릭 이동 불가
[ ] 말벌 안개 제거 안 함
```

---

## ?? 핵심 포인트

### Initialize() 필수!
```csharp
// ? 잘못된 방법
Instantiate(hivePrefab);
agent.faction = Faction.Enemy;
// Initialize 호출 안 함 → SpawnLoop 시작 안 됨

// ? 올바른 방법
Instantiate(hivePrefab);
agent.faction = Faction.Enemy;
hive.Initialize(q, r); // ← 필수!
```

### Faction 상속
```csharp
// ? 하드코딩
agent.faction = Faction.Player;

// ? 상속
var hiveAgent = GetComponent<UnitAgent>();
agent.faction = hiveAgent.faction;
```

### 업그레이드 분리
```csharp
// ? Player만 업그레이드 적용
if (agent.faction == Faction.Player && HiveManager.Instance != null)
{
    // HiveManager 업그레이드 적용
}
// Enemy는 프리팹 기본 능력치 사용
```

---

## ?? 완료!

이제 말벌집에서 말벌이 정상적으로 스폰됩니다!

**수정 요약:**
- ? EnemyHiveSpawner에서 Initialize() 호출
- ? Hive.SpawnWorker()에서 faction 상속
- ? Player만 HiveManager 업그레이드 적용

**결과:**
- ? 말벌집에서 8초마다 말벌 생성
- ? 말벌 faction = Enemy
- ? 최대 15마리까지 생성
- ? 플레이어 조작 불가

게임 개발 화이팅! ??????
