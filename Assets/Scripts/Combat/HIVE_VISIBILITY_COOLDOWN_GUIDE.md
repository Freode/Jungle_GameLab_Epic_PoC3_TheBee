# ?? 말벌집 지속 가시성 & 공격 쿨타임 시스템 가이드

## 완료된 개선사항

### 1. ? 말벌집 지속 가시성
- 한 번 발견한 말벌집은 계속 보임
- 시야에서 벗어나도 계속 표시
- 전략적 정찰 중요성 증가

### 2. ? 공격 쿨타임 시스템
- 모든 유닛에 공격 쿨타임 적용
- 쿨타임 동안 회피 행동
- 주변 타일로 랜덤 이동
- 전술적 전투 구현

---

## ?? 수정된 파일

### 1. EnemyVisibilityController.cs
```csharp
? discoveredHives (발견된 하이브 기록)
? 말벌집 한 번 발견 시 계속 표시
? ResetDiscoveredHives() - 게임 재시작용
```

### 2. CombatUnit.cs
```csharp
? attackCooldown (공격 쿨타임)
? lastAttackTime (마지막 공격 시간)
? CanAttack() - 공격 가능 여부
? TryAttack() - 쿨타임 체크 후 공격
? GetAttackCooldownRemaining() - 남은 쿨타임
```

### 3. EnemyAI.cs
```csharp
? 공격 쿨타임 체크
? EvadeInCurrentTile() - 회피 행동
? GetRandomAdjacentTile() - 랜덤 이동
? 쿨타임 동안 주변 이동
```

### 4. UnitBehaviorController.cs
```csharp
? WaitAndCombatRoutine() - 전투 루프
? EvadeNearby() - 회피 행동
? 쿨타임 기반 전투 시스템
```

---

## ?? 시스템 작동 방식

### 1. 말벌집 지속 가시성

#### 발견 시스템
```
[일꾼이 말벌집 근처 접근]
   ↓
일꾼 시야(3칸) 내 말벌집 진입
   ↓
discoveredHives에 말벌집 추가 ?
   ↓
말벌집이 보이기 시작
   ↓
일꾼이 멀리 이동 (시야 밖)
   ↓
말벌집은 계속 보임 ? (한 번 발견됨)
```

#### 가시성 판단
```csharp
if (isHive)
{
    // 현재 시야 내이면 발견 기록
    if (isCurrentlyVisible)
    {
        discoveredHives.Add(unit.gameObject);
    }
    
    // 발견된 하이브는 항상 보임
    shouldBeVisible = discoveredHives.Contains(unit.gameObject);
}
else
{
    // 일반 유닛은 현재 시야 내에만 보임
    shouldBeVisible = isCurrentlyVisible;
}
```

### 2. 공격 쿨타임 시스템

#### 공격 흐름
```
[전투 시작]
   ↓
공격 가능? (CanAttack())
   ├─ Yes → 공격 실행 ?
   │         lastAttackTime 갱신
   │         attackCooldown 초 대기
   │         ↓
   └─ No  → 회피 행동 ?
             주변 1칸 랜덤 이동
             쿨타임 대기
             ↓
             반복...
```

#### CombatUnit 쿨타임 체크
```csharp
public bool CanAttack()
{
    return Time.time - lastAttackTime >= attackCooldown;
}

public bool TryAttack(CombatUnit target)
{
    if (!CanAttack()) return false; // 쿨타임 중
    
    target.TakeDamage(attack);
    lastAttackTime = Time.time; // 갱신
    
    return true;
}
```

---

## ?? 시나리오

### 시나리오 1: 말벌집 발견

```
[초기 상태]
플레이어: 말벌집 위치 모름
말벌집: 안 보임

[정찰 시작]
일꾼 1마리 → 말벌집 근처 접근
   ↓
시야(3칸) 내 진입
   ↓
말벌집 발견! ?
   ↓
discoveredHives 등록
   ↓
[일꾼 복귀]
일꾼 → 하이브로 복귀 (시야 밖)
   ↓
말벌집은 계속 보임 ?
   ↓
[게임 내내 유지]
말벌집은 항상 표시됨 ?
```

### 시나리오 2: 공격 쿨타임 전투 (Enemy AI)

