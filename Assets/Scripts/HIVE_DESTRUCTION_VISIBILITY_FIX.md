# ?? 하이브 파괴 & 적 하이브 시야 수정 가이드

## 완료된 개선사항

### 1. ? 하이브 파괴 시 여왕벌 활성화
- CombatUnit.Die() 수정
- 하이브 컴포넌트 체크
- DestroyHive() 리플렉션 호출
- 여왕벌 재활성화

### 2. ? 적 하이브 시야 강화
- UpdateEnemyVisibility() 로그 강화
- 발견 즉시 로그 출력
- 하이브 표시 상태 로그
- showDebugLogs 옵션

---

## ?? 수정된 파일

### 1. CombatUnit.cs
```csharp
? Die() 수정
   - Hive 컴포넌트 체크
   - DestroyHive() 리플렉션 호출
   - 일반 유닛은 바로 파괴
```

### 2. EnemyVisibilityController.cs
```csharp
? UpdateEnemyVisibility() 강화
   - 하이브 발견 로그
   - 표시 상태 디버그 로그
   - showDebugLogs 옵션
```

### 3. EnemyAI.cs
```csharp
? 중복 코드 제거
   - attackRange 필드 중복 제거
   - FindNearestPlayerUnit 변수 정리
   - IsWithinHiveRange 간소화
```

---

## ?? 시스템 작동 방식

### 1. 하이브 파괴 시 여왕벌 활성화

#### 시나리오 A: 이사로 인한 파괴

```
[하이브 이사]
StartRelocation()
   ↓
[10초 카운트다운]
RelocationCountdown()
   ↓
[하이브 파괴]
DestroyHive()
   ↓
[여왕벌 활성화] ?
queenBee.canMove = true
Renderer/Collider 활성화
   ↓
[결과]
여왕벌이 하이브 위치에서 등장 ?
```

---

#### 시나리오 B: 적 공격으로 파괴

```
[적 공격]
?? → ??
   ↓
[데미지 누적]
TakeDamage()
   ↓
[체력 0]
health <= 0
   ↓
[Die() 호출]
CombatUnit.Die()
   ↓
[하이브 체크] ?
var hive = GetComponent<Hive>()
if (hive != null)
{
    // DestroyHive() 호출 ?
    리플렉션.Invoke(hive, "DestroyHive")
}
   ↓
[DestroyHive() 실행]
   ↓
[여왕벌 활성화] ?
queenBee.canMove = true
queenBee.transform.position = hivePos
Renderer/Collider 활성화
   ↓
[결과]
?? 여왕벌이 하이브 자리에서 등장 ?
일꾼들이 여왕벌 추종 ?
```

---

### 2. 적 하이브 시야 시스템

```
[플레이어 유닛 이동]
   ↓
[시야 범위 계산]
CalculatePlayerVision()
   ↓
[적 하이브 체크]
foreach (unit in AllUnits)
{
    if (unit.faction == Enemy && hive != null)
    {
        bool isVisible = currentVisibleTiles.Contains(unitPos)
        
        if (isVisible)
        {
            if (!discoveredHives.Contains(unit))
            {
                discoveredHives.Add(unit) ?
                Debug.Log("적 하이브 발견!") ?
            }
        }
        
        shouldBeVisible = discoveredHives.Contains(unit)
        
        if (showDebugLogs && shouldBeVisible)
            Debug.Log("하이브 표시 중") ?
    }
}
   ↓
[시각화 설정]
SetUnitVisibility(unit, shouldBeVisible)
   ↓
[결과]
발견된 하이브는 항상 보임 ?
```

---

## ?? 핵심 코드

### 1. CombatUnit.Die() 수정

```csharp
// CombatUnit.cs
void Die()
{
    // 하이브인 경우 DestroyHive() 호출 ?
    var hive = GetComponent<Hive>();
    if (hive != null)
    {
        // Hive의 DestroyHive() 메서드를 리플렉션으로 호출 ?
        var destroyMethod = typeof(Hive).GetMethod("DestroyHive", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (destroyMethod != null)
        {
            destroyMethod.Invoke(hive, null); ?
        }
        else
        {
            // DestroyHive 메서드가 없으면 직접 파괴
            Destroy(gameObject);
        }
    }
    else
    {
        // 일반 유닛은 바로 파괴
        Destroy(gameObject);
    }
}
```

