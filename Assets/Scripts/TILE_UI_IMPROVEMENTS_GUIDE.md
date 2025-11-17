# ?? 타일 내부 이동 & 업그레이드 UI 가이드

## 완료된 개선사항

### 1. ? 타일 경계 이탈 방지
- 육각형 타일 경계 내부만 이동
- 이동 중 실시간 경계 체크
- 안전한 위치로 자동 보정

### 2. ? 육각형 전체 범위 이동
- X축 뿐만 아니라 전방향 이동
- 극좌표 기반 랜덤 위치 생성
- 6개 섹터 균등 분포

### 3. ? 업그레이드 결과 UI
- 업그레이드 완료 시 알림 표시
- 효과 및 현재 값 표시
- Fade In/Out 애니메이션

---

## ?? 수정/생성된 파일

### 1. TileHelper.cs
```csharp
? GetRandomPositionInTile() 수정
   - 육각형 6개 섹터 기반 랜덤
   - 각도별 최대 거리 계산
   - 균등 분포 보장
   
? GetRandomPositionInCurrentTile() 수정
   - 3번 재시도 로직
   - 최소 거리 보장
   
? IsPositionInTile() 추가
   - 타일 경계 체크
   - 육각형 반지름 기준
```

### 2. UnitController.cs
```csharp
? MoveWithinCurrentTile() 수정
   - 마진 20%로 증가
   - 타일 경계 체크 추가
   
? MoveWithinTileCoroutine() 수정
   - 이동 중 경계 체크
   - 경계 초과 시 중단
   - 안전 위치로 보정
```

### 3. UpgradeResultUI.cs (신규)
```csharp
? ShowUpgradeResult() - 결과 표시
? DisplayUpgradeRoutine() - 애니메이션
? FadeCanvasGroup() - Fade 효과
```

### 4. HiveManager.cs
```csharp
? 모든 업그레이드 메서드 수정
   - UpgradeResultUI 연동
   - 업그레이드 이름, 효과, 현재 값 표시
```

---

## ?? 시스템 작동 방식

### 1. 타일 내부 이동 (육각형 전체)

#### 이전 (X축만)
```
      타일
┌─────────┐
│  →  →   │ X축으로만 이동
│         │
└─────────┘
```

#### 현재 (전방향)
```
      ?
    ? ? ?
  ? ? ? ? ?  육각형 전체
    ? ? ?
      ?
      
6개 섹터 균등 분포 ?
```

### 2. 경계 체크

```
[이동 시작]
목표: (x, z)
   ↓
[경계 체크]
IsPositionInTile()?
   ├─ Yes → 이동 허용 ?
   └─ No  → 중단 및 보정
             ↓
        타일 중심으로 이동
```

### 3. 업그레이드 UI 표시

```
[업그레이드 실행]
   ↓
HiveManager.UpgradeXXX()
   ↓
업그레이드 성공
   ↓
UpgradeResultUI.ShowUpgradeResult()
   ↓
[Fade In 0.3초]
┌─────────────────────┐
│ ? 날카로운 침        │
│ 일꾼 공격력 +1      │
│ 현재: 4             │
└─────────────────────┘
   ↓
[표시 3초]
   ↓
[Fade Out 0.3초]
   ↓
숨김
```

---

## ?? Unity 설정 (필수!)

### 1. 업그레이드 결과 UI 생성

#### Canvas 구조
```
Canvas (Screen Space - Overlay)
└─ UpgradeResultPanel
   ├─ Background (Image)
   ├─ UpgradeNameText (TextMeshProUGUI)
   ├─ UpgradeEffectText (TextMeshProUGUI)
   └─ CurrentValueText (TextMeshProUGUI)
```

#### 단계별 생성

**1단계: Canvas 생성**
```
Hierarchy 우클릭
→ UI → Canvas

Canvas 설정:
- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
- Reference Resolution: 1920x1080
```

