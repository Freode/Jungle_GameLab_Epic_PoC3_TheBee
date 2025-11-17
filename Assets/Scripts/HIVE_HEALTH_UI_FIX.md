# ?? 하이브 건설 & 이사 준비 UI 개선 가이드

## 완료된 개선사항

### 1. ? 새 하이브 건설 시 최대 체력으로 설정
- Initialize()에서 체력 설정 순서 변경
- combat.health = combat.maxHealth
- 즉시 전투 가능한 상태

### 2. ? 이사 준비 카운트다운 UI 추가
- relocateTimerText UI 필드 추가
- 실시간 카운트다운 표시
- "이사 준비 중... N초" 문구

---

## ?? 수정된 파일

### Hive.cs
```csharp
? [Header("UI")] 추가
   - relocateTimerText (TextMeshProUGUI)
   
? OnEnable() 수정
   - UI 텍스트 초기화
   
? Initialize() 수정
   - 체력 설정을 맨 앞으로 이동
   - combat.health = combat.maxHealth
   
? RelocationCountdown() 수정
   - UI 텍스트 활성화/비활성화
   - 실시간 카운트다운 표시
```

---

## ?? 시스템 작동 방식

### 1. 새 하이브 건설 시 최대 체력

#### 문제 상황 (이전)
```
[하이브 건설]
Initialize(q, r)
   ↓
[순서]
1. maxWorkers 설정
2. 기존 일꾼 추가
3. SpawnLoop 시작
4. 경계선 표시
5. 체력 설정 ? (맨 끝)
   ↓
[문제]
combat.health = 0 (기본값)
combat.maxHealth = 200
→ 체력 0/200 ?
```

#### 해결 (현재)
```
[하이브 건설]
Initialize(q, r)
   ↓
[순서]
1. 체력 설정 ? (맨 앞)
   combat.maxHealth = GetHiveMaxHealth()
   combat.health = combat.maxHealth ?
2. maxWorkers 설정
3. 기존 일꾼 추가
4. SpawnLoop 시작
5. 경계선 표시
   ↓
[결과]
combat.health = 200
combat.maxHealth = 200
→ 체력 200/200 ?
```

---

### 2. 이사 준비 카운트다운 UI

#### UI 표시 흐름
```
[이사 버튼 클릭]
StartRelocation(25)
   ↓
[카운트다운 시작]
RelocationCountdown()
   ↓
[UI 활성화] ?
relocateTimerText.gameObject.SetActive(true)
   ↓
[매 프레임 업데이트]
countdown -= Time.deltaTime
   ↓
[UI 텍스트 업데이트] ?
int remainingSeconds = Ceil(countdown)
relocateTimerText.text = "이사 준비 중...\n{remainingSeconds}초"
   ↓
[카운트다운 완료]
   ↓
[UI 비활성화] ?
relocateTimerText.gameObject.SetActive(false)
   ↓
[하이브 파괴]
DestroyHive()
```

---

## ?? 핵심 코드

### 1. 새 하이브 최대 체력 설정

```csharp
// Hive.cs - Initialize()
public void Initialize(int q, int r)
{
    this.q = q; 
    this.r = r;
    
    // Apply upgrades: hive health (먼저 설정) ?
    var combat = GetComponent<CombatUnit>();
    if (combat != null && HiveManager.Instance != null)
    {
        combat.maxHealth = HiveManager.Instance.GetHiveMaxHealth();
        combat.health = combat.maxHealth; // 최대 체력으로 설정 ?
        
        if (showDebugLogs)
            Debug.Log($"[하이브 초기화] 체력 설정: {combat.health}/{combat.maxHealth}");
    }
    
    // Apply upgrades: max workers
    if (HiveManager.Instance != null)
    {
        maxWorkers = HiveManager.Instance.GetMaxWorkers();
    }
    
    // ...나머지 코드...
}
```

---

### 2. 이사 준비 UI 텍스트

```csharp
// Hive.cs - 필드 선언
[Header("UI")]
public TMPro.TextMeshProUGUI relocateTimerText; // 이사 준비 타이머 텍스트 ?
```

```csharp
// Hive.cs - OnEnable()
void OnEnable()
{
    // ...기존 코드...
    
    // UI 텍스트 초기화 ?
    if (relocateTimerText != null)
    {
        relocateTimerText.gameObject.SetActive(false);
    }
    
    // ...나머지 코드...
}
```