**리플렉션 사용 이유:**
- `DestroyHive()`는 `private` 메서드
- `public`으로 변경하지 않고 호출
- 캡슐화 유지

---

### 2. 적 하이브 시야 강화

```csharp
// EnemyVisibilityController.cs
void UpdateEnemyVisibility()
{
    CalculatePlayerVision();

    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit.faction != Faction.Enemy) continue;

        var hive = unit.GetComponent<Hive>();
        bool isHive = (hive != null);
        
        Vector2Int unitPos = new Vector2Int(unit.q, unit.r);
        bool isCurrentlyVisible = currentVisibleTiles.Contains(unitPos);

        bool shouldBeVisible = false;

        if (isHive)
        {
            if (isCurrentlyVisible)
            {
                if (!discoveredHives.Contains(unit.gameObject))
                {
                    discoveredHives.Add(unit.gameObject);
                    
                    // 발견 로그 ?
                    Debug.Log($"[시야] 적 하이브 발견: {unit.name} at ({unit.q}, {unit.r})");
                    
                    if (showDebugLogs)
                        Debug.Log($"[시야 디버그] 발견된 하이브 수: {discoveredHives.Count}");
                }
            }

            shouldBeVisible = discoveredHives.Contains(unit.gameObject);
            
            // 표시 상태 로그 ?
            if (showDebugLogs && shouldBeVisible)
                Debug.Log($"[시야 디버그] 하이브 {unit.name} 표시 중");
        }
        else
        {
            shouldBeVisible = isCurrentlyVisible;
        }

        SetUnitVisibility(unit, shouldBeVisible);
    }
}
```

---

## ?? 비교표

### 하이브 파괴

| 시나리오 | 이전 | 현재 |
|----------|------|------|
| **이사** | 작동 | 작동 ? |
| **적 공격** | 즉시 파괴 ? | DestroyHive 호출 ? |
| **여왕벌** | 활성화 안 됨 ? | 활성화됨 ? |

### 적 하이브 시야

| 항목 | 이전 | 현재 |
|------|------|------|
| **발견 로그** | 있음 | 강화 ? |
| **표시 로그** | 없음 | 있음 ? |
| **디버그 옵션** | 있음 | 강화 ? |

---

## ?? 시각화

### 하이브 파괴 흐름

```
[적 공격]
?? → ??
   ↓
[체력 감소]
HP: 200 → 150 → 100 → 50 → 0
   ↓
[Die() 호출]
   ↓
[하이브 체크] ?
GetComponent<Hive>() != null
   ↓
[DestroyHive 호출] ?
리플렉션.Invoke(hive, "DestroyHive")
   ↓
[여왕벌 활성화]
?? canMove = true
   Renderer ON
   Collider ON
   ↓
[결과]
?? ?????? ?
```

---

### 적 하이브 시야

```
[플레이어 시야]
    ??
   /|\
  / | \
 /  |  \
?? ?? ?? (3칸 시야)
   ↓
[하이브 발견]
   ??? (적 하이브)
   ↓
[로그 출력] ?
"[시야] 적 하이브 발견!"
   ↓
[표시 상태]
discoveredHives.Add(hive)
   ↓
[항상 보임] ?
shouldBeVisible = true
```

---

## ?? 문제 해결

### Q: 하이브 파괴 후 여왕벌이 안 나와요

**확인:**
```
[ ] CombatUnit.Die() 실행 확인
[ ] GetComponent<Hive>() != null
[ ] DestroyHive() 호출 확인
[ ] Console에 "[하이브 파괴] 여왕벌 활성화 완료" 로그
```

**해결:**
```
1. Console에서 Die() 호출 확인
2. 하이브 GameObject에 Hive 컴포넌트 존재 확인
3. queenBee 변수가 할당되었는지 확인
4. Renderer/Collider 활성화 확인
```

---

### Q: 적 하이브가 여전히 안 보여요

**확인:**
```
[ ] showDebugLogs = true 설정
[ ] Console에 "[시야] 적 하이브 발견" 로그
[ ] Console에 "[시야 디버그] 하이브 표시 중" 로그
[ ] SetUnitVisibility() 호출 확인
```

**해결:**
```
1. EnemyVisibilityController.showDebugLogs = true
2. Console 로그 확인
3. discoveredHives.Count 확인
4. Renderer.enabled 상태 확인
```

