# ?? 테크 트리 명령 UI 호환 가이드

## ? 테크 트리를 명령 UI로 여는 방법

### 1?? OpenTechTreeCommand SO 생성

#### Unity Editor에서:
```
1. Project 창에서 우클릭
2. Create → Commands → OpenTechTreeCommand
3. 파일 이름: "OpenTechTree" (또는 원하는 이름)
```

#### Inspector 설정:
```
Id: open_tech_tree
Display Name: 테크 트리
Icon: (테크 트리 아이콘 이미지)
Requires Target: ? (체크 해제)
Hide Panel On Click: ? (체크 해제)
Resource Cost: 0
```

---

### 2?? 하이브 명령에 추가

#### Hive의 IUnitCommandProvider 설정:

```csharp
// Hive.cs에서 명령 리스트에 추가
public List<SOCommand> GetAvailableCommands()
{
    var commands = new List<SOCommand>();
    
    // 기존 명령들...
    commands.Add(relocateHiveCommand);
    commands.Add(exploreCommand);
    commands.Add(gatherCommand);
    commands.Add(attackCommand);
    
    // 테크 트리 열기 명령 추가
    commands.Add(openTechTreeCommand);  // ?? 추가!
    
    // 업그레이드 명령들...
    
    return commands;
}
```

또는 Inspector에서 직접 추가:
```
Hive 컴포넌트
└── Available Commands (List)
    ├── RelocateHive
    ├── Explore
    ├── Gather
    ├── Attack
    ├── OpenTechTree ?? 여기에 드래그 앤 드롭!
    └── ... (업그레이드 명령들)
```

---

### 3?? 작동 방식

#### 사용자 관점:
1. **하이브 선택**
2. **명령 패널**에서 "테크 트리" 버튼 클릭
3. **테크 트리 UI** 열림
4. 명령 패널은 그대로 유지됨 (hidePanelOnClick = false)

#### 기술적 동작:
```csharp
// SOOpenTechTreeCommand.Execute()
TechTreeUI.Instance.OpenTechTreePanel();
```

---

### 4?? 테크 트리 UI 닫기

#### 방법 1: 닫기 버튼
- 테크 트리 패널의 X 버튼 클릭

#### 방법 2: ESC 키
- `TechTreeUI.Update()`에서 ESC 키 감지
```csharp
if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
{
    CloseTechTreePanel();
}
```

#### 방법 3: 프로그래밍
```csharp
TechTreeUI.Instance.CloseTechTreePanel();
```

---

### 5?? 명령 UI와의 호환성

#### ? 호환 가능한 이유:

1. **requiresTarget = false**
   - 타겟 선택이 필요없음
   - 클릭하면 바로 실행

2. **hidePanelOnClick = false**
   - 명령 패널을 숨기지 않음
   - 테크 트리와 명령 패널 동시 사용 가능

3. **resourceCost = 0**
   - 비용이 들지 않음
   - 항상 사용 가능

4. **IsAvailable() 체크**
   - 아군 하이브에서만 사용 가능
   - 적군 하이브에서는 버튼이 비활성화됨

---

### 6?? UI 계층 구조

```
Canvas
├── CommandPanel (항상 활성)
│   └── Commands
│       ├── RelocateHive Button
│       ├── Explore Button
│       ├── Gather Button
│       ├── Attack Button
│       └── OpenTechTree Button ?? 새로 추가
│
└── TechTreePanel (필요시 활성)
    ├── CloseButton (X)
    ├── TabButtons
    │   ├── Worker Tab
    │   ├── Queen Tab
    │   └── Hive Tab
    └── ScrollViews
        ├── WorkerScrollView
        ├── QueenScrollView
        └── HiveScrollView
```

---

### 7?? 추가 기능

#### 토글 기능 (선택사항)
테크 트리를 열고 닫는 토글 버튼을 만들 수도 있습니다:

```csharp
public override void Execute(UnitAgent agent, CommandTarget target)
{
    if (TechTreeUI.Instance != null)
    {
        // 열려있으면 닫고, 닫혀있으면 열기
        TechTreeUI.Instance.ToggleTechTreePanel();
    }
}
```

#### 단축키 조합 (선택사항)
Shift + T로 테크 트리 토글:

```csharp
// TechTreeUI.Update()에 추가
if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T))
{
    ToggleTechTreePanel();
}
```

---

### 8?? 디버깅

#### 테크 트리가 안 열리는 경우:

```csharp
// 1. TechTreeUI.Instance 확인
if (TechTreeUI.Instance == null)
{
    Debug.LogError("TechTreeUI.Instance가 null입니다!");
}

// 2. techTreePanel 확인
if (TechTreeUI.Instance.techTreePanel == null)
{
    Debug.LogError("techTreePanel이 null입니다!");
}

// 3. 명령 실행 로그 확인
Debug.Log("[OpenTechTreeCommand] Execute 호출됨");
```

#### 명령 버튼이 안 보이는 경우:

```csharp
// Hive의 명령 리스트 확인
public override List<SOCommand> GetAvailableCommands()
{
    var commands = base.GetAvailableCommands();
    Debug.Log($"[Hive] 사용 가능한 명령 수: {commands.Count}");
    
    foreach (var cmd in commands)
    {
        Debug.Log($"  - {cmd.displayName}");
    }
    
    return commands;
}
```

---

### 9?? 장점

? **직관적인 접근**
- 하이브를 선택하면 바로 테크 트리 버튼이 보임

? **명령과 통합**
- 다른 명령들과 동일한 방식으로 접근

? **컨텍스트 유지**
- 명령 패널이 닫히지 않아 다른 작업 가능

? **확장 가능**
- 나중에 다른 UI 패널도 같은 방식으로 추가 가능

---

### ?? 사용 예시

```
[플레이어 시나리오]

1. 하이브 건설 완료
2. 하이브 클릭하여 선택
3. 명령 패널이 나타남
4. "테크 트리" 버튼 클릭
5. 테크 트리 UI 열림
6. 원하는 테크 연구
7. ESC 또는 X 버튼으로 닫기
8. 명령 패널은 여전히 활성 상태
9. 다른 명령 실행 가능
```

---

## ?? 완료!

이제 테크 트리를 명령 UI를 통해 자연스럽게 열 수 있습니다!