```csharp
// Hive.cs - RelocationCountdown()
IEnumerator RelocationCountdown()
{
    float countdown = 10f;
    var combat = GetComponent<CombatUnit>();
    int initialHealth = combat != null ? combat.health : 0;
    float healthDrainRate = initialHealth / countdown;
    
    Debug.Log($"[하이브 이사] 카운트다운 시작: {countdown}초");
    
    // UI 텍스트 활성화 ?
    if (relocateTimerText != null)
    {
        relocateTimerText.gameObject.SetActive(true);
    }

    while (countdown > 0f)
    {
        countdown -= Time.deltaTime;
        
        // 체력 점진 감소
        if (combat != null)
        {
            float drainAmount = healthDrainRate * Time.deltaTime;
            combat.health = Mathf.Max(1, combat.health - Mathf.CeilToInt(drainAmount));
        }
        
        // UI 텍스트 업데이트 ?
        if (relocateTimerText != null)
        {
            int remainingSeconds = Mathf.CeilToInt(countdown);
            relocateTimerText.text = $"이사 준비 중...\n{remainingSeconds}초";
        }
        
        // 1초마다 로그 출력
        if (showDebugLogs && Mathf.FloorToInt(countdown) != Mathf.FloorToInt(countdown + Time.deltaTime))
        {
            Debug.Log($"[하이브 이사] 남은 시간: {Mathf.FloorToInt(countdown)}초");
        }

        yield return null;
    }

    Debug.Log("[하이브 이사] 카운트다운 완료! 하이브 파괴 시작");
    
    // UI 텍스트 비활성화 ?
    if (relocateTimerText != null)
    {
        relocateTimerText.gameObject.SetActive(false);
    }
    
    // 하이브 파괴
    DestroyHive();
}
```

---

## ?? Unity 설정

### 1. 하이브 Prefab 설정

```
Hive Prefab
├─ [Components]
│  ├─ UnitAgent
│  ├─ CombatUnit
│  │  ├─ Max Health: 200
│  │  ├─ Health: 200 (자동 설정됨)
│  │  └─ Attack: 0
│  └─ Hive (Script)
│     ├─ relocateTimerText: [UI 텍스트 할당] ?
│     └─ ...
└─ [Children]
   └─ RelocateTimerText (TextMeshProUGUI) ?
      ├─ Text: ""
      ├─ Font Size: 24
      ├─ Color: Red
      ├─ Alignment: Center
      └─ Active: false (초기 상태)
```

---

### 2. UI 텍스트 생성

#### 방법 1: Hive Prefab에 직접 추가
```
1. Hive Prefab 열기
2. 자식 GameObject 생성: "RelocateTimerText"
3. TextMeshProUGUI 컴포넌트 추가
4. 설정:
   - Text: "" (비어있음)
   - Font Size: 24
   - Color: Red
   - Alignment: Center, Middle
   - Active: false
5. Hive 컴포넌트의 relocateTimerText에 할당
```

#### 방법 2: Canvas에 추가
```
1. Canvas 아래 "HiveRelocateUI" 생성
2. Panel 추가 (배경)
3. TextMeshProUGUI 추가
4. 설정:
   - Rect Transform: Center, Middle
   - Width: 200, Height: 80
   - Text: ""
   - Font Size: 24
   - Color: Red
   - Alignment: Center
5. Hive 컴포넌트의 relocateTimerText에 할당
```

---

## ?? 비교표

### 1. 하이브 체력

| 시점 | 이전 | 현재 |
|------|------|------|
| **건설 직후** | 0/200 | 200/200 ? |
| **업그레이드 적용** | 안 됨 | 즉시 적용 ? |
| **전투 가능** | 불가 | 가능 ? |

### 2. 이사 준비 UI

| 항목 | 이전 | 현재 |
|------|------|------|
| **UI 표시** | 없음 | 있음 ? |
| **카운트다운** | 로그만 | UI 표시 ? |
| **시각적 피드백** | 부족 | 명확 ? |

---

## ?? 시각화

### 하이브 체력 설정

```
[이전]
Initialize()
→ combat.health = 0 ?
→ HP: 0/200

[현재]
Initialize()
→ combat.health = maxHealth ?
→ HP: 200/200 ?
```

---

### 이사 준비 UI

