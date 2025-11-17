# 하이브 명령 UI 업그레이드 시스템

## 개요
하이브 명령 UI에 7가지 영구 업그레이드를 추가했습니다. 플레이어는 꿀 자원을 소모하여 다양한 능력치를 향상시킬 수 있습니다.

## 업그레이드 목록

| 업그레이드 | 효과 | 기본값 | 증가량 |
|-----------|------|--------|--------|
| 하이브 활동 범위 | 일꾼 활동 가능 타일 | 5 | +1 |
| 일꾼 공격력 | 전투 데미지 | 10 | +1 |
| 일꾼 체력 | 최대 HP | 50 | +5 |
| 일꾼 이동 속도 | 이동 속도 | 2.0 | +1 |
| 하이브 체력 | 하이브 최대 HP | 200 | +30 |
| 최대 일꾼 수 | 하이브당 일꾼 제한 | 10 | +5 |
| 자원 채취량 | 한 번에 채취하는 양 | 1 | +2 |

## Unity 에디터 설정

### 1. 업그레이드 명령 생성

각 업그레이드마다 ScriptableObject를 만들어야 합니다.

#### 생성 방법
1. Project 창에서 우클릭
2. `Create > Commands > UpgradeCommand` 선택
3. 이름 변경 (예: `Upgrade_HiveRange`)

### 2. 업그레이드 명령 설정 예시

#### 하이브 활동 범위 업그레이드
```
이름: Upgrade_HiveRange
Display Name: 활동 범위 확장
Id: upgrade_hive_range
Icon: [범위 아이콘]
Requires Target: false (체크 해제)
Hide Panel On Click: false
Resource Cost: 20
Upgrade Type: HiveRange
```

#### 일꾼 공격력 업그레이드
```
이름: Upgrade_WorkerAttack
Display Name: 날카로운 침
Id: upgrade_worker_attack
Icon: [공격 아이콘]
Requires Target: false
Hide Panel On Click: false
Resource Cost: 15
Upgrade Type: WorkerAttack
```

#### 일꾼 체력 업그레이드
```
이름: Upgrade_WorkerHealth
Display Name: 강화 외골격
Id: upgrade_worker_health
Icon: [방어 아이콘]
Requires Target: false
Hide Panel On Click: false
Resource Cost: 20
Upgrade Type: WorkerHealth
```

#### 일꾼 이동 속도 업그레이드
```
이름: Upgrade_WorkerSpeed
Display Name: 빠른 날개
Id: upgrade_worker_speed
Icon: [속도 아이콘]
Requires Target: false
Hide Panel On Click: false
Resource Cost: 15
Upgrade Type: WorkerSpeed
```

#### 하이브 체력 업그레이드
```
이름: Upgrade_HiveHealth
Display Name: 강화 벌집
Id: upgrade_hive_health
Icon: [방어 아이콘]
Requires Target: false
Hide Panel On Click: false
Resource Cost: 25
Upgrade Type: HiveHealth
```

#### 최대 일꾼 수 업그레이드
```
이름: Upgrade_MaxWorkers
Display Name: 확장 군집
Id: upgrade_max_workers
Icon: [인구 아이콘]
Requires Target: false
Hide Panel On Click: false
Resource Cost: 30
Upgrade Type: MaxWorkers
```

#### 자원 채취량 업그레이드
```
이름: Upgrade_GatherAmount
Display Name: 효율적 채집
Id: upgrade_gather_amount
Icon: [자원 아이콘]
Requires Target: false
Hide Panel On Click: false
Resource Cost: 20
Upgrade Type: GatherAmount
```

### 3. 하이브 프리팹에 명령 추가

1. 하이브 프리팹 선택
2. Inspector에서 `Hive (Script)` 컴포넌트 찾기
3. `Hive Commands` 배열 확장
4. 생성한 7개의 업그레이드 명령을 배열에 드래그 앤 드롭

