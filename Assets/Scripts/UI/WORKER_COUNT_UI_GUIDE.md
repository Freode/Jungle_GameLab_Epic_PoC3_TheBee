# Worker Count UI 설정 가이드

플레이어 하이브의 현재/최대 일꾼 수를 화면에 표시하는 UI 시스템입니다.

## ?? 기능

- 현재 일꾼 수 / 최대 일꾼 수 형태로 표시
- 일꾼 수 비율에 따라 색상 자동 변경
- 자동으로 플레이어 하이브 감지
- 주기적으로 업데이트

## ?? Unity 설정

### 1. UI 생성

```
Hierarchy 우클릭 → UI → Text - TextMeshPro

위치:
├── Canvas
    └── WorkerCountUI (GameObject)
        └── WorkerCountText (TextMeshProUGUI)
```

### 2. WorkerCountUI 컴포넌트 설정

```
1. WorkerCountUI GameObject 선택

2. Add Component → Worker Count UI

3. Inspector 설정:
   ├── Worker Count Text: WorkerCountText 드래그
   └── Update Interval: 0.5 (0.5초마다 업데이트)
```

### 3. TextMeshProUGUI 설정

```
WorkerCountText 선택:

├── Text: "일꾼: 0/10" (초기값)
├── Font Size: 24
├── Alignment: Top-Left (또는 원하는 위치)
├── Color: White
├── Auto Size: ?
└── Rich Text: ? (색상 태그 사용)

RectTransform:
├── Anchor: Top-Left
├── Pos X: 10
├── Pos Y: -10
└── Size: 200 x 50
```

## ?? 색상 시스템

일꾼 수 비율에 따라 자동으로 색상이 변경됩니다:

| 비율 | 색상 | 의미 |
|------|------|------|
| 100% | ?? 노란색 (#FFFF00) | 최대치 도달 |
| 70~99% | ?? 초록색 (#00FF00) | 건강한 상태 |
| 40~69% | ? 흰색 | 보통 상태 |
| 0~39% | ?? 빨간색 (#FF0000) | 위험 상태 |

## ?? 코드 예시

### 외부에서 강제 업데이트

```csharp
// 일꾼이 생성되거나 죽었을 때 즉시 업데이트
WorkerCountUI.Instance.ForceUpdate();
```

### 플레이어 하이브 직접 설정

```csharp
// 특정 하이브로 설정
Hive myHive = GetComponent<Hive>();
WorkerCountUI.Instance.SetPlayerHive(myHive);
```

## ?? Hive.cs 연동 (선택사항)

일꾼이 생성되거나 죽을 때 즉시 UI를 업데이트하려면:

```csharp
// Hive.cs의 SpawnWorker() 메서드에 추가
void SpawnWorker()
{
    // ... 기존 코드 ...
    
    // UI 업데이트 ?
    if (WorkerCountUI.Instance != null)
    {
        WorkerCountUI.Instance.ForceUpdate();
    }
}
```

```csharp
// CombatUnit.cs의 TakeDamage() 메서드에 추가
public void TakeDamage(int damage)
{
    // ... 기존 코드 ...
    
    if (health <= 0)
    {
        // 죽을 때 UI 업데이트 ?
        if (WorkerCountUI.Instance != null)
        {
            WorkerCountUI.Instance.ForceUpdate();
        }
    }
}
```

## ?? 표시 예시

```
일꾼: 5/10      ← 흰색 (50%)
일꾼: 8/10      ← 초록색 (80%)
일꾼: 10/10     ← 노란색 (100%)
일꾼: 2/10      ← 빨간색 (20%)
```

## ?? 커스터마이징

### Update Interval 조정

```csharp
// Inspector에서 조정 또는 코드로:
[SerializeField] private float updateInterval = 0.5f; // 0.5초
```

- 0.5초: 부드러운 업데이트 (권장)
- 1.0초: 성능 최적화
- 0.1초: 실시간 (부하 높음)

### 텍스트 포맷 변경

`UpdateWorkerCountDisplay()` 메서드에서 수정:

```csharp
// 기본
workerCountText.text = $"일꾼: {currentWorkers}/{maxWorkers}";

// 영문
workerCountText.text = $"Workers: {currentWorkers}/{maxWorkers}";

// 아이콘 포함
workerCountText.text = $"?? {currentWorkers}/{maxWorkers}";
```

## ?? 문제 해결

### 1. "일꾼: -/-" 표시
- 플레이어 하이브를 찾을 수 없음
- TileManager.Instance가 null인지 확인
- Hive의 faction이 Player인지 확인

### 2. UI가 보이지 않음
- Canvas의 Render Mode 확인
- TextMeshProUGUI의 Color Alpha 확인
- WorkerCountText가 올바르게 연결되었는지 확인

### 3. 숫자가 업데이트 안 됨
- Update Interval 확인
- playerHive가 null인지 확인
- GetWorkers() 메서드가 제대로 작동하는지 확인

## ?? 참고

- 자동으로 플레이어 하이브를 찾습니다
- 하이브가 파괴되면 자동으로 다시 찾습니다
- 싱글톤 패턴으로 어디서든 접근 가능
- Rich Text 태그로 색상 표시
