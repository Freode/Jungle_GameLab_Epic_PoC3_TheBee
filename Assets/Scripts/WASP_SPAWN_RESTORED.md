# ?? 적 말벌 스폰 위치 복원 가이드

## 변경 사항

### ? 적 말벌 스폰 위치를 하이브로 복원
- 맵 밖 스폰 로직 제거
- 하이브 위치에서 직접 생성
- 즉시 타겟 하이브로 이동 명령

---

## ?? 수정된 파일

### 1. EnemyAI.cs
```csharp
? 중복 코드 제거
   - Attack() 메서드 정리
   - IsWithinHiveRange() 중복 제거
```

### 2. WaspWaveManager.cs
```csharp
? SpawnAndAttackWasp() 복원
   - 맵 밖 생성 로직 제거
   - 하이브 위치에서 생성 ?
   - 비활성화 로직 제거
   
? MoveToTargetHive() 단순화
   - 즉시 이동 명령
   
? MoveToHiveAndActivate() 제거
   - 더 이상 필요 없음
```

---

## ?? 시스템 작동 방식

### 말벌 스폰 (복원된 로직)

```
[웨이브 발동]
   ↓
[하이브 위치에서 생성]
Vector3 spawnPos = GetRandomPositionInTile(
    fromHive.q, fromHive.r, 
    0.5f, 0.15f
)
GameObject waspObj = Instantiate(waspPrefab, spawnPos, ...)
   ↓
[기본 설정]
agent.SetPosition(fromHive.q, fromHive.r)
agent.homeHive = fromHive
agent.faction = Enemy
   ↓
[보스 강화 (선택)]
if (isBoss)
{
    combat.maxHealth *= 2
    combat.attack *= 1.5
    transform.localScale *= 1.3
}
   ↓
[AI 설정]
enemyAI.visionRange = isBoss ? 3 : 1
enemyAI.activityRange = isBoss ? 10 : 3
   ↓
[타겟으로 이동]
StartCoroutine(MoveToTargetHive(agent, targetHive))
   ↓
[가시성 업데이트]
EnemyVisibilityController.ForceUpdateVisibility()
```

---

## ?? 핵심 코드

### 하이브에서 생성

```csharp
// WaspWaveManager.cs
void SpawnAndAttackWasp(Hive fromHive, Hive targetHive, bool isBoss)
{
    // 하이브 위치에서 랜덤 생성 ?
    Vector3 spawnPos = TileHelper.GetRandomPositionInTile(
        fromHive.q, fromHive.r, 0.5f, 0.15f
    );
    GameObject waspObj = Instantiate(waspPrefab, spawnPos, Quaternion.identity);
    
    var agent = waspObj.GetComponent<UnitAgent>();
    agent.SetPosition(fromHive.q, fromHive.r);
    agent.homeHive = fromHive;
    agent.faction = Faction.Enemy;
    
    // 타겟으로 이동
    StartCoroutine(MoveToTargetHive(agent, targetHive));
    
    // 가시성 업데이트
    StartCoroutine(HideWaspOnSpawn(agent));
}
```

### 타겟으로 이동

```csharp
IEnumerator MoveToTargetHive(UnitAgent wasp, Hive target)
{
    yield return null; // 초기화 대기

    var controller = wasp.GetComponent<UnitController>();
    var startTile = TileManager.Instance.GetTile(wasp.q, wasp.r);
    var targetTile = TileManager.Instance.GetTile(target.q, target.r);

    if (startTile != null && targetTile != null)
    {
        var path = Pathfinder.FindPath(startTile, targetTile);
        if (path != null && path.Count > 0)
        {
            controller.agent = wasp;
            controller.SetPath(path);
        }
    }
}
```

---

## ?? 비교표

| 요소 | 이전 (맵 밖) | 현재 (하이브) |
|------|-------------|--------------|
| **생성 위치** | 맵 밖 (mapRadius + 100) | 하이브 위치 ? |
| **초기 상태** | 비활성화 | 즉시 활성화 ? |
| **이동 단계** | 순간이동 → 활성화 → 이동 | 즉시 이동 ? |
| **코드 복잡도** | 높음 | 낮음 ? |
| **자연스러움** | 부자연스러움 | 자연스러움 ? |

---

## ?? 시각화

### 이전 (맵 밖 스폰)

```
[맵 밖]
(100, 100) 생성
   ↓ 비활성화
[순간이동]
하이브 (5, 5)
   ↓ 활성화
[이동 시작]
타겟 하이브 (0, 0)
```

### 현재 (하이브 스폰)

```
[하이브]
(5, 5) 생성 ?
   ↓
[즉시 이동]
타겟 하이브 (0, 0) ?

간단하고 자연스러움!
```

---

## ? 테스트 체크리스트

```
[ ] 웨이브 발동
[ ] 말벌이 하이브에서 생성
[ ] 말벌이 즉시 활성화 상태
[ ] 말벌이 타겟 하이브로 이동
[ ] 가시성 시스템 작동 (플레이어 시야 밖이면 숨김)
[ ] 보스 말벌 강화 (2배 체력, 1.5배 공격력)
```

---

## ?? 문제 해결

### Q: 말벌이 생성 안 돼요

**확인:**
```
[ ] fromHive != null
[ ] workerPrefab != null
[ ] TileHelper.GetRandomPositionInTile() 작동
[ ] Instantiate() 성공
```

### Q: 말벌이 이동 안 해요

**확인:**
```
[ ] UnitController 컴포넌트 존재
[ ] MoveToTargetHive() 코루틴 실행
[ ] Pathfinder.FindPath() 성공
[ ] SetPath() 호출
```

### Q: 말벌이 계속 보여요

**확인:**
```
[ ] EnemyVisibilityController.Instance != null
[ ] HideWaspOnSpawn() 코루틴 실행
[ ] ForceUpdateVisibility() 호출
[ ] 플레이어 시야 범위 밖인지 확인
```

---

## ?? 장점

### 이전 방식 (맵 밖 스폰)

**장점:**
- 플레이어에게 보이지 않음

**단점:**
- ? 부자연스러움
- ? 코드 복잡함
- ? 순간이동 필요
- ? 활성화/비활성화 처리

### 현재 방식 (하이브 스폰)

**장점:**
- ? 자연스러움
- ? 코드 간단함
- ? 즉시 이동
- ? 가시성 시스템 활용

**단점:**
- 플레이어 시야에 보일 수 있음 (하지만 가시성 시스템이 처리)

---

## ?? 완료!

**핵심 변경:**
- ? 하이브에서 생성
- ? 즉시 활성화
- ? 간단한 로직
- ? 자연스러운 출현

**코드 개선:**
- 복잡도 감소
- 가독성 향상
- 유지보수 용이

**게임 플레이:**
- 자연스러운 적 출현
- 가시성 시스템 활용
- 전투 시작 직관적

게임 개발 화이팅! ??????
