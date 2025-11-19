# HoverInfoUI 설정 가이드

## ? 세 가지 기능 구현 완료!

### 1. 마우스 호버 정보 UI ?
### 2. 자원량에 따른 다단계 색상 ?
### 3. 말벌집 파괴 시 자원 타일 변환 ?

---

## ?? Unity 설정 방법

### 1. HoverInfoUI GameObject 생성

#### **Canvas 생성 (아직 없다면)**
```
Hierarchy:
└─ Canvas (UI)
   ├─ Render Mode: Screen Space - Overlay
   ├─ Canvas Scaler
   │  ├─ UI Scale Mode: Scale With Screen Size
   │  └─ Reference Resolution: 1920 x 1080
   └─ Graphic Raycaster (기본 포함)
```

#### **HoverInfoUI 오브젝트 생성**
```
Hierarchy:
└─ Canvas
   └─ HoverInfoUI (Empty GameObject)
      └─ Component: HoverInfoUI (Script)
```

---

### 2. InfoPanel UI 구조 생성

```
Canvas
└─ HoverInfoUI
   └─ InfoPanel (Panel)
      ├─ Image (배경)
      │  ├─ Color: 검은색 (0, 0, 0, 200)
      │  └─ Raycast Target: false
      ├─ TitleText (TextMeshProUGUI)
      │  ├─ Text: "유닛 정보"
      │  ├─ Font Size: 18
      │  ├─ Color: 흰색
      │  ├─ Alignment: Center
      │  └─ Auto Size: false
      └─ DetailsText (TextMeshProUGUI)
         ├─ Text: "상세 정보..."
         ├─ Font Size: 14
         ├─ Color: 흰색 (200)
         ├─ Alignment: Left
         └─ Auto Size: false
```

---

### 3. InfoPanel 레이아웃 설정

#### **InfoPanel (Panel)**
```
RectTransform:
├─ Width: 250
├─ Height: 150
├─ Pivot: (0, 1) ← 왼쪽 위
└─ Anchor: Top Left

Image:
├─ Color: (0, 0, 0, 200) ← 반투명 검은색
└─ Raycast Target: false ← 마우스 이벤트 차단 안 함
```

#### **TitleText (TextMeshProUGUI)**
```
RectTransform:
├─ Anchors: Stretch Top
├─ Left: 10, Right: -10
├─ Top: -10, Height: 30

Text:
├─ Font Size: 18
├─ Color: White
├─ Alignment: Center Middle
└─ Overflow: Ellipsis
```

#### **DetailsText (TextMeshProUGUI)**
```
RectTransform:
├─ Anchors: Stretch
├─ Left: 10, Right: -10
├─ Top: -50, Bottom: 10

Text:
├─ Font Size: 14
├─ Color: White (200, 200, 200)
├─ Alignment: Top Left
├─ Overflow: Overflow
└─ Wrapping: Enabled
```

---

### 4. HoverInfoUI 스크립트 설정

#### **Inspector 설정**
```
HoverInfoUI (Script)
├─ Info Panel: [InfoPanel GameObject 드래그]
├─ Title Text: [TitleText 드래그]
├─ Details Text: [DetailsText 드래그]
├─ Panel Rect: [InfoPanel RectTransform 드래그]
└─ Settings
   ├─ Offset: (10, 10)
   └─ Update Interval: 0.1
```

---

## ?? 자원 색상 설정 (HexTile)

### Inspector에서 색상 조정

```
HexTile (Script)
└─ 자원 색상 설정
   ├─ Resource Very High Color: (0.2, 0.8, 0.2) ← 진한 녹색
   ├─ Resource High Color: (0.4, 0.9, 0.4) ← 밝은 녹색
   ├─ Resource Medium Color: (0.8, 0.8, 0.2) ← 노란색
   ├─ Resource Low Color: (0.9, 0.6, 0.2) ← 주황색
   ├─ Resource Very Low Color: (0.9, 0.3, 0.2) ← 빨간색
   └─ Depleted Color: (0.5, 0.5, 0.5) ← 회색
```

### 자원량 색상 단계

| 자원량 | 색상 | 설명 |
|--------|------|------|
| 80%~100% | 진한 녹색 | 자원 매우 풍부 |
| 60%~80% | 밝은 녹색 | 자원 풍부 |
| 40%~60% | 노란색 | 자원 보통 |
| 20%~40% | 주황색 | 자원 부족 |
| 0%~20% | 빨간색 | 자원 매우 부족 |
| 0% | 회색 | 자원 고갈 |

---

## ?? 말벌집 파괴 보상

