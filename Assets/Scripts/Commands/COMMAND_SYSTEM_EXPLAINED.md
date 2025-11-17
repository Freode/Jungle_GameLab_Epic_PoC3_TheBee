# SOCommand vs SOUpgradeCommand 설명

## 왜 두 개가 있나요?

### 구조 설명

```
ICommand (인터페이스)
    ↑
SOCommand (기본 구현)
    ↑
SOUpgradeCommand (업그레이드 전용 확장)
```

## SOCommand (기본 명령)

**목적**: 모든 유닛 명령의 기본 클래스

**사용 예시**:
- 이동 (move)
- 하이브 건설 (construct_hive)
- 하이브 이동 (relocate_hive)
- 탐험 (hive_explore)
- 채집 (hive_gather)
- 공격 (hive_attack)

**특징**:
- `id` 기반으로 명령 식별
- `switch-case`로 핸들러에 라우팅
- 타겟이 필요할 수 있음 (`requiresTarget`)

## SOUpgradeCommand (업그레이드 전용)

**목적**: 업그레이드 명령을 더 편하게 만들기 위한 전용 클래스

**장점**:
1. **Inspector가 더 깔끔함**: `upgradeType` 드롭다운에서 선택
2. **타입 안전성**: enum으로 업그레이드 종류 보장
3. **코드 명확성**: 업그레이드 전용임이 명확함

**특징**:
- `upgradeType` enum으로 업그레이드 지정
- 항상 `requiresTarget = false`
- 하이브에서만 사용 가능
- `UpgradeCommandHandler`에 위임

## 비교표

| 특징 | SOCommand | SOUpgradeCommand |
|------|-----------|------------------|
| **타겟 필요** | 명령마다 다름 | 항상 불필요 |
| **설정 방법** | id 문자열 입력 | upgradeType 드롭다운 |
| **사용 대상** | 모든 유닛 | 하이브만 |
| **실수 가능성** | id 오타 가능 | enum으로 방지 |
| **확장성** | switch-case 추가 필요 | 새 enum 값만 추가 |

## Unity에서 사용하는 방법

### SOCommand로 업그레이드 만들기

```
1. Create > Commands > SOCommand
2. 설정:
   - Id: "upgrade_worker_attack"
   - Display Name: "공격력 증가"
   - Requires Target: false
   - Resource Cost: 15
```

**단점**: id를 정확히 입력해야 함 (오타 가능)

### SOUpgradeCommand로 만들기 (추천)

```
1. Create > Commands > UpgradeCommand
2. 설정:
   - Display Name: "공격력 증가"
   - Upgrade Type: WorkerAttack (드롭다운에서 선택)
   - Resource Cost: 15
```

**장점**: 
- 드롭다운에서 선택하므로 오타 없음
- 어떤 업그레이드인지 명확함

## 코드 흐름

### SOCommand 사용 시

```
1. 플레이어가 버튼 클릭
2. SOCommand.Execute() 호출
3. switch (id) { case "upgrade_worker_attack": ... }
4. UpgradeCommandHandler.ExecuteUpgrade() 호출
5. HiveManager.UpgradeWorkerAttack() 실행
```

### SOUpgradeCommand 사용 시

```
1. 플레이어가 버튼 클릭
2. SOUpgradeCommand.Execute() 호출
3. UpgradeCommandHandler.ExecuteUpgrade(upgradeType, cost)
4. HiveManager.UpgradeWorkerAttack() 실행
```

**차이점**: SOUpgradeCommand는 switch-case를 건너뛰고 바로 실행

## 언제 어떤 것을 사용하나요?

### SOCommand 사용

- **타겟이 필요한 명령**: 이동, 공격, 채집
- **기존 명령 시스템 확장**: 새로운 일반 명령 추가
- **조건부 동작**: id에 따라 다른 동작

### SOUpgradeCommand 사용 (추천)

- **업그레이드 명령**: 모든 업그레이드
- **타입 안전성 필요**: enum으로 보장
- **Inspector 편의성**: 드롭다운 선택

## 호환성

**완벽하게 호환됩니다!**

- 둘 다 `ICommand` 인터페이스 구현
- 둘 다 `Hive.hiveCommands` 배열에 추가 가능
- `UnitCommandPanel`에서 동일하게 처리됨

```csharp
// Hive.cs
public SOCommand[] hiveCommands; // SOCommand와 SOUpgradeCommand 둘 다 가능!

// 사용 예시
hiveCommands = new SOCommand[] {
    moveCommand,           // SOCommand
    upgradeAttackCommand,  // SOUpgradeCommand
    relocateCommand        // SOCommand
};
```

## 결론

### 왜 두 개를 유지하나요?

1. **유연성**: 
   - SOCommand: 일반 명령용
   - SOUpgradeCommand: 업그레이드 전용

2. **편의성**:
   - 업그레이드는 SOUpgradeCommand가 더 편함
   - 일반 명령은 SOCommand로 충분

3. **확장성**:
   - 새 업그레이드: SOUpgradeCommand + enum 추가
   - 새 일반 명령: SOCommand + switch-case 추가

### 추천 사용법

```
? 업그레이드 = SOUpgradeCommand 사용
? 일반 명령 = SOCommand 사용
? 섞어서 사용 가능 (둘 다 호환됨)
```

## 실제 사용 예시

### 하이브 명령 설정

```csharp
[Header("Hive Commands")]
public SOCommand[] hiveCommands = new SOCommand[] {
    // 일반 명령들 (SOCommand)
    relocateHiveCommand,
    exploreCommand,
    gatherCommand,
    attackCommand,
    
    // 업그레이드들 (SOUpgradeCommand)
    upgradeRangeCommand,
    upgradeAttackCommand,
    upgradeHealthCommand,
    upgradeSpeedCommand,
    upgradeHiveHealthCommand,
    upgradeMaxWorkersCommand,
    upgradeGatherCommand
};
```

### Inspector에서 보이는 모습

**SOCommand (일반)**:
```
Relocate Hive
├─ Id: "relocate_hive"
├─ Display Name: "하이브 이동"
├─ Requires Target: true
└─ Resource Cost: 50
```

**SOUpgradeCommand (업그레이드)**:
```
Upgrade Worker Attack
├─ Display Name: "공격력 증가"
├─ Upgrade Type: [WorkerAttack ▼]  ← 드롭다운!
├─ Requires Target: false
└─ Resource Cost: 15
```

## 마이그레이션 가이드

### SOCommand → SOUpgradeCommand로 변경하려면?

```
1. 기존 업그레이드 SOCommand 삭제
2. Create > Commands > UpgradeCommand
3. upgradeType 드롭다운에서 선택
4. 나머지 설정 복사
5. Hive.hiveCommands 배열에 다시 추가
```

### SOUpgradeCommand → SOCommand로 변경하려면?

```
1. Create > Commands > SOCommand
2. Id를 "upgrade_" + 타입 으로 설정
   예: "upgrade_worker_attack"
3. requiresTarget = false 체크 해제
4. Hive.hiveCommands 배열에 다시 추가
```

둘 다 동일하게 작동하지만, **SOUpgradeCommand가 더 편리하고 안전합니다!**