```
[이사 버튼 클릭]
   ↓
┌────────────────┐
│ 이사 준비 중... │ ?
│     10초       │
└────────────────┘
   ↓
┌────────────────┐
│ 이사 준비 중... │
│      9초       │
└────────────────┘
   ↓
┌────────────────┐
│ 이사 준비 중... │
│      1초       │
└────────────────┘
   ↓
[UI 비활성화]
   ↓
[하이브 파괴]
```

---

## ?? 문제 해결

### Q: 새 하이브 체력이 0이에요

**확인:**
```
[ ] Initialize() 실행 확인
[ ] combat.maxHealth 설정 확인
[ ] combat.health = combat.maxHealth 실행 확인
[ ] HiveManager.GetHiveMaxHealth() 값 확인
```

**해결:**
```
1. Console에서 "[하이브 초기화] 체력 설정: X/X" 로그 확인
2. HiveManager.Instance != null 확인
3. CombatUnit 컴포넌트 존재 확인
```

---

### Q: 이사 준비 UI가 안 나와요

**확인:**
```
[ ] relocateTimerText 필드 할당 확인
[ ] UI GameObject 존재 확인
[ ] SetActive(true) 호출 확인
[ ] Canvas 렌더링 확인
```

**해결:**
```
1. Inspector에서 relocateTimerText 할당 확인
2. Prefab에 UI GameObject 추가
3. Canvas가 씬에 존재하는지 확인
4. UI가 카메라 뷰에 보이는지 확인
```

---

### Q: UI 텍스트가 업데이트 안 돼요

**확인:**
```
[ ] while 루프 실행 확인
[ ] relocateTimerText != null 확인
[ ] text 할당 코드 실행 확인
```

**해결:**
```
1. Debug.Log로 countdown 값 출력
2. UI 텍스트 컴포넌트 확인
3. yield return null 존재 확인
```

---

## ?? 추가 개선 아이디어

### 1. 체력바 UI

```csharp
// 하이브 위에 체력바 표시
public class HiveHealthBar : MonoBehaviour
{
    public Image healthBarFill;
    private CombatUnit combat;
    
    void Update()
    {
        if (combat != null)
        {
            float fillAmount = (float)combat.health / combat.maxHealth;
            healthBarFill.fillAmount = fillAmount;
        }
    }
}
```

---

### 2. 이사 준비 진행바

```csharp
// RelocationCountdown() 수정
public Image progressBar; // Inspector에서 할당

while (countdown > 0f)
{
    countdown -= Time.deltaTime;
    
    // 진행바 업데이트 ?
    if (progressBar != null)
    {
        progressBar.fillAmount = countdown / 10f;
    }
    
    // ...
}
```

---

### 3. 색상 변경

```csharp
// 남은 시간에 따라 색상 변경
if (relocateTimerText != null)
{
    int remainingSeconds = Mathf.CeilToInt(countdown);
    
    // 색상 변경 ?
    if (remainingSeconds <= 3)
        relocateTimerText.color = Color.red;
    else if (remainingSeconds <= 5)
        relocateTimerText.color = Color.yellow;
    else
        relocateTimerText.color = Color.white;
    
    relocateTimerText.text = $"이사 준비 중...\n{remainingSeconds}초";
}
```

---

### 4. 사운드 효과

```csharp
// 카운트다운 시작 사운드
if (relocateTimerText != null)
{
    relocateTimerText.gameObject.SetActive(true);
    
    // 사운드 재생 ?
    AudioSource.PlayClipAtPoint(relocateStartSound, transform.position);
}
```

---

## ? 테스트 체크리스트

```
[ ] 새 하이브 건설 → 체력 200/200
[ ] 업그레이드 후 건설 → 업그레이드 적용됨
[ ] 하이브 이사 버튼 클릭
[ ] UI 텍스트 표시 확인
[ ] "이사 준비 중... 10초" 확인
[ ] 카운트다운 실시간 업데이트
[ ] 10초 후 UI 비활성화
[ ] 하이브 파괴 확인
[ ] 여왕벌 활성화 확인
```

---

## ?? 완료!

**핵심 수정:**
- ? 새 하이브 최대 체력 설정
- ? 이사 준비 카운트다운 UI
- ? 실시간 텍스트 업데이트

**결과:**
- 하이브 건설 시 즉시 전투 가능
- 명확한 이사 준비 피드백
- 플레이어 경험 개선

**게임 플레이:**
- 하이브 건설 → 즉시 방어 가능
- 이사 준비 → 시각적 피드백
- 카운트다운 → 전략적 선택

게임 개발 화이팅! ??????
