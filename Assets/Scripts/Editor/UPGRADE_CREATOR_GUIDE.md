# ?? 업그레이드 명령 자동 생성 가이드

## 한 번의 클릭으로 7가지 업그레이드 생성!

Unity 에디터 메뉴를 통해 자동으로 7가지 업그레이드 명령을 생성할 수 있습니다.

---

## ? 빠른 시작

### 1단계: 업그레이드 명령 자동 생성

```
Unity 에디터 상단 메뉴
→ Tools
→ 업그레이드
→ 7가지 업그레이드 명령 생성
```

**완료!** ??

7개의 업그레이드 ScriptableObject가 자동으로 생성됩니다.

### 생성되는 파일 위치
```
Assets/
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

---

## ?? 생성되는 업그레이드 목록

| 파일명 | 표시 이름 | ID | 효과 | 비용 |
|--------|----------|-----|------|------|
| Upgrade_HiveRange | 활동 범위 확장 | upgrade_hive_range | 범위 +1 | 20 |
| Upgrade_WorkerAttack | 날카로운 침 | upgrade_worker_attack | 공격력 +1 | 15 |
| Upgrade_WorkerHealth | 강화 외골격 | upgrade_worker_health | 체력 +5 | 20 |
| Upgrade_WorkerSpeed | 빠른 날개 | upgrade_worker_speed | 속도 +1 | 15 |
| Upgrade_HiveHealth | 강화 벌집 | upgrade_hive_health | 하이브 체력 +30 | 25 |
| Upgrade_MaxWorkers | 확장 군집 | upgrade_max_workers | 최대 일꾼 +5 | 30 |
| Upgrade_GatherAmount | 효율적 채집 | upgrade_gather_amount | 채취량 +2 | 20 |

---

## ?? 2단계: 하이브에 추가하기

### 방법 1: 수동으로 추가

```
1. Project 창에서 하이브 프리팹 선택
2. Inspector에서 Hive (Script) 찾기
3. "Hive Commands" 배열 확장
4. 배열 크기 +7 증가
5. ScriptableObjects/Commands/Upgrades 폴더 열기
6. 7개 업그레이드를 Ctrl+A로 모두 선택
7. Hive Commands 배열의 빈 슬롯에 드래그 앤 드롭
```

### 방법 2: 스크립트로 추가 (고급)

하이브 프리팹에 업그레이드를 자동으로 추가하려면:

```csharp
// Hive.cs에서
void Start()
{
    if (hiveCommands == null || hiveCommands.Length == 0)
    {
        LoadUpgradeCommands();
    }
}

void LoadUpgradeCommands()
{
    #if UNITY_EDITOR
    var upgrades = new List<SOCommand>();
    
    // 기존 명령 유지
    if (hiveCommands != null)
        upgrades.AddRange(hiveCommands);
    
    // 업그레이드 폴더에서 자동 로드
    string[] guids = AssetDatabase.FindAssets("t:SOUpgradeCommand");
    foreach (string guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var upgrade = AssetDatabase.LoadAssetAtPath<SOUpgradeCommand>(path);
        if (upgrade != null && !upgrades.Contains(upgrade))
            upgrades.Add(upgrade);
    }
    
    hiveCommands = upgrades.ToArray();
    #endif
}
```

---

## ??? 추가 도구

### 업그레이드 폴더 열기
```
Tools → 업그레이드 → 업그레이드 폴더 열기
```
- 생성된 업그레이드 폴더를 바로 열어줍니다
- Project 창에서 폴더를 찾아다니지 않아도 됩니다

### 업그레이드 명령 삭제
```
Tools → 업그레이드 → 업그레이드 명령 삭제
```
- 모든 업그레이드 명령을 삭제합니다
- 재생성이 필요할 때 사용하세요

---

## ?? 커스터마이징

생성된 업그레이드를 수정하려면:

### 비용 변경
```
1. Project 창에서 업그레이드 에셋 선택
2. Inspector에서 Resource Cost 값 수정
```

### 이름 변경
```
1. Inspector에서 Display Name 수정
2. 게임 내 버튼에 표시되는 이름입니다
```

### 아이콘 추가
```
1. Inspector에서 Icon 필드 찾기
2. 원하는 스프라이트 드래그 앤 드롭
```

---

## ?? 아이콘 준비 (선택사항)

업그레이드에 아이콘을 추가하려면:

### 1. 아이콘 이미지 준비
```
권장 크기: 64x64 또는 128x128 픽셀
형식: PNG (투명 배경 권장)
```

### 2. Unity로 가져오기
```
1. Assets/Sprites/Icons/Upgrades/ 폴더에 이미지 복사
2. 각 이미지 선택
3. Inspector에서 Texture Type을 "Sprite (2D and UI)"로 변경
4. Apply 클릭
```

### 3. 업그레이드에 할당
```
1. 업그레이드 에셋 선택
2. Inspector에서 Icon 필드에 스프라이트 드래그
```

### 아이콘 예시
```
icon_range.png      → Upgrade_HiveRange
icon_attack.png     → Upgrade_WorkerAttack
icon_shield.png     → Upgrade_WorkerHealth
icon_speed.png      → Upgrade_WorkerSpeed
icon_fortress.png   → Upgrade_HiveHealth
icon_population.png → Upgrade_MaxWorkers
icon_harvest.png    → Upgrade_GatherAmount
```

---

## ?? 문제 해결

### "이미 존재합니다" 메시지가 나와요
```
? 덮어쓰기를 선택하면 기존 파일을 업데이트합니다
? 건너뛰기를 선택하면 기존 파일을 유지합니다
```

### 생성된 파일이 안 보여요
```
? Project 창에서 "ScriptableObjects" 폴더 확인
? Tools → 업그레이드 → 업그레이드 폴더 열기 메뉴 사용
? Project 창 상단의 검색창에 "Upgrade_" 입력
```

### Hive Commands 배열에 추가했는데 안 보여요
```
? 하이브 프리팹 저장 (Ctrl+S)
? 씬의 하이브도 프리팹에 연결되어 있는지 확인
? Play Mode에서 하이브 선택하여 테스트
```

---

## ?? 기본 설정값

자동 생성되는 업그레이드의 기본 설정:

```yaml
공통 설정:
  Requires Target: false
  Hide Panel On Click: false

