# ?? 적 유닛 AI 시스템 가이드

## 개요
말벌이 플레이어 유닛을 자동으로 감지하고 추적/공격하는 AI 시스템

---

## ? 주요 기능

### 1. 자동 타겟 탐색
```
- 시야 범위(5칸) 내 플레이어 유닛 자동 감지
- 가장 가까운 유닛 우선 타겟팅
- 1초마다 주기적 스캔
```

### 2. 추적 및 공격
```
- 타겟 발견 시 자동 추격
- 공격 범위(1칸) 내 도달 시 자동 공격
- 실시간 경로 찾기 및 이동
```

### 3. 활동 범위 제한
```
- 하이브 중심 10칸 반경 내에서만 활동
- 타겟이 범위 밖으로 도망가면 추격 중단
- 자동으로 하이브로 복귀
```

---

## ?? 작동 방식

### 행동 순서도

```
[대기] 
  ↓ 1초마다 스캔
[타겟 탐색]
  ↓ 발견
[추격 시작]
  ↓ 공격 범위 내
[공격 실행]
  ↓ 타겟 처치 or 범위 이탈
[추격 중단]
  ↓
[하이브 복귀]
  ↓
[대기]
```

### 상태별 행동

#### 1. 대기 상태
```
조건:
- 타겟 없음
- 하이브 근처 (2칸 이내)

행동:
- 주기적 타겟 스캔 (1초마다)
- 제자리 대기
```

#### 2. 추격 상태
```
조건:
- 타겟 발견
- 하이브 범위 내
- 공격 범위 밖

행동:
- 타겟으로 경로 찾기
- 자동 이동
- 매 초 경로 갱신
```

#### 3. 공격 상태
```
조건:
- 타겟과 거리 1칸 이내

행동:
- 타겟에게 데미지
- 타겟 체력 확인
- 처치 시 추격 중단
```

#### 4. 복귀 상태
```
조건:
- 타겟 상실
- 하이브에서 멀리 떨어짐 (2칸 초과)

행동:
- 하이브로 경로 찾기
- 자동 이동
- 도착 시 대기 상태
```

---

## ?? AI 설정

### Inspector 설정
```yaml
Enemy AI (Script)
├─ AI 설정
│  ├─ Vision Range: 5 (시야 범위)
│  ├─ Activity Range: 10 (하이브 활동 범위)
│  ├─ Scan Interval: 1 (타겟 탐색 주기)
│  └─ Attack Range: 1 (공격 범위)
└─ 디버그
   └─ Show Debug Logs: false
```

### 범위 설정 가이드

#### 시야 범위 (Vision Range)
```
3칸: 근거리 전투 (방어적)
5칸: 표준 (권장) ?
7칸: 장거리 탐지 (공격적)
```

#### 활동 범위 (Activity Range)
```
5칸: 하이브 근처만 방어
10칸: 표준 (권장) ?
15칸: 넓은 영역 순찰
```

#### 공격 범위 (Attack Range)
```
1칸: 근접 공격 (권장) ?
2칸: 중거리 공격
3칸: 원거리 공격
```

---

## ?? 코드 구조

### EnemyAI.cs

#### 주요 메서드

##### 1. UpdateBehavior()
```csharp
void UpdateBehavior()
{
    // 현재 타겟 유효성 검사
    if (currentTarget != null)
    {
        // 타겟이 죽었거나 너무 멀어짐
        // 하이브 범위 벗어남
        // → 추격 중단
        
        // 공격 범위 내
        // → 공격 실행
        
        // 추격 거리
        // → 이동 경로 갱신
    }
    else
    {
        // 새 타겟 탐색
        currentTarget = FindNearestPlayerUnit();
    }
}
```

##### 2. FindNearestPlayerUnit()
```csharp
UnitAgent FindNearestPlayerUnit()
{
    // 모든 유닛 순회
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        // 플레이어 유닛만
        if (unit.faction != Faction.Player) continue;
        
        // 시야 범위 내
        if (distance <= visionRange)
        {
            // 하이브 활동 범위 내
            if (IsWithinHiveRange(unit.q, unit.r))
            {
                // 가장 가까운 유닛 선택
            }
        }
    }
}
```

##### 3. ChaseTarget()
```csharp
void ChaseTarget(UnitAgent target)
{
    // 경로 찾기
    var path = Pathfinder.FindPath(startTile, targetTile);
    
    // 이동 실행
    controller.SetPath(path);
}
```

##### 4. Attack()
```csharp
void Attack(UnitAgent target)
{
    var targetCombat = target.GetComponent<CombatUnit>();
    
    // 데미지 적용
    targetCombat.TakeDamage(combat.attack);
    
    // 타겟 처치 확인
    if (targetCombat.health <= 0)
    {
        StopChasing();
    }
}
```

---

## ?? 시나리오 예시

### 시나리오 1: 하이브 방어

```
1. 플레이어 일꾼이 말벌집 근처로 접근
   ↓
2. 말벌 1: 시야 5칸 내 일꾼 감지
   ↓
3. 말벌 1: 일꾼 추격 시작
   ↓
4. 말벌 2, 3: 동일하게 감지 및 추격
   ↓
5. 말벌들이 일꾼을 포위
   ↓
6. 공격 범위(1칸) 도달
   ↓
7. 동시 공격 → 일꾼 처치
   ↓
8. 말벌들 하이브로 복귀
```

### 시나리오 2: 추격 중단

```
1. 말벌이 일꾼 추격 중
   ↓
2. 일꾼이 하이브에서 10칸 이상 도망
   ↓
3. 말벌: "타겟이 하이브 범위 밖. 추격 중단."
   ↓
4. 말벌 이동 중단
   ↓
5. 하이브로 복귀
```

