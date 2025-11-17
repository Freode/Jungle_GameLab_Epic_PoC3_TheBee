# ?? 개선 사항 가이드

## 완료된 개선사항

### 1. ? 드래그 선택에서 벌집 제외
### 2. ? 일꾼 체력 업그레이드 시 현재 체력 증가
### 3. ? 적 하이브 생성 시스템 구축

---

## 1. 드래그 선택에서 벌집 제외 ?

### 변경 사항
```csharp
// DragSelector.cs - SelectUnitsInDragArea()

// 하이브(벌집)도 제외
var hive = unit.GetComponent<Hive>();
if (hive != null) continue;
```

### 결과
```
드래그 선택 시:
? 일꾼 꿀벌만 선택됨
? 벌집은 선택 안 됨
? 여왕벌도 선택 안 됨
```

### 테스트
```
1. 벌집과 일꾼이 함께 있는 곳에 드래그
2. 일꾼만 선택되고 벌집은 제외됨
3. Console: "[드래그 선택] N개의 일꾼 선택됨"
```

---

## 2. 일꾼 체력 업그레이드 시 현재 체력 증가 ?

### 문제점
```
업그레이드 전: HP 45/50
업그레이드 후: HP 45/55 ? (최대 체력만 증가)
```

### 해결
```
업그레이드 전: HP 45/50
업그레이드 후: HP 50/55 ? (현재 체력도 +5 증가)
```

### 코드 변경
```csharp
// HiveManager.cs - UpdateAllWorkerCombat()

int oldMaxHealth = combat.maxHealth;
int newMaxHealth = GetWorkerMaxHealth();
int healthIncrease = newMaxHealth - oldMaxHealth;

combat.maxHealth = newMaxHealth;
combat.health = Mathf.Min(combat.health + healthIncrease, combat.maxHealth);
```

### 동작 원리
```
1. 체력 업그레이드 (+5)
2. 최대 체력 증가: 50 → 55
3. 현재 체력도 증가: 45 → 50
4. 체력 초과 방지: Min(50, 55) = 50
```

### 예시
```
케이스 1: 부상당한 일꾼
- 업그레이드 전: 30/50
- 업그레이드 후: 35/55 (+5)

케이스 2: 만피 일꾼
- 업그레이드 전: 50/50
- 업그레이드 후: 55/55 (+5)

케이스 3: 거의 죽을뻔한 일꾼
- 업그레이드 전: 5/50
- 업그레이드 후: 10/55 (+5)
```

---

## 3. 적 하이브 생성 시스템 ?

### 개요
```
장수말벌집: (0, 0) 위치에 1개만 생성
일반 말벌집: 거리 4~9 사이, 최소 간격 3 이상으로 랜덤 생성
```

### 생성 규칙

#### 장수말벌집 (Elite Wasp Hive)
```
위치: (0, 0) - 맵 정중앙
개수: 1개
체력: 400 (기본값)
특징: 보스급 하이브
```

#### 일반 말벌집 (Normal Wasp Hive)
```
위치: 장수말벌집으로부터 거리 4~9 사이
개수: 설정 가능 (기본 3개)
체력: 250 (기본값)
최소 간격: 3 이상 (서로 너무 붙지 않음)
```

### Unity 설정

#### 1단계: GameObject 생성
```
Hierarchy에서:
1. Create Empty → "EnemyHiveSpawner"
2. EnemyHiveSpawner.cs 컴포넌트 추가
```

#### 2단계: Inspector 설정
```yaml
Enemy Hive Spawner (Script)
├─ 프리팹
│  ├─ Elite Wasp Hive Prefab: [장수말벌집 프리팹]
│  ├─ Normal Wasp Hive Prefab: [일반 말벌집 프리팹]
│  └─ Wasp Prefab: [말벌 프리팹]
│
├─ 생성 설정
│  ├─ Normal Hive Count: 3
│  ├─ Min Distance From Elite: 4
│  ├─ Max Distance From Elite: 9
│  └─ Min Distance Between Hives: 3
│
└─ 하이브 능력치
   ├─ Elite Hive Health: 400
   ├─ Normal Hive Health: 250
   ├─ Wasp Spawn Interval: 8
   └─ Max Wasps Per Hive: 15
```

### 사용 방법

#### 방법 1: 게임 시작 시 자동 생성
```csharp
// EnemyHiveSpawner.cs - Start()
void Start()
{
    SpawnAllEnemyHives(); // 주석 해제
}
```

#### 방법 2: 수동 생성
```csharp
// 게임 중 원하는 시점에
EnemyHiveSpawner.Instance.SpawnAllEnemyHives();
```

#### 방법 3: UI 버튼으로 생성
```csharp
// 버튼 OnClick 이벤트에 연결
public void OnSpawnEnemiesButtonClick()
{
    EnemyHiveSpawner.Instance.SpawnAllEnemyHives();
}
```

### API