**2단계: UpgradeResultPanel 생성**
```
Canvas 우클릭
→ UI → Panel

이름: UpgradeResultPanel

RectTransform:
- Anchor: Middle Center
- Pos X: 0, Pos Y: 200 (화면 위쪽)
- Width: 400, Height: 150

Image:
- Color: (0, 0, 0, 180) - 반투명 검정
```

**3단계: CanvasGroup 추가**
```
UpgradeResultPanel 선택
→ Add Component
→ Canvas Group

설정:
- Alpha: 0 (시작 시 투명)
- Interactable: false
- Block Raycasts: false
```

**4단계: UpgradeNameText 생성**
```
UpgradeResultPanel 우클릭
→ UI → Text - TextMeshPro

이름: UpgradeNameText

RectTransform:
- Anchor: Top Center
- Pos X: 0, Pos Y: -30
- Width: 360, Height: 40

TextMeshPro:
- Text: "업그레이드 이름"
- Font Size: 28
- Color: White
- Alignment: Center
- Font Style: Bold
```

**5단계: UpgradeEffectText 생성**
```
UpgradeResultPanel 우클릭
→ UI → Text - TextMeshPro

이름: UpgradeEffectText

RectTransform:
- Anchor: Middle Center
- Pos X: 0, Pos Y: 0
- Width: 360, Height: 30

TextMeshPro:
- Text: "효과 설명"
- Font Size: 20
- Color: (200, 255, 200) - 연한 초록
- Alignment: Center
```

**6단계: CurrentValueText 생성**
```
UpgradeResultPanel 우클릭
→ UI → Text - TextMeshPro

이름: CurrentValueText

RectTransform:
- Anchor: Bottom Center
- Pos X: 0, Pos Y: 30
- Width: 360, Height: 30

TextMeshPro:
- Text: "현재: 10"
- Font Size: 18
- Color: (255, 255, 150) - 연한 노랑
- Alignment: Center
```

**7단계: UpgradeResultUI 컴포넌트 추가**
```
Canvas 선택
→ Add Component
→ UpgradeResultUI

Inspector 설정:
- Result Panel: UpgradeResultPanel 드래그
- Upgrade Name Text: UpgradeNameText 드래그
- Upgrade Effect Text: UpgradeEffectText 드래그
- Current Value Text: CurrentValueText 드래그
- Display Duration: 3
- Fade In Duration: 0.3
- Fade Out Duration: 0.3
```

### 2. 시각적 개선 (선택)

#### 배경 이미지 추가
```
UpgradeResultPanel → Image
→ Sprite: 업그레이드 배경 이미지
→ Image Type: Sliced (9-slice)
```

#### 아이콘 추가
```
UpgradeResultPanel 우클릭
→ UI → Image

이름: UpgradeIcon

RectTransform:
- Anchor: Left Center
- Pos X: 30, Pos Y: 0
- Width: 50, Height: 50
```

#### 애니메이션 추가
```
UpgradeResultPanel 선택
→ Add Component
→ Animator

Animation:
- Bounce In
- Shake
- Scale Pulse
```

---

## ?? 코드 구조

### TileHelper.cs - 육각형 랜덤 위치

```csharp
public static Vector3 GetRandomPositionInTile(int q, int r, float hexSize, float margin)
{
    Vector3 center = HexToWorld(q, r, hexSize);
    float maxRadius = hexSize * (1f - margin);
    
    // 6개 섹터 중 하나 선택
    int sector = Random.Range(0, 6);
    float sectorAngle = 60f * Mathf.Deg2Rad;
    float angle = (sector * sectorAngle) + Random.Range(-sectorAngle * 0.5f, sectorAngle * 0.5f);
    
    // 각도별 최대 거리 계산
    float angleOffset = Mathf.Abs(Mathf.Repeat(angle, sectorAngle) - sectorAngle * 0.5f);
    float maxDistAtAngle = maxRadius / Mathf.Cos(angleOffset);
    float distance = Random.Range(0f, Mathf.Min(Random.Range(0f, maxRadius), maxDistAtAngle));
    
    // 극좌표 → 직교좌표
    float randomX = Mathf.Cos(angle) * distance;
    float randomZ = Mathf.Sin(angle) * distance;
    
    return new Vector3(center.x + randomX, center.y, center.z + randomZ);
}
```