### 작동 방식

```
[말벌집 파괴]
   ↓
EnemyHive.DestroyHive() 호출
   ↓
타일 자원 지형으로 변환
   ↓
자원량 500 설정 ?
   ↓
타일 색상 업데이트 (진한 녹색)
```

### 확인 방법
1. 게임 실행
2. 말벌집 찾기
3. 말벌집 공격하여 파괴
4. 타일이 자원 타일로 변환되는지 확인
5. Console 로그: `[적 하이브] 타일을 자원 타일로 변환: (q, r), 자원량: 500`

---

## ?? 작동 확인

### HoverInfoUI 테스트

```
[유닛 호버]
마우스를 유닛 위에 올림
   ↓
InfoPanel 표시 ?
   ↓
Title: "일꾼 꿀벌" / "말벌" / "여왕벌"
Details:
- 위치: (q, r)
- 진영: 플레이어/적
- 체력: 현재/최대
- 공격력: X
- 현재 작업: 이동/채취/공격...

[타일 호버]
마우스를 타일 위에 올림
   ↓
InfoPanel 표시 ?
   ↓
Title: "타일 (q, r)"
Details:
- 지형: Forest/Plains...
- 자원량: X (있는 경우)
- 시야 상태: 미탐색/탐색됨/시야 내
- 적 하이브 위치 (있는 경우)
```

---

## ?? 색상 비교표

### 이전 vs 현재

| 상태 | 이전 | 현재 |
|------|------|------|
| 100% | 지형 색상 | 진한 녹색 ? |
| 80% | 지형 색상 | 진한 녹색 ? |
| 60% | 약간 어두움 | 밝은 녹색 ? |
| 40% | 회색 섞임 | 노란색 ? |
| 20% | 많이 어두움 | 주황색 ? |
| 10% | 거의 회색 | 빨간색 ? |
| 0% | 회색 | 회색 ? |

---

## ??? 추가 커스터마이징

### HoverInfoUI 스타일 변경

```csharp
// Assets\Scripts\UI\HoverInfoUI.cs

[Header("Settings")]
[SerializeField] private Vector2 offset = new Vector2(10f, 10f); // 패널 오프셋
[SerializeField] private float updateInterval = 0.1f; // 업데이트 주기
```

### 색상 그라데이션 조정

```csharp
// Assets\Scripts\Hex\HexTile.cs

// 자원 색상 변경
resourceVeryHighColor = new Color(0.0f, 1.0f, 0.0f); // 더 밝은 녹색
resourceHighColor = new Color(0.5f, 1.0f, 0.5f);
resourceMediumColor = new Color(1.0f, 1.0f, 0.0f); // 순수 노란색
resourceLowColor = new Color(1.0f, 0.5f, 0.0f);
resourceVeryLowColor = new Color(1.0f, 0.0f, 0.0f); // 순수 빨간색
```

---

## ? 최종 체크리스트

```
[HoverInfoUI]
[ ] Canvas에 GameObject 생성 ?
[ ] InfoPanel UI 구조 생성 ?
[ ] TitleText, DetailsText 설정 ?
[ ] HoverInfoUI 스크립트 연결 ?
[ ] 유닛 호버 시 정보 표시 ?
[ ] 타일 호버 시 정보 표시 ?

[자원 색상]
[ ] HexTile 색상 설정 추가 ?
[ ] 6단계 색상 그라데이션 ?
[ ] 자원량에 따라 색상 변화 ?

[말벌집 파괴]
[ ] 타일 자원 지형으로 변환 ?
[ ] 자원량 500 설정 ?
[ ] 타일 색상 업데이트 ?

[빌드]
[ ] 컴파일 성공 ?
```

---

## ?? 완료!

**구현된 기능:**
1. ? **HoverInfoUI**: 마우스 호버 시 유닛/타일 정보 표시
2. ? **자원 색상**: 6단계 그라데이션 (녹색→노란색→주황색→빨간색)
3. ? **말벌집 보상**: 파괴 시 자원 타일 변환 (자원량 500)

**주요 파일:**
- `Assets\Scripts\UI\HoverInfoUI.cs` ← 새로 생성
- `Assets\Scripts\Hex\HexTile.cs` ← 색상 시스템 개선
- `Assets\Scripts\Hive\EnemyHive.cs` ← 파괴 보상 추가

**Unity 설정:**
1. Canvas에 HoverInfoUI GameObject 생성
2. InfoPanel UI 구조 생성
3. 스크립트 연결 및 설정
4. 게임 실행 후 테스트

게임 개발 화이팅! ???????