```csharp
// 모든 적 하이브 생성
EnemyHiveSpawner.Instance.SpawnAllEnemyHives();

// 장수말벌집 가져오기
GameObject eliteHive = EnemyHiveSpawner.Instance.GetEliteHive();

// 일반 말벌집 목록 가져오기
List<GameObject> normalHives = EnemyHiveSpawner.Instance.GetNormalHives();

// 모든 적 하이브 제거
EnemyHiveSpawner.Instance.ClearAllEnemyHives();

// 특정 하이브 제거 (파괴됐을 때)
EnemyHiveSpawner.Instance.UnregisterHive(hiveGameObject);
```

### 생성 예시

#### 시나리오 1: 기본 생성
```
장수말벌집: (0, 0)
일반 말벌집 1: (5, 3)  - 거리 8
일반 말벌집 2: (-4, 6) - 거리 7, 간격 4
일반 말벌집 3: (6, -5) - 거리 6, 간격 5
```

#### 시나리오 2: 밀집 생성
```
Min Distance Between Hives: 3

장수말벌집: (0, 0)
일반 말벌집 1: (4, 0)  - 거리 4
일반 말벌집 2: (0, 4)  - 거리 4, 간격 4
일반 말벌집 3: (-4, 0) - 거리 4, 간격 4
```

### 위치 검증 알고리즘

```csharp
1. 랜덤 거리(4~9) 및 방향 생성
2. Hex 좌표로 변환
3. 실제 거리 확인 (4~9 범위 내인지)
4. 기존 하이브들과의 거리 확인 (최소 3 이상)
5. 타일 존재 여부 확인
6. 유효하면 생성, 아니면 재시도 (최대 50회)
```

### 디버그 로그

```
[적 하이브 생성] 장수말벌집 생성: (0, 0)
[적 하이브 생성] 일반 말벌집 생성: (5, 3)
[적 하이브 생성] 일반 말벌집 생성: (-4, 6)
[적 하이브 생성] 일반 말벌집 생성: (6, -5)
[적 하이브 생성] 장수말벌집 1개, 일반 말벌집 3개 생성 완료
```

---

## ?? 전체 시스템 요약

### 1. 드래그 선택
```
필터링:
? 일꾼 꿀벌만 선택
? 벌집 제외
? 여왕벌 제외
```

### 2. 체력 업그레이드
```
최대 체력 증가: +5
현재 체력 증가: +5
→ 모든 일꾼의 생존력 향상
```

### 3. 적 하이브 생성
```
장수말벌집: (0, 0)
├─ 체력: 400
└─ 말벌 생성: 15마리까지

일반 말벌집: 거리 4~9
├─ 체력: 250
├─ 최소 간격: 3
└─ 말벌 생성: 15마리까지
```

---

## ?? 게임 플레이 흐름

### 초기 상태
```
1. 플레이어 하이브 생성
2. 적 하이브 생성
   - 장수말벌집 (0, 0)
   - 일반 말벌집 3개 랜덤 배치
3. 양측 하이브에서 유닛 생성 시작
```

### 전투 흐름
```
1. 플레이어: 일꾼들을 드래그로 선택
   - 벌집은 선택 안 됨 ?
2. 적 하이브 근처로 이동
3. 자동 전투 시작
4. 체력 업그레이드로 생존력 향상
   - 현재 체력도 함께 증가 ?
5. 적 하이브 파괴
```

---

## ?? 커스터마이징

### 적 하이브 난이도 조정

#### 쉬움
```yaml
Normal Hive Count: 2
Elite Hive Health: 300
Normal Hive Health: 200
Max Wasps Per Hive: 10
Wasp Spawn Interval: 10
```

#### 보통
```yaml
Normal Hive Count: 3
Elite Hive Health: 400
Normal Hive Health: 250
Max Wasps Per Hive: 15
Wasp Spawn Interval: 8
```

#### 어려움
```yaml
Normal Hive Count: 5
Elite Hive Health: 600
Normal Hive Health: 350
Max Wasps Per Hive: 20
Wasp Spawn Interval: 6
```

---

## ? 테스트 체크리스트

### 드래그 선택
```
[ ] 일꾼만 드래그 → 선택됨
[ ] 벌집 포함 드래그 → 일꾼만 선택됨
[ ] 여왕벌 포함 드래그 → 일꾼만 선택됨
[ ] 여러 마리 동시 선택 확인
```

### 체력 업그레이드
```
[ ] 만피 일꾼: 50/50 → 55/55
[ ] 부상 일꾼: 30/50 → 35/55
[ ] 업그레이드 후 Console 메시지 확인
[ ] 모든 기존 일꾼에 적용 확인
```

### 적 하이브 생성
```
[ ] 장수말벌집 (0, 0) 생성 확인
[ ] 일반 말벌집 3개 생성 확인
[ ] 거리 4~9 범위 확인
[ ] 최소 간격 3 이상 확인
[ ] 말벌 자동 생성 확인
[ ] Console 로그 확인
```

---

## ?? 완료!

모든 개선사항이 적용되었습니다!

**핵심 개선:**
- ? 드래그 선택 정확도 향상 (벌집 제외)
- ? 체력 업그레이드 만족도 향상 (현재 체력 증가)
- ? 적 진영 시스템 완성 (장수말벌집 + 일반 말벌집)

게임 개발 화이팅! ??????