```
[말벌 vs 일꾼]
말벌: attackCooldown = 2초
일꾼: attackCooldown = 2초

[전투 시작]
T=0초:  말벌 공격 성공 ? (데미지 12)
        일꾼 HP: 50 → 38
        
T=0.5초: 말벌 쿨타임 중 → 회피 이동 ?
         (0,0) → (1,0) 이동
         
T=1초:  말벌 여전히 쿨타임 → 회피 이동 ?
        (1,0) → (0,1) 이동
        
T=2초:  말벌 쿨타임 종료 → 추격 시작
        일꾼 위치로 이동
        
T=3초:  말벌 공격 성공 ? (데미지 12)
        일꾼 HP: 38 → 26
        
T=3.5초: 말벌 쿨타임 중 → 회피 이동 ?
         (0,1) → (1,1) 이동
         
...반복
```

### 시나리오 3: 공격 쿨타임 전투 (Player)

```
[일꾼 vs 말벌]
일꾼: attackCooldown = 2초
말벌: attackCooldown = 2초

[플레이어 명령] 일꾼 → 말벌 공격

[전투 루프]
T=0초:  일꾼 공격 성공 ? (데미지 10)
        말벌 HP: 60 → 50
        
T=0.5초: 일꾼 쿨타임 중 → 회피 이동 ?
         주변 랜덤 타일로 이동
         
T=1초:  일꾼 쿨타임 중 → 회피 이동 ?
        또 다른 주변 타일로 이동
        
T=2초:  일꾼 쿨타임 종료 → 공격 가능
        말벌에게 접근
        
T=2.5초: 일꾼 공격 성공 ? (데미지 10)
         말벌 HP: 50 → 40
         
T=3초:  일꾼 쿨타임 중 → 회피 이동 ?
        
...전투 계속
```

---

## ?? 전투 패턴

### 공격 가능 시
```
[유닛 A] ━━━ 공격 ━━━> [유닛 B]
         (데미지 적용)
```

### 쿨타임 시
```
[유닛 A] 
    ↓ 회피
  ? ? ?
  ? A ?  → 랜덤 이동
  ? ? ?
```

### 전투 흐름
```
공격 → 쿨타임 → 회피 → 쿨타임 종료 → 공격 → ...
```

---

## ?? Inspector 설정

### CombatUnit 설정
```yaml
Combat Unit (Script)
├─ 기본 능력치
│  ├─ Max Health: 50
│  ├─ Health: 50
│  ├─ Attack: 10
│  └─ Attack Range: 1
└─ 공격 쿨타임
   └─ Attack Cooldown: 2 ? (초)
```

### 쿨타임 조절
```csharp
// 빠른 공격
attackCooldown = 1f;

// 표준 공격 (권장)
attackCooldown = 2f; ?

// 느린 공격
attackCooldown = 3f;
```

---

## ?? 코드 구조

### CombatUnit.cs

#### 공격 가능 체크
```csharp
public bool CanAttack()
{
    return Time.time - lastAttackTime >= attackCooldown;
}
```

#### 공격 시도
```csharp
public bool TryAttack(CombatUnit target)
{
    // 쿨타임 체크
    if (!CanAttack()) return false;
    
    // 공격 실행
    target.TakeDamage(attack);
    lastAttackTime = Time.time;
    
    return true;
}
```

#### 남은 쿨타임
```csharp
public float GetAttackCooldownRemaining()
{
    float remaining = attackCooldown - (Time.time - lastAttackTime);
    return Mathf.Max(0f, remaining);
}
```

### EnemyAI.cs

#### 쿨타임 체크 및 회피
```csharp
void UpdateBehavior()
{
    if (currentTarget != null)
    {
        bool canAttack = combat != null && combat.CanAttack();
        
        if (distanceToTarget <= attackRange)
        {
            if (canAttack)
            {
                Attack(currentTarget); // 공격
            }
            else
            {
                EvadeInCurrentTile(); // 회피
            }
        }
    }
}
```

#### 회피 행동
```csharp
void EvadeInCurrentTile()
{
    if (controller.IsMoving()) return;
    
    // 주변 1칸 랜덤 선택
    var evadeTile = GetRandomAdjacentTile();
    
    // 이동 실행
    controller.SetPath(path);
}
```

### UnitBehaviorController.cs

#### 전투 루프
```csharp
IEnumerator WaitAndCombatRoutine(HexTile dest, UnitAgent enemy)
{
    // 목적지 도착까지 대기
    while (...)
        yield return null;
    
    // 전투 루프
    while (enemyCombat.health > 0 && myCombat.health > 0)
    {
        if (myCombat.CanAttack())
        {
            myCombat.TryAttack(enemyCombat); // 공격
        }
        else
        {
            EvadeNearby(); // 회피
        }
        
        yield return new WaitForSeconds(0.1f);
    }
}
```

