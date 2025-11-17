# ?? 적 하이브 전용 클래스 분리 가이드

## 완료된 개선사항

### ? EnemyHive 클래스 생성
- 플레이어 Hive와 완전 분리
- 말벌 전용 기능 구현
- 시야 시스템 최적화
- 단순화된 구조

### ? EnemyAI 연동
- EnemyHive 타입으로 변경
- FindNearestEnemyHive() 구현
- 하이브 위치 기반 활동 범위

### ? WaspWaveManager 최적화
- EnemyHive 타입으로 변경
- 플레이어 Hive 분리
- 웨이브 공격 시스템 개선

### ? CombatUnit.Die() 개선
- EnemyHive 파괴 처리
- 플레이어 Hive와 분리

---

## ?? 생성/수정된 파일

### 1. EnemyHive.cs (신규 생성)
```csharp
? 적 말벌집 전용 클래스
   - q, r 위치
   - waspPrefab (말벌 프리팹)
   - spawnInterval, maxWasps
   - visionRange, activityRange
   - Initialize(q, r, maxHealth)
   - SpawnWasp()
   - DestroyHive()
   - GetWasps(), GetWaspCount()
```

### 2. EnemyAI.cs
```csharp
? homeHive 타입 변경
   - Hive → EnemyHive
   
? FindNearestEnemyHive() 추가
   - 가장 가까운 EnemyHive 찾기
   
? Start() 수정
   - 홈 하이브 자동 설정
```

### 3. WaspWaveManager.cs
```csharp
? enemyHives 타입 변경
   - List<Hive> → List<EnemyHive>
   
? RegisterEnemyHive(EnemyHive)
? UnregisterEnemyHive(EnemyHive)
? FindNearestEnemyHive(Hive) → EnemyHive
? FindBossHive() → EnemyHive
? SpawnAndAttackWasp(EnemyHive, Hive, bool)
```

### 4. CombatUnit.cs
```csharp
? Die() 수정
   - Hive 체크
   - EnemyHive 체크
   - 각각 DestroyHive() 호출
```

### 5. EnemyVisibilityController.cs
```csharp
? UpdateEnemyVisibility() 수정
   - playerHive 체크
   - enemyHive 체크
   - 둘 다 하이브로 처리
```

### 6. Hive.cs
```csharp
? OnEnable() 수정
   - WaspWaveManager 호출 제거
   
? OnDisable() 수정
   - WaspWaveManager 호출 제거
```

---

## ?? 시스템 작동 방식

### 1. 클래스 분리 전 (이전)

```
[하이브 생성]
GameObject: Hive
├─ Hive 컴포넌트
│  ├─ workerPrefab (꿀벌 또는 말벌) ?
│  ├─ queenBee (플레이어 전용) ?
│  ├─ isRelocating (플레이어 전용) ?
│  └─ hiveCommands (플레이어 전용) ?
└─ UnitAgent (faction: Player 또는 Enemy)

[문제점]
1. 플레이어/적 기능이 혼재 ?
2. 불필요한 변수 포함 ?
3. 코드 복잡도 증가 ?
4. 버그 발생 위험 ?
```

---

### 2. 클래스 분리 후 (현재)

```
[플레이어 하이브]
GameObject: PlayerHive
├─ Hive 컴포넌트 ?
│  ├─ workerPrefab (꿀벌) ?
│  ├─ queenBee ?
│  ├─ isRelocating ?
│  ├─ hiveCommands ?
│  └─ relocateTimerText ?
└─ UnitAgent (faction: Player)

[적 하이브]
GameObject: EnemyHive
├─ EnemyHive 컴포넌트 ?
│  ├─ waspPrefab (말벌) ?
│  ├─ visionRange ?
│  ├─ activityRange ?
│  └─ showDebugLogs ?
└─ UnitAgent (faction: Enemy)

[장점]
1. 플레이어/적 완전 분리 ?
2. 필요한 변수만 포함 ?
3. 코드 간결화 ?
4. 버그 방지 ?
```

---

## ?? 핵심 코드

### 1. EnemyHive.cs (신규)

