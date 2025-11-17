# ?? EnemyHive 좌표 초기화 수정 가이드

## 완료된 개선사항

### ? EnemyHiveSpawner에서 EnemyHive.Initialize() 호출
- Hive 대신 EnemyHive 컴포넌트 사용
- q, r 좌표 올바르게 초기화
- 타일에 자동 부착
- 체력 설정 (엘리트 500, 일반 250)

---

## ?? 수정된 파일

### EnemyHiveSpawner.cs
```csharp
? SpawnEliteWaspHive() 수정
   - EnemyHive.Initialize(q, r, maxHealth) 호출
   - q, r 좌표 올바르게 설정
   - 엘리트 체력 500
   
? SpawnNormalWaspHives() 수정
   - EnemyHive.Initialize(pos.x, pos.y, maxHealth) 호출
   - q, r 좌표 올바르게 설정
   - 일반 체력 250
```

---

## ?? 시스템 작동 방식

### 이전 방식의 문제

```
[이전 방식]
EnemyHiveSpawner.SpawnEliteWaspHive()
{
    Vector3 position = HexToWorld(q, r, 0.5f)
    eliteHive = Instantiate(prefab, position, ...)
    
    // Hive 컴포넌트 초기화 시도 ?
    var hive = eliteHive.GetComponent<Hive>()
    if (hive != null)
    {
        hive.Initialize(q, r) ?
    }
}
   ↓
[문제점]
1. EnemyHive는 Hive가 아님 ?
2. GetComponent<Hive>() = null ?
3. q, r 초기화 안 됨 ?
4. 타일 부착 안 됨 ?
   ↓
[결과]
EnemyHive.q = 0, EnemyHive.r = 0 (기본값) ?
타일 부착 안 됨 ?
```

---

### 새로운 방식

```
[새로운 방식]
EnemyHiveSpawner.SpawnEliteWaspHive()
{
    int q = 0, r = 0
    
    // 프리팹 생성 (위치는 나중에 설정) ?
    eliteHive = Instantiate(prefab, Vector3.zero, ...)
    
    // EnemyHive 컴포넌트 가져오기 ?
    var enemyHive = eliteHive.GetComponent<EnemyHive>()
    if (enemyHive != null)
    {
        // EnemyHive.Initialize() 호출 ?
        enemyHive.Initialize(q, r, 500)
        {
            // EnemyHive 내부에서
            this.q = q ?
            this.r = r ?
            
            var tile = GetTile(q, r) ?
            transform.SetParent(tile.transform) ?
            tile.enemyHive = this ?
            transform.localPosition = Vector3.zero ?
            
            // UnitAgent 설정
            hiveAgent.SetPosition(q, r) ?
            hiveAgent.faction = Enemy ?
            
            // CombatUnit 설정
            combat.maxHealth = 500 ?
            combat.health = 500 ?
        }
    }
}
   ↓
[결과]
EnemyHive.q = 0, EnemyHive.r = 0 ?
타일에 부착됨 ?
UnitAgent 초기화됨 ?
CombatUnit 초기화됨 ?
```

---

## ?? 핵심 코드

### 1. SpawnEliteWaspHive() 수정

```csharp
// EnemyHiveSpawner.cs
/// <summary>
/// 엘리트말벌집 생성 (0, 0 위치)
/// </summary>
void SpawnEliteWaspHive()
{
    int q = 0, r = 0;
    
    // 타일 존재 확인
    var tile = TileManager.Instance?.GetTile(q, r);
    if (tile == null)
    {
        Debug.LogError($"[적 하이브 생성] 타일 ({q}, {r})이 존재하지 않습니다!");
        return;
    }

    // 프리팹 생성 (위치는 Initialize에서 설정됨) ?
    eliteHive = Instantiate(eliteWaspHivePrefab, Vector3.zero, Quaternion.identity);
    eliteHive.name = "EliteWaspHive";

    // EnemyHive 컴포넌트 가져오기 ?
    var enemyHive = eliteHive.GetComponent<EnemyHive>();
    if (enemyHive != null)
    {
        // EnemyHive.Initialize() 호출 (q, r 설정 및 타일 부착) ?
        enemyHive.Initialize(q, r, 500); // 엘리트는 체력 높게
        Debug.Log($"[적 하이브 생성] 엘리트말벌집 생성: ({q}, {r})");
    }
    else
    {
        Debug.LogError($"[적 하이브 생성] EnemyHive 컴포넌트를 찾을 수 없습니다!");
        Destroy(eliteHive);
        eliteHive = null;
    }
}
```

---