### 4. 업그레이드 타입 설명

```csharp
public enum UpgradeType
{
    HiveRange,      // 하이브 활동 범위 +1
    WorkerAttack,   // 일꾼 공격력 +1
    WorkerHealth,   // 일꾼 체력 +5
    WorkerSpeed,    // 일꾼 이동 속도 +1
    HiveHealth,     // 하이브 체력 +30
    MaxWorkers,     // 최대 일꾼 수 +5
    GatherAmount    // 자원 채취량 +2
}
```

## 사용 방법

### 게임 플레이
1. **하이브 선택**: 하이브 클릭
2. **명령 패널 확인**: 화면에 업그레이드 버튼들 표시
3. **업그레이드 클릭**: 
   - 자원이 충분하면 즉시 적용
   - 자원이 부족하면 버튼 비활성화 (회색)
4. **효과 확인**:
   - 콘솔에 "[업그레이드] ..." 메시지 출력
   - 기존 유닛/하이브에 즉시 반영
   - 새로 생성되는 유닛도 자동 적용

### 업그레이드 효과 적용

#### 즉시 적용되는 대상
- **하이브 활동 범위**: 모든 하이브의 경계선 즉시 갱신
- **일꾼 스탯**: 모든 기존 일꾼의 스탯 즉시 업데이트
- **하이브 체력**: 모든 하이브의 체력 비율 유지하며 증가
- **최대 일꾼 수**: 모든 하이브의 제한 즉시 증가

#### 새로 생성되는 유닛
- 하이브에서 새로 스폰되는 일꾼은 자동으로 업그레이드된 스탯 적용
- 추가 설정 불필요

## 코드 구조

### HiveManager.cs
```csharp
// 업그레이드 레벨 추적
public int hiveRangeLevel = 0;
public int workerAttackLevel = 0;
// ... 등등

// 업그레이드 메서드
public bool UpgradeHiveRange(int cost)
public bool UpgradeWorkerAttack(int cost)
// ... 등등

// 스탯 계산 메서드
public int GetWorkerAttack()
public float GetWorkerSpeed()
// ... 등등
```

### SOUpgradeCommand.cs
```csharp
public class SOUpgradeCommand : SOCommand
{
    public UpgradeType upgradeType;
    
    public override void Execute(UnitAgent agent, CommandTarget target)
    {
        // HiveManager의 업그레이드 메서드 호출
    }
}
```

### Hive.cs
```csharp
public void Initialize(int q, int r)
{
    // 하이브 초기화 시 업그레이드 스탯 적용
    maxWorkers = HiveManager.Instance.GetMaxWorkers();
    combat.maxHealth = HiveManager.Instance.GetHiveMaxHealth();
}

void SpawnWorker()
{
    // 새 일꾼 생성 시 업그레이드 스탯 적용
    combat.attack = HiveManager.Instance.GetWorkerAttack();
    combat.maxHealth = HiveManager.Instance.GetWorkerMaxHealth();
    controller.moveSpeed = HiveManager.Instance.GetWorkerSpeed();
    behavior.gatherAmount = HiveManager.Instance.GetGatherAmount();
}
```

## 추천 비용 설정

| 업그레이드 | 추천 비용 | 이유 |
|-----------|----------|------|
| 하이브 활동 범위 | 20-30 | 전략적으로 중요 |
| 일꾼 공격력 | 15-20 | 전투력 강화 |
| 일꾼 체력 | 20-25 | 생존력 증가 |
| 일꾼 이동 속도 | 15-20 | 효율성 향상 |
| 하이브 체력 | 25-35 | 방어력 강화 |
| 최대 일꾼 수 | 30-40 | 경제력 확장 |
| 자원 채취량 | 20-30 | 경제력 강화 |

## 밸런스 조정

### 증가량 수정
`HiveManager.cs`의 Get 메서드들을 수정하세요:

```csharp
// 공격력을 +2씩 증가시키려면
public int GetWorkerAttack()
{
    return 10 + (workerAttackLevel * 2);  // 기존: workerAttackLevel
}

// 체력을 +10씩 증가시키려면
public int GetWorkerMaxHealth()
{
    return 50 + (workerHealthLevel * 10);  // 기존: * 5
}
```

### 기본값 수정
```csharp
// 일꾼 기본 공격력을 15로 변경
public int GetWorkerAttack()
{
    return 15 + workerAttackLevel;  // 기존: 10
}
```

## 테스트 방법

1. **게임 실행**
2. **자원 수집**: 일꾼으로 자원 타일 채취
3. **하이브 선택**: 하이브 클릭하여 명령 패널 열기
4. **업그레이드**: 각 업그레이드 버튼 클릭
5. **효과 확인**:
   - 콘솔 로그 확인
   - 일꾼 스탯 확인 (전투 시 데미지, 이동 속도 등)
   - 하이브 경계선 확장 확인
   - 새로 생성되는 일꾼의 스탯 확인

## 문제 해결

### 업그레이드 버튼이 비활성화됨
- HiveManager가 씬에 있는지 확인
- 자원이 충분한지 확인 (HiveUI로 현재 자원 확인)

### 효과가 적용되지 않음
- TileManager가 씬에 있는지 확인
- 콘솔에서 "[업그레이드] ..." 메시지 확인
- 새로 생성된 유닛으로 테스트

### 경계선이 업데이트되지 않음
- HexBoundaryHighlighter가 씬에 있는지 확인
- HiveManager.hiveActivityRadius 값 Inspector에서 확인

## 확장 가이드

### 새로운 업그레이드 추가

1. **UpgradeType enum에 추가**:
```csharp
public enum UpgradeType
{
    // 기존...
    NewUpgrade  // 새 업그레이드
}
```

2. **HiveManager에 레벨 변수 추가**:
```csharp
public int newUpgradeLevel = 0;
```

3. **업그레이드 메서드 추가**:
```csharp
public bool UpgradeNew(int cost)
{
    if (!TrySpendResources(cost)) return false;
    newUpgradeLevel++;
    UpdateNewEffect();
    return true;
}
```

4. **스탯 계산 메서드 추가**:
```csharp
public int GetNewStat()
{
    return baseValue + newUpgradeLevel;
}
```

5. **SOUpgradeCommand.Execute에 case 추가**:
```csharp
case UpgradeType.NewUpgrade:
    success = HiveManager.Instance.UpgradeNew(resourceCost);
    break;
```

## 주의사항

?? **중요**: 
- 업그레이드는 영구적이며 되돌릴 수 없습니다
- 반복 구매 가능 (레벨 제한 없음)
- 모든 하이브와 일꾼에게 동시 적용됩니다

?? **팁**:
- 레벨 제한을 추가하려면 `UpgradeXXX` 메서드에서 레벨 체크 추가
- 비용을 레벨에 따라 증가시키려면 비용 계산 공식 수정
- UI에 현재 레벨 표시를 원하면 UnitCommandPanel 수정

## 파일 구조

```
Assets/
├── Scripts/
│   ├── Manager/
│   │   └── HiveManager.cs       (업그레이드 시스템 + 스탯 관리)
│   ├── Hive/
│   │   └── Hive.cs              (새 유닛에 스탯 적용)
│   └── Commands/
│       └── SOUpgradeCommand.cs  (업그레이드 명령)
└── ScriptableObjects/
    └── Commands/
        └── Upgrades/
            ├── Upgrade_HiveRange.asset
            ├── Upgrade_WorkerAttack.asset
            ├── Upgrade_WorkerHealth.asset
            ├── Upgrade_WorkerSpeed.asset
            ├── Upgrade_HiveHealth.asset
            ├── Upgrade_MaxWorkers.asset
            └── Upgrade_GatherAmount.asset
```
