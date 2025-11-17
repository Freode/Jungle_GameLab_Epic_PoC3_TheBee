# ?? Enemy 시스템 개선 완료

## 완료된 사항

### 1. ? EnemyHiveSpawner 단순화
- Hive 컴포넌트의 기존 로직 활용
- 단순히 프리팹만 생성하고 Faction.Enemy 설정

### 2. ? Enemy 유닛 활동 범위 비활성화
- HexBoundaryHighlighter: Enemy 하이브 범위 표시 안 함
- 플레이어 하이브만 활동 범위 표시

### 3. ? Enemy 유닛 전장의 안개 제거 비활성화
- UnitAgent: Enemy 유닛은 FogOfWarManager에 등록 안 함
- 플레이어만 시야 확보 가능

### 4. ? Enemy 유닛 명령 불가
- UnitCommandPanel: Enemy 유닛 선택 시 명령 버튼 숨김
- TileClickMover: Enemy 유닛 우클릭/타일 명령 차단
- 정보는 표시하되 조작 불가

---

## ?? 수정된 파일

### 1. EnemyHiveSpawner.cs
```csharp
? 단순화:
   - Hive 설정 로직 제거
   - CombatUnit 설정 로직 제거
   - Faction만 Enemy로 설정

? 프리팹 활용:
   - eliteWaspHivePrefab
   - normalWaspHivePrefab
   - (프리팹에 Hive 컴포넌트 미리 설정)
```

### 2. HexBoundaryHighlighter.cs
```csharp
? ShowBoundary() 수정:
   - Enemy 하이브 체크
   - Enemy면 활동 범위 표시 안 함
```

### 3. UnitAgent.cs
```csharp
? RegisterWithFog() 수정:
   - Enemy 유닛은 등록 안 함

? SetPosition() 수정:
   - Enemy 유닛은 FogOfWarManager 업데이트 안 함

? Unregister() 수정:
   - Enemy 유닛은 해제 불필요
```

### 4. UnitCommandPanel.cs
```csharp
? RebuildCommands() 수정:
   - Enemy 유닛은 명령 버튼 생성 안 함
   - 정보만 표시
```

### 5. TileClickMover.cs
```csharp
? OnTileCommand() 수정:
   - Enemy 유닛 선택 시 명령 차단

? HandleRightClick() 수정:
   - Enemy 유닛은 선택만 가능
```

---

## ?? 게임 플레이

### 플레이어 유닛
```
? 선택 가능
? 명령 내릴 수 있음
? 활동 범위 표시됨
? 전장의 안개 제거함
```

### Enemy 유닛
```
? 선택 가능 (정보 확인용)
? 명령 불가
? 활동 범위 표시 안 됨
? 전장의 안개 제거 안 함
```

---

## ?? 시나리오

### 1. 말벌 클릭
```
1. 말벌 좌클릭
   ↓
2. UnitCommandPanel 표시
   - 이름: "말벌"
   - 체력: 60/60
   - 공격력: 12
   ↓
3. 명령 버튼 없음
   ↓
4. 우클릭해도 이동 명령 안 됨
```

### 2. 말벌집 클릭
```
1. 말벌집 좌클릭
   ↓
2. UnitCommandPanel 표시
   - 이름: "말벌집"
   - 체력: 250/250
   - 공격력: (비표시)
   ↓
3. 명령 버튼 없음
   ↓
4. 활동 범위 표시 안 됨
```

### 3. 플레이어 일꾼 클릭
```
1. 일꾼 좌클릭
   ↓
2. UnitCommandPanel 표시
   - 이름: "일꾼 꿀벌"
   - 체력: 50/50
   - 공격력: 10
   ↓
3. 명령 버튼 표시
   - [이동] [채집] [공격] 등
   ↓
4. 우클릭으로 이동 가능
```

---

## ?? 시스템 비교

| 기능 | Player | Enemy |
|------|--------|-------|
| 선택 가능 | ? | ? |
| 정보 표시 | ? | ? |
| 명령 내리기 | ? | ? |
| 활동 범위 표시 | ? | ? |
| 전장의 안개 제거 | ? | ? |
| 우클릭 이동 | ? | ? |