### UnitController.cs - 경계 체크

```csharp
IEnumerator MoveWithinTileCoroutine(Vector3 targetPos)
{
    isMoving = true;
    Vector3 startPos = transform.position;
    // ...이동 로직
    
    while (elapsed < travelTime)
    {
        Vector3 lerpedPos = Vector3.Lerp(startPos, targetPos, t);
        
        // 경계 체크
        if (TileHelper.IsPositionInTile(lerpedPos, agent.q, agent.r, agent.hexSize))
        {
            transform.position = lerpedPos; // 허용
        }
        else
        {
            break; // 경계 밖 → 중단
        }
        
        yield return null;
    }
    
    // 최종 위치 안전 확인
    if (!TileHelper.IsPositionInTile(targetPos, agent.q, agent.r, agent.hexSize))
    {
        transform.position = TileHelper.HexToWorld(agent.q, agent.r, agent.hexSize);
    }
    
    isMoving = false;
}
```

### UpgradeResultUI.cs - 결과 표시

```csharp
public void ShowUpgradeResult(string upgradeName, string effect, string currentValue)
{
    StartCoroutine(DisplayUpgradeRoutine(upgradeName, effect, currentValue));
}

IEnumerator DisplayUpgradeRoutine(string upgradeName, string effect, string currentValue)
{
    // 텍스트 설정
    upgradeNameText.text = $"<color=#FFD700>?</color> {upgradeName}";
    upgradeEffectText.text = effect;
    currentValueText.text = $"현재: {currentValue}";
    
    // Fade In
    yield return FadeCanvasGroup(0f, 1f, fadeInDuration);
    
    // 표시 유지
    yield return new WaitForSeconds(displayDuration);
    
    // Fade Out
    yield return FadeCanvasGroup(1f, 0f, fadeOutDuration);
    
    Hide();
}
```

---

## ?? 시각화

### 타일 내부 이동 (육각형)

```
[이전: X축만]
     타일
  ┌───────┐
  │ → → → │
  │       │
  └───────┘

[현재: 전방향]
      ?
    ? ? ?
  ? ? ? ? ?
    ? ? ?
      ?
      
6개 섹터 균등 분포
각도별 최대 거리 계산
육각형 경계 내부만 이동 ?
```

### 업그레이드 UI

```
┌─────────────────────────┐
│                         │
│   ? 날카로운 침          │
│                         │
│   일꾼 공격력 +1        │
│                         │
│   현재: 4               │
│                         │
└─────────────────────────┘

[Fade In 0.3초]
[표시 3초]
[Fade Out 0.3초]
```

---

## ?? 설정 조절

### 타일 마진

```csharp
// UnitController.cs - MoveWithinCurrentTile()

// 안전하게 (큰 마진)
margin = 0.3f; // 30%

// 표준 (권장)
margin = 0.2f; // 20% ?

// 경계까지 (작은 마진)
margin = 0.1f; // 10%
```

### UI 표시 시간

```csharp
// UpgradeResultUI.cs

// 빠르게
displayDuration = 2f;
fadeInDuration = 0.2f;
fadeOutDuration = 0.2f;

// 표준 (권장)
displayDuration = 3f; ?
fadeInDuration = 0.3f; ?
fadeOutDuration = 0.3f; ?

// 느리게
displayDuration = 5f;
fadeInDuration = 0.5f;
fadeOutDuration = 0.5f;
```

---

## ?? 비교표

### 타일 내부 이동

| 요소 | 이전 | 현재 |
|------|------|------|
| **이동 범위** | X축만 | 전방향 ? |
| **분포** | 불균등 | 균등 ? |
| **경계 체크** | 없음 | 실시간 ? |
| **타일 이탈** | 발생 ? | 방지 ? |