개별 비용:
  하이브 활동 범위: 20
  일꾼 공격력: 15
  일꾼 체력: 20
  일꾼 이동 속도: 15
  하이브 체력: 25
  최대 일꾼 수: 30
  자원 채취량: 20
```

---

## ?? 테스트

생성 후 바로 테스트하기:

### 1. 게임 실행
```
Play 버튼 클릭
```

### 2. 자원 획득
```
일꾼으로 자원 타일 채취
또는
F1 키로 자원 치트 (HiveManager에 추가한 경우)
```

### 3. 업그레이드 확인
```
1. 하이브 클릭
2. 명령 패널에 7개 업그레이드 버튼 확인
3. 자원이 충분하면 흰색, 부족하면 회색
4. 버튼 클릭하여 업그레이드 실행
5. Console에서 "[업그레이드] ..." 메시지 확인
```

---

## ?? 팁

### 빠른 재생성
```
기존 업그레이드를 모두 삭제하고 재생성하려면:
1. Tools → 업그레이드 → 업그레이드 명령 삭제
2. Tools → 업그레이드 → 7가지 업그레이드 명령 생성
```

### 버전 관리 (Git)
```
생성된 .asset 파일들을 Git에 커밋하세요:
git add Assets/ScriptableObjects/Commands/Upgrades/
git commit -m "Add 7 upgrade commands"
```

### 팀 작업
```
팀원들은 프로젝트를 클론한 후:
1. Unity에서 프로젝트 열기
2. 자동으로 업그레이드 파일이 로드됨
3. 추가 작업 불필요!
```

---

## ?? 고급 사용법

### 에디터 스크립트 위치
```
Assets/Scripts/Editor/UpgradeCommandCreator.cs
```

### 스크립트 수정
비용이나 이름을 변경하려면 `UpgradeCommandCreator.cs`를 수정하세요:

```csharp
CreateUpgradeCommand(
    "Upgrade_WorkerAttack",
    "초강력 침",              // 이름 변경
    "upgrade_worker_attack",
    "일꾼의 공격력을 대폭 증가시킵니다.",  // 설명 변경
    UpgradeType.WorkerAttack,
    50                       // 비용 변경 (15 → 50)
);
```

### 새로운 업그레이드 추가
8번째, 9번째 업그레이드를 추가하려면:

```csharp
CreateUpgradeCommand(
    "Upgrade_QueenSpeed",
    "여왕의 날개",
    "upgrade_queen_speed",
    "여왕벌의 이동 속도를 증가시킵니다.",
    UpgradeType.QueenSpeed,  // enum에 추가 필요
    40
);
```

---

## ? 완료 체크리스트

생성 후 확인사항:

```
[ ] Tools 메뉴에서 "7가지 업그레이드 명령 생성" 클릭
[ ] Console에서 "7가지 업그레이드 명령이 생성되었습니다!" 확인
[ ] Project 창에서 ScriptableObjects/Commands/Upgrades 폴더 확인
[ ] 7개의 .asset 파일 확인
[ ] 하이브 프리팹의 Hive Commands 배열에 추가
[ ] 게임 실행하여 버튼 표시 확인
[ ] 업그레이드 실행 테스트
[ ] 효과 적용 확인
```

---

## ?? 완료!

이제 단 한 번의 클릭으로 7가지 업그레이드 명령을 생성할 수 있습니다!

**다음 단계:**
1. Tools → 업그레이드 → 7가지 업그레이드 명령 생성
2. 하이브에 업그레이드 추가
3. 게임 플레이!

즐거운 게임 개발 되세요! ???
