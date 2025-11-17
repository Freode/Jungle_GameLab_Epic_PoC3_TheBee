# ?? Enemy 가시성 & AI 개선 가이드

## 완료된 개선사항

### 1. ? Enemy 유닛 실시간 가시성 제어
- 플레이어 시야 내에만 보임
- 시야에서 벗어나면 자동 숨김
- 전장의 안개가 걷혀있어도 현재 시야 밖이면 안 보임

### 2. ? 말벌 하이브 근처 자유 이동
- 하이브 1칸 이내에서는 활동 범위 제한 없음
- 하이브 근처에서 유연한 방어 가능

---

## ?? 생성된 파일

### EnemyVisibilityController.cs
```csharp
? 실시간 플레이어 시야 계산
? Enemy 유닛 가시성 자동 제어
? 0.2초마다 업데이트 (최적화)
```

### EnemyAI.cs (수정)
```csharp
? IsWithinHiveRange() 개선
   - 하이브 1칸 이내면 항상 허용
   
? ReturnToHive() 개선
   - 하이브 1칸 이내면 복귀 안 함
```

---

## ?? 시스템 작동 방식

### Enemy 가시성 제어

#### 플레이어 시야 계산
```
1. 모든 플레이어 유닛의 위치 확인
   ↓
2. 각 유닛의 visionRange 내 타일 계산
   ↓
3. 모든 시야 범위를 합산 (HashSet)
   ↓
4. currentVisibleTiles에 저장
```

#### Enemy 가시성 적용
```
1. 모든 Enemy 유닛 순회
   ↓
2. 유닛 위치가 currentVisibleTiles 내에 있는지 확인
   ↓
3. 있으면 → 렌더러 활성화 (보임)
   ↓
4. 없으면 → 렌더러 비활성화 (안 보임)
```

---

## ?? 시나리오 예시

### 시나리오 1: 말벌 정찰

```
[플레이어]
일꾼 1마리 (시야: 3칸)

[말벌]
말벌집 근처 대기 중

[상황 1] 일꾼이 말벌집 근처로 이동
   ↓
일꾼 시야(3칸) 내에 말벌 진입
   ↓
말벌이 보이기 시작 ?
   ↓
말벌 AI 작동 → 일꾼 추격

[상황 2] 일꾼이 도망
   ↓
말벌이 시야(3칸) 밖으로 이탈
   ↓
말벌이 안 보임 ?
   ↓
(하지만 말벌은 계속 추격 중 - 활동 범위 내면)
   ↓
일꾼이 다시 가까워지면
   ↓
말벌이 다시 보임 ?
```

### 시나리오 2: 하이브 방어

```
[말벌집]
위치: (0, 0)

[말벌 3마리]
위치: (0, 0), (1, 0), (0, 1) - 하이브 1칸 이내

[상황 1] 플레이어 일꾼 접근
   ↓
말벌들이 일꾼 감지
   ↓
말벌 1: 하이브에서 멀리 추격 (활동 범위 내)
말벌 2, 3: 하이브 근처에서 대기 ?
   ↓
말벌 2, 3은 하이브 1칸 이내에서 자유롭게 이동 가능

[상황 2] 일꾼이 하이브 직접 공격
   ↓
말벌 2, 3이 즉시 반응
   ↓
하이브 주변(1칸)에서 유연하게 방어 ?
```

### 시나리오 3: 전장의 안개

```
[이전 시스템]
전장의 안개 걷힌 곳 = 말벌이 항상 보임 ?

[새 시스템]
전장의 안개 걷힌 곳 + 현재 시야 내 = 말벌이 보임 ?

[예시]
1. 일꾼이 말벌집 근처 방문 → 안개 걷힘
2. 일꾼이 멀리 이동 → 안개는 걷힌 상태
3. 말벌이 그 위치에 있어도 → 현재 시야 밖이므로 안 보임 ?
4. 일꾼이 다시 가까이 가면 → 말벌이 보임 ?
```

---

## ?? Unity 설정

### 1단계: EnemyVisibilityController 추가

```
Hierarchy:
1. Create Empty → "EnemyVisibilityController"
2. EnemyVisibilityController.cs 추가
```

### 2단계: Inspector 설정

```yaml
Enemy Visibility Controller (Script)
└─ 설정
   └─ Update Interval: 0.2 (초)
```

---

## ?? 코드 구조

### EnemyVisibilityController.cs

#### 핵심 메서드

##### 1. UpdateEnemyVisibility()
```csharp
void UpdateEnemyVisibility()
{
    // 1. 플레이어 시야 계산
    CalculatePlayerVision();
    
    // 2. 모든 Enemy 유닛 순회
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit.faction != Faction.Enemy) continue;
        
        // 3. 시야 내에 있는지 확인
        bool isVisible = currentVisibleTiles.Contains(unitPos);
        
        // 4. 가시성 적용
        SetUnitVisibility(unit, isVisible);
    }
}
```

##### 2. CalculatePlayerVision()
```csharp
void CalculatePlayerVision()
{
    currentVisibleTiles.Clear();
    
    // 모든 플레이어 유닛의 시야 합산
    foreach (var unit in TileManager.Instance.GetAllUnits())
    {
        if (unit.faction != Faction.Player) continue;
        
        // 시야 범위 타일 추가
        var visibleTiles = GetTilesInRange(unit.q, unit.r, unit.visionRange);
        foreach (var tile in visibleTiles)
        {
            currentVisibleTiles.Add(tile);
        }
    }
}
```