```csharp
// Assets\Scripts\Hive\EnemyHive.cs
public class EnemyHive : MonoBehaviour
{
    [Header("위치")]
    public int q;
    public int r;

    [Header("생성 설정")]
    public GameObject waspPrefab; // 말벌 프리팹 ?
    public float spawnInterval = 8f;
    public int maxWasps = 15;

    [Header("전투 설정")]
    public int visionRange = 5; // 말벌집 시야 범위 ?
    public int activityRange = 5; // 말벌 활동 범위 ?

    private List<UnitAgent> wasps = new List<UnitAgent>();
    private UnitAgent hiveAgent;
    private CombatUnit combat;

    /// <summary>
    /// 말벌집 초기화 ?
    /// </summary>
    public void Initialize(int q, int r, int maxHealth = 250)
    {
        this.q = q;
        this.r = r;

        // UnitAgent 설정
        hiveAgent.SetPosition(q, r);
        hiveAgent.faction = Faction.Enemy;
        hiveAgent.visionRange = visionRange;
        hiveAgent.canMove = false;

        // CombatUnit 설정
        combat.maxHealth = maxHealth;
        combat.health = maxHealth;
        combat.attack = 0; // 하이브는 공격 안 함

        // 월드 위치 설정
        Vector3 worldPos = TileHelper.HexToWorld(q, r, 0.5f);
        transform.position = worldPos;
    }

    /// <summary>
    /// 말벌 생성 ?
    /// </summary>
    void SpawnWasp()
    {
        if (waspPrefab == null) return;
        if (wasps.Count >= maxWasps) return;

        // 타일 내부 랜덤 위치
        Vector3 spawnPos = TileHelper.GetRandomPositionInTile(q, r, 0.5f, 0.15f);

        // 말벌 생성
        GameObject waspObj = Instantiate(waspPrefab, spawnPos, Quaternion.identity);
        var waspAgent = waspObj.GetComponent<UnitAgent>();
        
        waspAgent.SetPosition(q, r);
        waspAgent.faction = Faction.Enemy;
        waspAgent.homeHive = null; // EnemyHive는 Hive가 아니므로 null ?

        // 말벌 리스트에 추가
        wasps.Add(waspAgent);

        // EnemyAI 설정
        var enemyAI = waspObj.GetComponent<EnemyAI>();
        enemyAI.visionRange = 3;
        enemyAI.activityRange = activityRange;
        enemyAI.attackRange = 0;
    }

    /// <summary>
    /// 말벌집 파괴 ?
    /// </summary>
    public void DestroyHive()
    {
        // WaspWaveManager에서 등록 해제
        if (WaspWaveManager.Instance != null)
        {
            WaspWaveManager.Instance.UnregisterEnemyHive(this);
        }

        // GameObject 파괴
        Destroy(gameObject);
    }
}
```

---

### 2. EnemyAI.cs 수정

```csharp
// Assets\Scripts\AI\EnemyAI.cs
public class EnemyAI : MonoBehaviour
{
    private EnemyHive homeHive; // Hive → EnemyHive ?

    void Start()
    {
        // 홈 하이브 찾기 (가장 가까운 EnemyHive) ?
        if (agent != null && agent.faction == Faction.Enemy)
        {
            homeHive = FindNearestEnemyHive();
        }
    }

    /// <summary>
    /// 가장 가까운 EnemyHive 찾기 ?
    /// </summary>
    EnemyHive FindNearestEnemyHive()
    {
        var allHives = FindObjectsOfType<EnemyHive>();
        EnemyHive nearest = null;
        float minDistance = float.MaxValue;

        Vector3 myPos = transform.position;

        foreach (var hive in allHives)
        {
            Vector3 hivePos = TileHelper.HexToWorld(hive.q, hive.r, 0.5f);
            float distance = Vector3.Distance(myPos, hivePos);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = hive;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 하이브 활동 범위 내에 있는지 확인 ?
    /// </summary>
    bool IsWithinHiveRange(int q, int r)
    {
        if (homeHive == null) return true;

        int distance = GetDistance(homeHive.q, homeHive.r, q, r);
        
        if (distance <= 1)
            return true;

        return distance <= activityRange;
    }
}
```

---

### 3. WaspWaveManager.cs 수정

```csharp
// Assets\Scripts\Manager\WaspWaveManager.cs
public class WaspWaveManager : MonoBehaviour
{
    private List<EnemyHive> enemyHives = new List<EnemyHive>(); // Hive → EnemyHive ?

    /// <summary>
    /// 적 하이브 등록 (EnemyHive 타입) ?
    /// </summary>
    public void RegisterEnemyHive(EnemyHive hive)
    {
        if (hive == null || enemyHives.Contains(hive)) return;
        
        enemyHives.Add(hive);
        Debug.Log($"[웨이브] 적 하이브 등록: {hive.name} at ({hive.q}, {hive.r})");
    }

    /// <summary>
    /// 적 하이브 제거 (파괴 시) ?
    /// </summary>
    public void UnregisterEnemyHive(EnemyHive hive)
    {
        if (enemyHives.Contains(hive))
        {
            enemyHives.Remove(hive);
            destroyedHivesCount++;
        }
    }

    /// <summary>
    /// 플레이어 하이브에서 가장 가까운 적 하이브 찾기 ?
    /// </summary>
    EnemyHive FindNearestEnemyHive(Hive playerHive)
    {
        enemyHives.RemoveAll(h => h == null);

        EnemyHive nearest = null;
        int minDist = int.MaxValue;

        foreach (var enemyHive in enemyHives)
        {
            int dist = Pathfinder.AxialDistance(playerHive.q, playerHive.r, enemyHive.q, enemyHive.r);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemyHive;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 말벌 생성 및 공격 명령 ?
    /// </summary>
    void SpawnAndAttackWasp(EnemyHive fromHive, Hive targetHive, bool isBoss)
    {
        GameObject waspPrefab = fromHive.waspPrefab; // workerPrefab → waspPrefab ?
        
        // 생성...
        
        enemyAI.activityRange = isBoss ? 10 : fromHive.activityRange; // ?
    }
}
```