### 시나리오 3: 집단 전투

```
[플레이어]
일꾼 10마리 → 말벌집 공격

[말벌집]
말벌 15마리 생성됨

[전투 시작]
1. 말벌 15마리 모두 일꾼 감지
2. 가장 가까운 일꾼부터 집중 공격
3. 일꾼 1명 처치 → 다음 타겟으로 전환
4. 일꾼들이 말벌집 파괴 시도
5. 말벌들은 하이브 10칸 내에서만 전투
```

---

## ?? 디버그 모드

### 활성화
```csharp
Inspector:
Show Debug Logs: ? (체크)
```

### 로그 예시
```
[Enemy AI] 새 타겟 발견: Worker_Bee_1
[Enemy AI] 타겟 추격 중: Worker_Bee_1
[Enemy AI] Wasp_1이(가) Worker_Bee_1을(를) 공격! 데미지: 12
[Enemy AI] 타겟 처치!
[Enemy AI] 추격 중단
[Enemy AI] 하이브로 복귀
```

### Gizmos (Scene View)

```
노란색 원: 시야 범위 (5칸)
빨간색 원: 활동 범위 (10칸, 하이브 중심)
초록색 선: 현재 타겟으로의 연결선
```

---

## ?? 최적화 팁

### 1. 스캔 주기 조정
```csharp
// 빠른 반응 (부하 높음)
scanInterval = 0.5f;

// 표준 (권장)
scanInterval = 1f; ?

// 느린 반응 (부하 낮음)
scanInterval = 2f;
```

### 2. 시야 범위 최적화
```csharp
// 많은 유닛 탐색 (부하 높음)
visionRange = 10;

// 표준 (권장)
visionRange = 5; ?

// 적은 유닛 탐색 (부하 낮음)
visionRange = 3;
```

### 3. 거리 계산 캐싱
```csharp
// 매번 계산 (현재)
int distance = GetDistance(q1, r1, q2, r2);

// 캐싱 (최적화)
private Dictionary<UnitAgent, int> cachedDistances;
```

---

## ?? 커스터마이징

### 1. 공격 패턴 변경

#### 집중 공격 (현재)
```csharp
// 가장 가까운 유닛 1개 타겟팅
UnitAgent FindNearestPlayerUnit() { ... }
```

#### 분산 공격
```csharp
// 여러 유닛에게 분산
UnitAgent FindLeastTargetedUnit()
{
    // 타겟팅된 횟수가 가장 적은 유닛 선택
}
```

### 2. 우선순위 타겟팅

#### 여왕벌 우선
```csharp
UnitAgent FindHighPriorityTarget()
{
    // 1순위: 여왕벌
    if (unit.isQueen) return unit;
    
    // 2순위: 체력 낮은 유닛
    // 3순위: 가까운 유닛
}
```

### 3. 집단 행동

#### 무리 지어 이동
```csharp
public class SwarmBehavior : MonoBehaviour
{
    public List<EnemyAI> swarmMembers;
    
    void Update()
    {
        // 무리의 평균 위치 계산
        Vector3 centerOfMass = CalculateCenter();
        
        // 무리 유지 (너무 멀어지면 따라감)
        foreach (var ai in swarmMembers)
        {
            if (Vector3.Distance(ai.transform.position, centerOfMass) > 3f)
            {
                ai.MoveTowards(centerOfMass);
            }
        }
    }
}
```

---

## ?? 문제 해결

### 말벌이 추격하지 않아요

**확인사항:**
```
? EnemyAI 컴포넌트가 추가되었는지
? UnitAgent.faction = Enemy인지
? homeHive가 설정되었는지
? visionRange > 0인지
? 플레이어 유닛이 시야 내에 있는지
```

### 말벌이 하이브로 복귀 안 해요

**확인사항:**
```
? homeHive 참조가 null이 아닌지
? activityRange가 적절한지 (10 권장)
? IsWithinHiveRange() 로직 확인
```

### 말벌이 공격하지 않아요

**확인사항:**
```
? CombatUnit 컴포넌트가 있는지
? attack 값이 0보다 큰지
? attackRange가 적절한지 (1 권장)
```

### 성능 문제

**해결:**
```
1. scanInterval 증가 (1 → 2초)
2. visionRange 감소 (5 → 3칸)
3. Enemy 유닛 수 제한
```

---

## ? 테스트 체크리스트

### 기본 동작
```
[ ] 말벌 생성 시 EnemyAI 자동 추가
[ ] 시야 범위 내 플레이어 유닛 감지
[ ] 타겟 자동 추격
[ ] 공격 범위 도달 시 자동 공격
[ ] 타겟 처치 후 추격 중단
```

### 범위 제한
```
[ ] 하이브 10칸 이내에서만 활동
[ ] 타겟이 범위 밖으로 도망 시 추격 중단
[ ] 자동으로 하이브 복귀
[ ] 하이브 근처 대기
```

### 집단 전투
```
[ ] 여러 말벌이 동시 추격
[ ] 타겟 전환 작동
[ ] 말벌끼리 겹치지 않음
[ ] 플레이어 유닛과 정상 전투
```

---

## ?? 완료!

말벌의 자동 AI 시스템이 완성되었습니다!

**핵심 기능:**
- ? 시야 범위(5칸) 내 자동 타겟 감지
- ? 자동 추격 및 공격
- ? 하이브 범위(10칸) 제한
- ? 범위 이탈 시 자동 복귀

**게임 플레이:**
- 플레이어는 말벌 하이브 근처로 조심스럽게 접근
- 말벌이 자동으로 방어
- 하이브에서 멀리 유인하면 추격 중단
- 전략적 플레이 가능

게임 개발 화이팅! ??????