### 2. SpawnNormalWaspHives() 수정

```csharp
// EnemyHiveSpawner.cs
/// <summary>
/// 일반 말벌집 생성 및 배치
/// </summary>
void SpawnNormalWaspHives(int count)
{
    List<Vector2Int> spawnedPositions = new List<Vector2Int>();
    int attempts = 0;
    int maxAttempts = 100;

    for (int i = 0; i < count && attempts < maxAttempts; attempts++)
    {
        Vector2Int pos = FindValidHivePosition(spawnedPositions);
        
        if (pos.x == int.MinValue) continue;

        // 프리팹 생성 (위치는 Initialize에서 설정됨) ?
        GameObject hive = Instantiate(normalWaspHivePrefab, Vector3.zero, Quaternion.identity);
        hive.name = $"NormalWaspHive_{i + 1}";

        // EnemyHive 컴포넌트 가져오기 ?
        var enemyHive = hive.GetComponent<EnemyHive>();
        if (enemyHive != null)
        {
            // EnemyHive.Initialize() 호출 (q, r 설정 및 타일 부착) ?
            enemyHive.Initialize(pos.x, pos.y, 250); // 일반은 기본 체력
            Debug.Log($"[적 하이브 생성] 일반 말벌집 생성: ({pos.x}, {pos.y})");
        }
        else
        {
            Debug.LogError($"[적 하이브 생성] EnemyHive 컴포넌트를 찾을 수 없습니다!");
            Destroy(hive);
            continue;
        }

        normalHives.Add(hive);
        spawnedPositions.Add(pos);
        i++;
    }

    if (normalHives.Count < count)
    {
        Debug.LogWarning($"[적 하이브 생성] 요청: {count}개, 실제 생성: {normalHives.Count}개");
    }
}
```

---

### 3. EnemyHive.Initialize() (참고)

```csharp
// EnemyHive.cs
public void Initialize(int q, int r, int maxHealth = 250)
{
    // 좌표 설정 ?
    this.q = q;
    this.r = r;

    // 타일 찾기 및 부착 ?
    if (TileManager.Instance != null)
    {
        var tile = TileManager.Instance.GetTile(q, r);
        if (tile != null)
        {
            transform.SetParent(tile.transform); ?
            tile.enemyHive = this; ?
            transform.localPosition = Vector3.zero; ?
        }
    }

    // UnitAgent 설정 ?
    if (hiveAgent != null)
    {
        hiveAgent.SetPosition(q, r);
        hiveAgent.faction = Faction.Enemy;
    }

    // CombatUnit 설정 ?
    if (combat != null)
    {
        combat.maxHealth = maxHealth;
        combat.health = maxHealth;
    }

    // 초기 렌더러 비활성화
    SetAllRenderersEnabled(false);

    // 가시성 체크
    if (EnemyVisibilityController.Instance != null)
    {
        StartCoroutine(InitializeVisibilityCheck());
    }
}
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **컴포넌트** | Hive ? | EnemyHive ? |
| **q, r 초기화** | 안 됨 ? | 됨 ? |
| **타일 부착** | 안 됨 ? | 됨 ? |
| **체력 설정** | 기본값 ? | 커스텀 ? |
| **로그** | 부족 ? | 상세 ? |

---

## ?? 시각화

### 생성 흐름

```
[EnemyHiveSpawner.SpawnEliteWaspHive()]
q = 0, r = 0
   ↓
[Instantiate]
eliteHive = Instantiate(prefab, Vector3.zero, ...)
   ↓
[GetComponent<EnemyHive>()] ?
var enemyHive = eliteHive.GetComponent<EnemyHive>()
   ↓
[Initialize(0, 0, 500)] ?
enemyHive.Initialize(0, 0, 500)
{
    this.q = 0 ?
    this.r = 0 ?
    
    tile = GetTile(0, 0) ?
    transform.SetParent(tile.transform) ?
    tile.enemyHive = this ?
    transform.localPosition = Vector3.zero ?
    
    hiveAgent.SetPosition(0, 0) ?
    hiveAgent.faction = Enemy ?
    
    combat.maxHealth = 500 ?
    combat.health = 500 ?
}
   ↓
[결과]
HexTile (0, 0)
└─ EnemyHive ?
   ├─ q = 0, r = 0 ?
   ├─ localPosition = (0, 0, 0) ?
   ├─ UnitAgent (faction = Enemy) ?
   └─ CombatUnit (500/500 HP) ?