---

### Q: 리플렉션이 작동 안 해요

**확인:**
```
[ ] DestroyHive 메서드 존재 확인
[ ] GetMethod 반환값 != null
[ ] Invoke 실행 확인
```

**해결:**
```
1. Hive.cs에 DestroyHive() 메서드 존재 확인
2. BindingFlags 설정 확인
   - NonPublic: private 메서드
   - Instance: 인스턴스 메서드
3. null 체크 추가
```

---

## ?? 추가 개선 아이디어

### 1. DestroyHive를 public으로 변경

```csharp
// Hive.cs
public void DestroyHive() // private → public ?
{
    Debug.Log("[하이브 파괴] 하이브가 파괴됩니다.");
    
    // ...기존 코드...
}

// CombatUnit.cs
void Die()
{
    var hive = GetComponent<Hive>();
    if (hive != null)
    {
        hive.DestroyHive(); // 직접 호출 ?
    }
    else
    {
        Destroy(gameObject);
    }
}
```

**장점:**
- 리플렉션 불필요
- 성능 향상
- 코드 간결화

---

### 2. 하이브 파괴 애니메이션

```csharp
// CombatUnit.cs
void Die()
{
    var hive = GetComponent<Hive>();
    if (hive != null)
    {
        // 파괴 애니메이션 ?
        StartCoroutine(DestroyHiveWithAnimation(hive));
    }
    else
    {
        Destroy(gameObject);
    }
}

IEnumerator DestroyHiveWithAnimation(Hive hive)
{
    // 파티클 효과
    var particles = Instantiate(explosionParticles, hive.transform.position, Quaternion.identity);
    
    // 사운드 효과
    AudioSource.PlayClipAtPoint(explosionSound, hive.transform.position);
    
    // 0.5초 대기
    yield return new WaitForSeconds(0.5f);
    
    // DestroyHive 호출
    hive.DestroyHive();
}
```

---

### 3. 적 하이브 발견 알림

```csharp
// EnemyVisibilityController.cs
if (isCurrentlyVisible)
{
    if (!discoveredHives.Contains(unit.gameObject))
    {
        discoveredHives.Add(unit.gameObject);
        
        Debug.Log($"[시야] 적 하이브 발견!");
        
        // UI 알림 ?
        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.ShowMessage(
                "적 하이브를 발견했습니다!",
                3f
            );
        }
        
        // 미니맵 마커 ?
        if (MinimapMarker.Instance != null)
        {
            MinimapMarker.Instance.AddMarker(
                unit.q, unit.r, 
                MarkerType.EnemyHive
            );
        }
    }
}
```

---

### 4. 하이브 파괴 통계

```csharp
// GameManager.cs
public class GameStats
{
    public int hivesDestroyed = 0;
    public int hivesLost = 0;
}

// CombatUnit.cs
void Die()
{
    var hive = GetComponent<Hive>();
    if (hive != null)
    {
        // 통계 업데이트 ?
        if (GameManager.Instance != null)
        {
            if (agent.faction == Faction.Player)
                GameManager.Instance.stats.hivesLost++;
            else
                GameManager.Instance.stats.hivesDestroyed++;
        }
        
        hive.DestroyHive();
    }
    else
    {
        Destroy(gameObject);
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 이사 → 여왕벌 등장 ?
[ ] 적 공격으로 하이브 파괴 → 여왕벌 등장 ?
[ ] 여왕벌 이동 가능 ?
[ ] 일꾼들이 여왕벌 추종 ?
[ ] 플레이어 유닛 이동 → 적 하이브 시야 진입
[ ] Console에 "[시야] 적 하이브 발견" 로그 ?
[ ] 하이브가 보임 ?
[ ] showDebugLogs로 디버그 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? CombatUnit.Die() 하이브 체크
- ? DestroyHive() 리플렉션 호출
- ? 적 하이브 시야 로그 강화

**결과:**
- 적 공격으로 하이브 파괴 시 여왕벌 활성화
- 명확한 적 하이브 발견 로그
- 안정적인 게임 플레이

**게임 플레이:**
- 하이브 방어 실패 → 여왕벌로 재건
- 적 하이브 발견 → 전략적 공격
- 자연스러운 게임 흐름

게임 개발 화이팅! ????????