### UI 표시

| 요소 | 이전 | 현재 |
|------|------|------|
| **업그레이드 알림** | Console만 | UI 표시 ? |
| **효과 표시** | 없음 | 있음 ? |
| **현재 값** | 없음 | 표시 ? |
| **애니메이션** | 없음 | Fade ? |

---

## ?? 추가 개선 아이디어

### 1. 사운드 추가
```csharp
// UpgradeResultUI.cs
public AudioClip upgradeSound;

IEnumerator DisplayUpgradeRoutine(...)
{
    // 사운드 재생
    if (upgradeSound != null)
    {
        AudioSource.PlayClipAtPoint(upgradeSound, Camera.main.transform.position);
    }
    
    // ...나머지 코드
}
```

### 2. 파티클 효과
```csharp
public ParticleSystem upgradeParticle;

void ShowUpgradeResult(...)
{
    if (upgradeParticle != null)
    {
        upgradeParticle.Play();
    }
    
    // ...나머지 코드
}
```

### 3. 아이콘 표시
```csharp
public Image upgradeIcon;

public void ShowUpgradeResult(string upgradeName, string effect, string currentValue, Sprite icon)
{
    if (upgradeIcon != null)
    {
        upgradeIcon.sprite = icon;
    }
    
    // ...나머지 코드
}
```

---

## ?? 문제 해결

### Q: 유닛이 여전히 타일 밖으로 나가요

**확인:**
```
[ ] TileHelper.IsPositionInTile() 호출 확인
[ ] margin 값 확인 (0.2 권장)
[ ] hexSize 값 정확한지 확인
[ ] 타일 크기와 유닛 크기 비율
```

**해결:**
```csharp
// margin 증가
margin = 0.3f; // 30%로 증가

// 또는 안전 위치 강제
transform.position = TileHelper.HexToWorld(q, r, hexSize);
```

### Q: 업그레이드 UI가 안 보여요

**확인:**
```
[ ] Canvas 존재
[ ] UpgradeResultUI 컴포넌트 추가
[ ] Result Panel 연결
[ ] 텍스트 컴포넌트 연결
[ ] Canvas 활성화 상태
```

**해결:**
```
1. Hierarchy에서 Canvas 확인
2. UpgradeResultUI.Instance != null 확인
3. resultPanel.SetActive(true) 호출 확인
```

### Q: UI가 계속 보여요

**확인:**
```
[ ] Hide() 호출 확인
[ ] canvasGroup.alpha = 0 확인
[ ] resultPanel.SetActive(false) 확인
```

---

## ? 테스트 체크리스트

### 타일 이동
```
[ ] 유닛이 육각형 전체에서 이동
[ ] X, Z 양방향 이동 확인
[ ] 타일 경계 내부만 이동
[ ] 경계 초과 시 자동 보정
```

### 업그레이드 UI
```
[ ] Canvas 생성 완료
[ ] UpgradeResultPanel 생성
[ ] 3개 텍스트 생성 및 연결
[ ] UpgradeResultUI 컴포넌트 추가
[ ] 업그레이드 실행 시 UI 표시
[ ] Fade In/Out 애니메이션 작동
[ ] 3초 후 자동 숨김
```

---

## ?? 완료!

**핵심 기능:**
- ? 타일 경계 이탈 방지
- ? 육각형 전체 범위 이동
- ? 6개 섹터 균등 분포
- ? 업그레이드 결과 UI
- ? Fade 애니메이션

**Unity 설정 필수:**
1. Canvas 생성
2. UpgradeResultPanel + 3개 텍스트
3. CanvasGroup 추가
4. UpgradeResultUI 컴포넌트

**게임 플레이:**
- 자연스러운 타일 내 이동
- 경계 이탈 없음
- 업그레이드 결과 시각화
- 직관적인 피드백

게임 개발 화이팅! ??????