---

## ?? 코드 예시

### Enemy 유닛 판별
```csharp
if (agent.faction == Faction.Enemy)
{
    // Enemy 처리
    return;
}
```

### Enemy 하이브 생성
```csharp
// 프리팹 생성
GameObject hive = Instantiate(waspHivePrefab, position, Quaternion.identity);

// Faction만 설정 (나머지는 프리팹에서 처리)
var agent = hive.GetComponent<UnitAgent>();
if (agent != null)
{
    agent.faction = Faction.Enemy;
}
```

### Enemy 명령 차단
```csharp
// UnitCommandPanel
if (currentAgent.faction == Faction.Enemy)
{
    Debug.Log("적 유닛은 명령을 내릴 수 없습니다.");
    return; // 명령 버튼 생성 안 함
}
```

---

## ?? 프리팹 설정 가이드

### 말벌집 프리팹 (WaspHive)
```
WaspHive GameObject
├─ UnitAgent (컴포넌트)
│  ├─ Faction: Player (런타임에 Enemy로 변경됨)
│  └─ Can Move: false
├─ Hive (컴포넌트)
│  ├─ Worker Prefab: Wasp 프리팹
│  ├─ Max Workers: 15
│  └─ Spawn Interval: 8
├─ CombatUnit (컴포넌트)
│  ├─ Max Health: 250
│  ├─ Health: 250
│  └─ Attack: 0
└─ SpriteRenderer (컴포넌트)
   └─ Sprite: 말벌집 이미지
```

### 말벌 프리팹 (Wasp)
```
Wasp GameObject
├─ UnitAgent (컴포넌트)
│  ├─ Faction: Enemy
│  └─ Can Move: true
├─ CombatUnit (컴포넌트)
│  ├─ Max Health: 60
│  ├─ Health: 60
│  └─ Attack: 12
├─ UnitController (컴포넌트)
│  └─ Move Speed: 2.0
└─ SpriteRenderer (컴포넌트)
   └─ Sprite: 말벌 이미지
```

---

## ?? 디버그 로그

### 말벌 선택 시
```
[명령 UI] 적 유닛은 명령을 내릴 수 없습니다.
```

### 말벌에게 명령 시도 시
```
적 유닛은 명령을 내릴 수 없습니다.
```

### 말벌집 활동 범위 표시 시도 시
```
HexBoundaryHighlighter.ShowBoundary ignored: Enemy hive
```

---

## ? 테스트 체크리스트

### Enemy 유닛 상호작용
```
[ ] 말벌 클릭 → 정보 표시됨
[ ] 말벌 클릭 → 명령 버튼 없음
[ ] 말벌 우클릭 → 이동 안 됨
[ ] 말벌집 클릭 → 정보 표시됨
[ ] 말벌집 클릭 → 명령 버튼 없음
[ ] 말벌집 클릭 → 활동 범위 표시 안 됨
```

### Player 유닛 상호작용
```
[ ] 일꾼 클릭 → 정보 표시됨
[ ] 일꾼 클릭 → 명령 버튼 표시됨
[ ] 일꾼 우클릭 → 이동 가능
[ ] 꿀벌집 클릭 → 활동 범위 표시됨
```

### 전장의 안개
```
[ ] Player 유닛 주변 → 안개 제거됨
[ ] Enemy 유닛 주변 → 안개 그대로 (제거 안 됨)
```

---

## ?? 완료!

Enemy 시스템이 완벽하게 구현되었습니다!

**핵심 개선:**
- ? EnemyHiveSpawner 단순화 (Hive 로직 활용)
- ? Enemy 유닛 활동 범위 비표시
- ? Enemy 유닛 안개 제거 비활성화
- ? Enemy 유닛 명령 불가 (정보만 표시)

**게임 플레이:**
- 플레이어는 적 정보를 확인할 수 있음
- 하지만 적을 직접 조작할 수는 없음
- 적은 자율적으로 행동 (AI 로직)

게임 개발 화이팅! ??????