```

---

## ?? 문제 해결

### Q: "EnemyHive 컴포넌트를 찾을 수 없습니다" 에러

**확인:**
```
[ ] Prefab에 EnemyHive 컴포넌트 있는지 확인
[ ] GetComponent<EnemyHive>() != null
```

**해결:**
```
1. Unity Editor에서 Prefab 열기
2. Inspector에서 EnemyHive 컴포넌트 확인
3. 없으면 Add Component → EnemyHive
```

---

### Q: q, r이 여전히 0으로 표시돼요

**확인:**
```
[ ] Initialize() 호출 확인
[ ] Console에 "초기화 완료" 로그
[ ] this.q = q, this.r = r 실행
```

**해결:**
```
1. Console 로그 확인
   - "[적 하이브] 초기화 완료: (5, 3)"
2. Inspector에서 EnemyHive 컴포넌트 확인
   - q = 5
   - r = 3
3. 안 되면 EnemyHive.showDebugLogs = true
```

---

### Q: 타일에 부착되지 않아요

**확인:**
```
[ ] TileManager.Instance != null
[ ] GetTile(q, r) != null
[ ] transform.SetParent() 실행
```

**해결:**
```
1. Console 로그 확인
   - "[적 하이브] 타일에 부착: (5, 3)"
2. Hierarchy에서 확인
   - HexTile (5, 3)
     └─ EnemyHive
3. 안 되면 타일 생성 확인
```

---

## ?? 추가 개선 아이디어

### 1. 체력 난이도별 설정

```csharp
// EnemyHiveSpawner.cs
public enum Difficulty { Easy, Normal, Hard }
public Difficulty difficulty = Difficulty.Normal;

void SpawnEliteWaspHive()
{
    int maxHealth = difficulty switch
    {
        Difficulty.Easy => 300,
        Difficulty.Normal => 500,
        Difficulty.Hard => 800,
        _ => 500
    };
    
    enemyHive.Initialize(q, r, maxHealth);
}
```

---

### 2. 생성 시 이펙트

```csharp
// EnemyHiveSpawner.cs
void SpawnEliteWaspHive()
{
    // ...existing code...
    
    // 생성 이펙트 ?
    if (spawnEffect != null)
    {
        var effect = Instantiate(spawnEffect, 
            eliteHive.transform.position, 
            Quaternion.identity);
        Destroy(effect, 2f);
    }
    
    // 생성 사운드 ?
    if (spawnSound != null)
    {
        AudioSource.PlayClipAtPoint(spawnSound, 
            eliteHive.transform.position);
    }
}
```

---

### 3. 생성 애니메이션

```csharp
// EnemyHiveSpawner.cs
IEnumerator SpawnWithAnimation(GameObject hivePrefab, int q, int r, int maxHealth)
{
    var hive = Instantiate(hivePrefab, Vector3.zero, Quaternion.identity);
    var enemyHive = hive.GetComponent<EnemyHive>();
    
    if (enemyHive != null)
    {
        enemyHive.Initialize(q, r, maxHealth);
        
        // 크기 애니메이션 ?
        Vector3 originalScale = hive.transform.localScale;
        hive.transform.localScale = Vector3.zero;
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            hive.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }
        
        hive.transform.localScale = originalScale;
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] EnemyHiveSpawner.SpawnAllEnemyHives() 실행
[ ] Console에 "엘리트말벌집 생성: (0, 0)" 로그 ?
[ ] Console에 "[적 하이브] 초기화 완료: (0, 0)" 로그 ?
[ ] Inspector에서 EnemyHive.q = 0 확인 ?
[ ] Inspector에서 EnemyHive.r = 0 확인 ?
[ ] Hierarchy에서 타일 자식 확인 ?
[ ] Console에 "일반 말벌집 생성: (5, 3)" 로그 ?
[ ] Console에 "[적 하이브] 초기화 완료: (5, 3)" 로그 ?
[ ] Inspector에서 EnemyHive.q = 5 확인 ?
[ ] Inspector에서 EnemyHive.r = 3 확인 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? Hive → EnemyHive 컴포넌트 변경
- ? EnemyHive.Initialize() 호출
- ? q, r 좌표 올바르게 초기화
- ? 체력 커스텀 설정 (엘리트 500, 일반 250)

**결과:**
- EnemyHive.q, r 올바르게 설정
- 타일에 정확히 부착
- UnitAgent 정상 초기화
- CombatUnit 정상 초기화

**게임 플레이:**
- 말벌집 정확한 위치에 생성
- AI가 올바른 좌표 참조
- Gizmo가 정확한 위치 표시
- 완벽한 좌표 동기화

게임 개발 화이팅! ????????