---

## ?? 비교표

### 말벌집 가시성

| 상황 | 이전 | 현재 |
|------|------|------|
| **처음 발견** | 보임 | 보임 + 기록 ? |
| **시야 이탈** | 안 보임 ? | 계속 보임 ? |
| **재방문** | 다시 발견 필요 | 계속 보임 ? |
| **전략성** | 낮음 | 높음 ? |

### 전투 시스템

| 요소 | 이전 | 현재 |
|------|------|------|
| **공격 제한** | 없음 | 쿨타임 ? |
| **회피 행동** | 없음 | 주변 이동 ? |
| **전투 패턴** | 단순 | 복잡 ? |
| **전술성** | 낮음 | 높음 ? |

---

## ?? 게임 플레이 개선

### 변경 전
```
? 말벌집이 계속 숨었다 보였다
? 정찰의 의미 없음
? 공격이 너무 빠름
? 전투가 단조로움
```

### 변경 후
```
? 한 번 발견한 말벌집은 계속 보임
? 정찰의 중요성 증가
? 공격 쿨타임으로 템포 조절
? 회피 행동으로 다이나믹한 전투
```

---

## ?? 디버그 로그

### 말벌집 발견
```
[가시성] 말벌집 발견! (0, 0)
[가시성] discoveredHives 추가
[가시성] 발견된 하이브: 1개
```

### 공격 쿨타임
```
[Enemy AI] Worker_Bee_1 공격! 데미지: 12
[Enemy AI] 공격 쿨타임 중...
[Enemy AI] 회피 이동: (1, 0)
[Enemy AI] 공격 쿨타임 중...
[Enemy AI] 회피 이동: (0, 1)
[Enemy AI] Worker_Bee_1 공격! 데미지: 12
```

---

## ?? 추가 개선 아이디어

### 1. 하이브 정보 UI
```csharp
// 발견한 하이브 표시
public class HiveInfoUI : MonoBehaviour
{
    void Update()
    {
        var discovered = EnemyVisibilityController.Instance.GetDiscoveredHiveCount();
        hiveCountText.text = $"발견한 적 하이브: {discovered}";
    }
}
```

### 2. 쿨타임 UI 표시
```csharp
// 공격 쿨타임 시각화
public class CooldownIndicator : MonoBehaviour
{
    void Update()
    {
        float remaining = combat.GetAttackCooldownRemaining();
        cooldownBar.fillAmount = 1f - (remaining / combat.attackCooldown);
    }
}
```

### 3. 회피 우선순위
```csharp
// 안전한 타일 우선 선택
HexTile GetSafeEvadeTile()
{
    // 적이 없는 타일
    // 하이브에 가까운 타일
    // 자원이 있는 타일
}
```

---

## ?? 문제 해결

### Q: 말벌집이 계속 안 보여요

**확인사항:**
```
? EnemyVisibilityController 존재 확인
? 일꾼이 말벌집 시야 내 방문했는지 확인
? discoveredHives에 추가되었는지 확인
```

### Q: 공격이 너무 느려요

**해결:**
```csharp
// attackCooldown 줄이기
combat.attackCooldown = 1f; // 2초 → 1초
```

### Q: 회피 행동이 안 보여요

**확인사항:**
```
? CanAttack() false 확인
? EvadeInCurrentTile() 호출 확인
? GetRandomAdjacentTile() 타일 반환 확인
```

---

## ? 테스트 체크리스트

### 말벌집 가시성
```
[ ] 일꾼으로 말벌집 발견
[ ] 말벌집이 보임
[ ] 일꾼 멀리 이동
[ ] 말벌집 계속 보임 ?
[ ] 게임 종료까지 유지
```

### 공격 쿨타임
```
[ ] 전투 시작
[ ] 첫 공격 성공
[ ] 쿨타임 동안 회피 이동
[ ] 쿨타임 종료 후 공격
[ ] 반복 확인
```

---

## ?? 완료!

**핵심 기능:**
- ? 말벌집 한 번 발견 시 계속 표시
- ? 모든 유닛 공격 쿨타임 적용
- ? 쿨타임 동안 회피 행동
- ? 다이나믹한 전투 시스템

**게임 플레이:**
- 정찰의 중요성 증가
- 전략적 하이브 탐색
- 템포 있는 전투
- 전술적 회피 행동

게임 개발 화이팅! ??????