##### 3. SetUnitVisibility()
```csharp
void SetUnitVisibility(UnitAgent unit, bool visible)
{
    // SpriteRenderer 제어
    var sprite = unit.GetComponentInChildren<SpriteRenderer>();
    if (sprite != null)
        sprite.enabled = visible;
    
    // Renderer 제어
    var renderer = unit.GetComponentInChildren<Renderer>();
    if (renderer != null)
        renderer.enabled = visible;
    
    // 자식 렌더러들도 제어
    // ...
}
```

### EnemyAI.cs

#### 수정된 메서드

##### IsWithinHiveRange()
```csharp
bool IsWithinHiveRange(int q, int r)
{
    if (homeHive == null) return true;
    
    int distanceToHive = GetDistance(homeHive.q, homeHive.r, q, r);
    
    // ? 하이브 1칸 이내면 항상 허용
    if (distanceToHive <= 1)
        return true;
    
    // 일반 활동 범위 체크
    int distance = GetDistance(homeHive.q, homeHive.r, q, r);
    return distance <= activityRange;
}
```

##### ReturnToHive()
```csharp
void ReturnToHive()
{
    // ...
    
    // ? 하이브 1칸 이내면 복귀하지 않음
    int distanceToHive = GetDistance(agent.q, agent.r, homeHive.q, homeHive.r);
    if (distanceToHive <= 1)
    {
        isChasing = false;
        return;
    }
    
    // 복귀 경로 찾기
    // ...
}
```

---

## ?? 시각적 효과

### Enemy 가시성 변화

```
플레이어 시야 내:
?? 말벌이 보임 (SpriteRenderer.enabled = true)

플레이어 시야 밖:
?? 말벌이 안 보임 (SpriteRenderer.enabled = false)
```

### 하이브 방어 범위

```
        [말벌 자유 이동 구역]
             1칸 이내
                ↓
    ? ? ? ? ?
    ? ? ?? ? ?  ← 하이브
    ? ? ? ? ?
    
    외곽 ?: 활동 범위 제한 적용
    내부 ?: 자유 이동 가능
```

---

## ?? 최적화

### 업데이트 주기 조정

```csharp
// 빠른 반응 (부하 높음)
updateInterval = 0.1f;

// 표준 (권장)
updateInterval = 0.2f; ?

// 느린 반응 (부하 낮음)
updateInterval = 0.5f;
```

### HashSet 사용

```csharp
// ? O(1) 조회 속도
private HashSet<Vector2Int> currentVisibleTiles;

// ? O(n) 조회 속도
private List<Vector2Int> currentVisibleTiles;
```

---

## ?? 비교표

### 가시성 시스템

| 상황 | 이전 | 현재 |
|------|------|------|
| 안개 걷힌 곳 | 항상 보임 | 현재 시야 내에만 보임 ? |
| 안개 안 걷힌 곳 | 안 보임 | 안 보임 |
| 시야 내 | 보임 | 보임 ? |
| 시야 밖 | 보임 (안개 걷힌 경우) | 안 보임 ? |

### 활동 범위

| 위치 | 이전 | 현재 |
|------|------|------|
| 하이브 1칸 이내 | 제한 적용 | 자유 이동 ? |
| 하이브 2~10칸 | 제한 적용 | 제한 적용 |
| 하이브 10칸 초과 | 추격 중단 | 추격 중단 |

---

## ?? 문제 해결

### Q: Enemy가 계속 보여요

**확인사항:**
```
? EnemyVisibilityController가 씬에 있는지
? Update()가 실행되는지
? currentVisibleTiles가 계산되는지
? SetUnitVisibility()가 호출되는지
```

### Q: Enemy가 안 보여요

**확인사항:**
```
? 플레이어 유닛이 있는지
? 플레이어 유닛의 visionRange > 0인지
? Enemy가 시야 범위 내에 있는지
? SpriteRenderer가 있는지
```

### Q: 말벌이 하이브 근처에서 안 움직여요

**확인사항:**
```
? EnemyAI.activityRange 값 확인
? IsWithinHiveRange() 로직 확인
? homeHive 참조 확인
```

---

## ? 테스트 체크리스트

### 가시성 테스트
```
[ ] 플레이어 유닛 근처에 Enemy 배치
[ ] Enemy가 보이는지 확인
[ ] 플레이어 유닛 멀리 이동
[ ] Enemy가 안 보이는지 확인
[ ] 플레이어 유닛 다시 접근
[ ] Enemy가 다시 보이는지 확인
```

### 하이브 이동 테스트
```
[ ] 말벌을 하이브 1칸 이내에 배치
[ ] 말벌이 자유롭게 이동하는지 확인
[ ] 말벌을 하이브 2칸 이상 떨어뜨림
[ ] 활동 범위 제한 작동 확인
[ ] 타겟이 범위 밖으로 도망
[ ] 말벌이 추격 중단 및 복귀 확인
```

### 통합 테스트
```
[ ] 플레이어 일꾼으로 말벌집 접근
[ ] 말벌들이 보이기 시작
[ ] 말벌들이 추격 시작
[ ] 도망가면 말벌이 안 보임
[ ] 말벌은 하이브로 복귀
[ ] 다시 접근하면 말벌이 보임
```

---

## ?? 완료!

Enemy 가시성 및 AI 시스템이 개선되었습니다!

**핵심 개선:**
- ? 실시간 플레이어 시야 기반 가시성
- ? 전장의 안개와 분리된 Enemy 가시성
- ? 하이브 1칸 이내 자유 이동
- ? 유연한 하이브 방어

**게임 플레이 변화:**
- 정찰의 중요성 증가
- 말벌이 더 자연스럽게 방어
- 긴장감 있는 전투
- 전략적 플레이 가능

게임 개발 화이팅! ??????