---

### 4. CombatUnit.Die() 수정

```csharp
// Assets\Scripts\Units\CombatUnit.cs
void Die()
{
    // 플레이어 하이브인 경우
    var hive = GetComponent<Hive>();
    if (hive != null)
    {
        // 리플렉션으로 DestroyHive() 호출
        var destroyMethod = typeof(Hive).GetMethod("DestroyHive", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (destroyMethod != null)
        {
            destroyMethod.Invoke(hive, null);
        }
    }
    // 적 하이브인 경우 ?
    else
    {
        var enemyHive = GetComponent<EnemyHive>();
        if (enemyHive != null)
        {
            enemyHive.DestroyHive(); // public 메서드 직접 호출 ?
        }
        else
        {
            // 일반 유닛은 바로 파괴
            Destroy(gameObject);
        }
    }
}
```

---

## ?? 비교표

### 클래스 구조

| 항목 | 이전 (Hive) | 현재 (분리) |
|------|-------------|-------------|
| **플레이어 하이브** | Hive | Hive ? |
| **적 하이브** | Hive ? | EnemyHive ? |
| **여왕벌 참조** | Hive.queenBee | Hive.queenBee ? |
| **이사 기능** | Hive.isRelocating | Hive.isRelocating ? |
| **말벌 프리팹** | Hive.workerPrefab ? | EnemyHive.waspPrefab ? |
| **시야 범위** | UnitAgent.visionRange | EnemyHive.visionRange ? |
| **활동 범위** | 없음 ? | EnemyHive.activityRange ? |

### 기능 분리

| 기능 | Hive | EnemyHive |
|------|------|-----------|
| **꿀벌 생성** | ? | ? |
| **말벌 생성** | ? | ? |
| **여왕벌 관리** | ? | ? |
| **하이브 이사** | ? | ? |
| **하이브 명령** | ? | ? |
| **일꾼 관리** | ? | ? |
| **경계선 표시** | ? | ? |
| **시야 범위** | ? | ? |
| **활동 범위** | ? | ? |

---

## ?? 시각화

### 클래스 관계도

```
[플레이어 영역]
Hive
├─ queenBee (UnitAgent)
├─ workerPrefab (꿀벌)
├─ workers (List<UnitAgent>)
├─ isRelocating
├─ hiveCommands
└─ relocateTimerText

HiveManager
├─ playerHives (List<Hive>)
└─ playerStoredResources

[적 영역]
EnemyHive
├─ waspPrefab (말벌) ?
├─ wasps (List<UnitAgent>) ?
├─ visionRange ?
├─ activityRange ?
└─ showDebugLogs ?

WaspWaveManager
├─ enemyHives (List<EnemyHive>) ?
└─ destroyedHivesCount

[공통]
UnitAgent
├─ faction (Player/Enemy)
├─ homeHive (Hive, EnemyHive는 null)
└─ visionRange

CombatUnit
├─ Die()
│  ├─ Hive → DestroyHive()
│  └─ EnemyHive → DestroyHive()
```

---

### 생성 흐름

```
[플레이어 하이브 생성]
ConstructHiveHandler
   ↓
Instantiate(hivePrefab)
   ↓
Hive.Initialize(q, r)
   ↓
SpawnWorker() (꿀벌)
   ↓
HiveManager.RegisterHive(hive)

[적 하이브 생성]
EnemyHiveSpawner
   ↓
Instantiate(enemyHivePrefab)
   ↓
EnemyHive.Initialize(q, r, maxHealth)
   ↓
SpawnWasp() (말벌)
   ↓
WaspWaveManager.RegisterEnemyHive(enemyHive) ?
```

---

## ?? 문제 해결

### Q: EnemyHive가 생성되지 않아요

**확인:**
```
[ ] EnemyHiveSpawner 존재
[ ] enemyHivePrefab 할당
[ ] EnemyHive 컴포넌트 부착
[ ] Initialize() 호출
```

**해결:**
```
1. EnemyHiveSpawner.cs 확인
2. enemyHivePrefab에 EnemyHive 컴포넌트 확인
3. Initialize(q, r, maxHealth) 호출 확인
```

---

### Q: 말벌이 홈 하이브를 찾지 못해요

**확인:**
```
[ ] EnemyAI.FindNearestEnemyHive() 실행
[ ] FindObjectsOfType<EnemyHive>() 결과
[ ] homeHive != null
```

**해결:**
```
1. Console 로그 확인
   - "[Enemy AI] 홈 하이브 설정"
2. EnemyHive가 씬에 존재하는지 확인
3. EnemyAI.showDebugLogs = true 설정
```

---

### Q: WaspWaveManager가 EnemyHive를 등록 못 해요

**확인:**
```
[ ] EnemyHive.OnEnable() 실행
[ ] WaspWaveManager.Instance != null
[ ] RegisterEnemyHive(this) 호출
```

**해결:**
```
1. Console 로그 확인
   - "[웨이브] 적 하이브 등록"
2. WaspWaveManager가 씬에 있는지 확인
3. EnemyHive.OnEnable() 호출 확인
```

---

## ?? 추가 개선 아이디어

### 1. EnemyHive 프리팹 생성

```
Unity Editor:
1. GameObject → Create Empty
2. 이름: "EnemyHive"
3. 컴포넌트 추가:
   - EnemyHive
   - UnitAgent (faction: Enemy)
   - CombatUnit
   - SpriteRenderer
4. Prefab 저장
5. EnemyHiveSpawner에 할당
```

---

### 2. 하이브 타입별 설정

```csharp
// EnemyHiveConfig.cs (신규)
[CreateAssetMenu(fileName = "EnemyHiveConfig", menuName = "Config/EnemyHiveConfig")]
public class EnemyHiveConfig : ScriptableObject
{
    [Header("말벌집 설정")]
    public int maxHealth = 250;
    public int visionRange = 5;
    public int activityRange = 5;
    public float spawnInterval = 8f;
    public int maxWasps = 15;
    
    [Header("말벌 설정")]
    public GameObject waspPrefab;
    public int waspHealth = 60;
    public int waspAttack = 12;
}

// EnemyHive.cs 수정
public class EnemyHive : MonoBehaviour
{
    public EnemyHiveConfig config; // ?
    
    public void Initialize(int q, int r)
    {
        if (config != null)
        {
            combat.maxHealth = config.maxHealth;
            visionRange = config.visionRange;
            activityRange = config.activityRange;
            // ...
        }
    }
}
```

---

### 3. 하이브 파괴 이펙트

```csharp
// EnemyHive.cs 수정
public class EnemyHive : MonoBehaviour
{
    [Header("이펙트")]
    public GameObject destroyParticles; // 파괴 파티클 ?
    public AudioClip destroySound; // 파괴 사운드 ?
    
    public void DestroyHive()
    {
        // 파괴 이펙트 ?
        if (destroyParticles != null)
        {
            var particles = Instantiate(destroyParticles, transform.position, Quaternion.identity);
            Destroy(particles, 2f);
        }
        
        // 파괴 사운드 ?
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }
        
        // 카메라 흔들림 ?
        CameraShake.Instance?.Shake(0.5f, 0.3f);
        
        // WaspWaveManager에서 등록 해제
        if (WaspWaveManager.Instance != null)
        {
            WaspWaveManager.Instance.UnregisterEnemyHive(this);
        }
        
        // GameObject 파괴
        Destroy(gameObject);
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] EnemyHive 생성 확인
[ ] EnemyHive.Initialize() 호출
[ ] 말벌 생성 확인
[ ] EnemyAI.FindNearestEnemyHive() 작동
[ ] 말벌이 홈 하이브 주변 순찰
[ ] WaspWaveManager 등록 확인
[ ] EnemyHive 파괴 확인
[ ] CombatUnit.Die() → EnemyHive.DestroyHive()
[ ] WaspWaveManager 등록 해제
[ ] 플레이어 Hive와 분리 확인
```

---

## ?? 완료!

**핵심 개선:**
- ? EnemyHive 클래스 생성
- ? Hive와 완전 분리
- ? 말벌 전용 기능 구현
- ? 시스템 안정화

**결과:**
- 명확한 클래스 구조
- 버그 감소
- 코드 간결화
- 유지보수 용이

**게임 플레이:**
- 플레이어 하이브: 꿀벌 생성, 이사, 명령
- 적 하이브: 말벌 생성, 웨이브 공격
- 완전 독립적인 시스템

게임 개발 화이팅! ????????
