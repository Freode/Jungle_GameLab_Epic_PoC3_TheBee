# ?? 여왕벌 & 하이브 건설/이사 시스템 가이드

## 개요
여왕벌이 하이브를 건설하면 하이브 안으로 들어가고, 하이브 이사 시 다시 나타나는 시스템입니다.

---

## ? 주요 기능

### 1. 하이브 건설
```
여왕벌 선택 → 하이브 건설 명령
  ↓
여왕벌이 하이브 위치로 이동
  ↓
여왕벌이 하이브 안으로 들어감
  ↓
여왕벌 선택 불가능 (canMove = false)
  ↓
여왕벌 비주얼 숨김 (Renderer/Collider 비활성화)
```

### 2. 하이브 이사
```
하이브 선택 → 이사 명령
  ↓
20초 대기
  ↓
하이브 제거
  ↓
여왕벌 다시 활성화 (canMove = true)
  ↓
여왕벌 비주얼 표시 (Renderer/Collider 활성화)
  ↓
여왕벌 선택 가능
```

---

## ?? 게임 플레이

### 하이브 건설 과정

#### 1단계: 여왕벌 선택
```
1. 여왕벌 클릭
2. 명령 패널에 "하이브 건설" 버튼 표시
```

#### 2단계: 건설 실행
```
1. "하이브 건설" 버튼 클릭
2. 여왕벌이 자동으로 하이브 위치로 이동
3. 하이브 생성
4. 여왕벌이 하이브 안으로 들어감
```

#### 3단계: 결과 확인
```
? 하이브 생성됨
? 여왕벌이 보이지 않음 (하이브 안)
? 여왕벌 클릭 불가능
? 일꾼 생성 시작 (10초마다)
```

### 하이브 이사 과정

#### 1단계: 이사 시작
```
1. 하이브 클릭
2. "하이브 이사" 버튼 클릭
3. 20초 카운트다운 시작
```

#### 2단계: 대기
```
- 20초 동안 일꾼 생성 중지
- 하이브는 그대로 유지
```

#### 3단계: 하이브 제거
```
1. 20초 후 하이브 자동 제거
2. 여왕벌 다시 나타남
3. 일꾼들이 여왕벌 따라다님
```

#### 4단계: 새 위치 건설
```
1. 여왕벌 선택 가능
2. 원하는 위치로 이동
3. 다시 하이브 건설
```

---

## ?? 구현 세부사항

### ConstructHiveHandler.cs

#### 여왕벌을 하이브로 이동
```csharp
private static void MoveQueenToHive(UnitAgent queen, int q, int r)
{
    // 논리적 위치 업데이트
    queen.SetPosition(q, r);
    
    // 물리적 위치 업데이트
    Vector3 hivePos = TileHelper.HexToWorld(q, r, queen.hexSize);
    queen.transform.position = hivePos;
}
```

#### 여왕벌 숨김 처리
```csharp
// 이동 불가능하게
agent.canMove = false;

// 선택 해제
agent.SetSelected(false);

// 렌더러 비활성화
queenRenderer.enabled = false;
queenSprite.enabled = false;

// 콜라이더 비활성화 (클릭 방지)
queenCollider.enabled = false;
queenCollider3D.enabled = false;
```

### Hive.cs - DestroyHive()

#### 여왕벌 재활성화
```csharp
// 이동 가능하게
queenBee.canMove = true;

// 렌더러 활성화
queenRenderer.enabled = true;
queenSprite.enabled = true;

// 콜라이더 활성화 (클릭 가능)
queenCollider.enabled = true;
queenCollider3D.enabled = true;
```

### TileClickMover.cs

#### 하이브 안 여왕벌 선택 방지
```csharp
void SelectUnit(UnitAgent unit)
{
    // 하이브 안의 여왕벌은 선택 불가
    if (unit.isQueen && !unit.canMove)
    {
        Debug.Log("여왕벌은 하이브 안에 있어 선택할 수 없습니다.");
        return;
    }
    
    // 일반 선택 로직...
}
```

### DragSelector.cs

#### 드래그 선택에서 여왕벌 제외
```csharp
// 이미 구현되어 있음
if (unit.isQueen) continue; // 여왕벌 제외
```

---

## ?? 상태 다이어그램

```
[여왕벌 자유 상태]
  ↓ 하이브 건설
[여왕벌 하이브 안]
  - canMove = false
  - 비주얼 숨김
  - 선택 불가
  ↓ 하이브 이사
[하이브 제거 대기 (20초)]
  ↓
[여왕벌 자유 상태]
  - canMove = true
  - 비주얼 표시
  - 선택 가능
```

---

## ?? 여왕벌 상태 확인

### Inspector에서 확인
```
여왕벌 GameObject 선택
→ UnitAgent 컴포넌트
→ Can Move 필드 확인
  - true: 자유 상태 (선택 가능)
  - false: 하이브 안 (선택 불가)
```

### 코드로 확인
```csharp
if (queenBee.canMove)
{
    Debug.Log("여왕벌은 자유롭게 이동 가능");
}
else
{
    Debug.Log("여왕벌은 하이브 안에 있음");
}
```

---

## ?? 문제 해결

### 여왕벌이 안 보여요

**하이브 건설 후:**
```
? 정상입니다! 여왕벌은 하이브 안에 있습니다.
? 하이브를 이사시켜야 다시 나타납니다.
```

**하이브 이사 후에도 안 보이면:**
```
1. Console에서 에러 확인
2. 여왕벌 GameObject가 삭제되지 않았는지 확인
3. Renderer/SpriteRenderer가 활성화되었는지 확인
4. Collider가 활성화되었는지 확인
```

### 여왕벌을 클릭할 수 없어요

**하이브 안에 있을 때:**
```
? 정상입니다! canMove = false이므로 선택 불가입니다.
```

**하이브 이사 후에도 클릭 안 되면:**
```
1. canMove가 true인지 확인
2. Collider가 활성화되었는지 확인
3. 다른 오브젝트가 가리고 있는지 확인
```

### 하이브 건설 후 여왕벌이 그대로 보여요

**확인사항:**
```
1. ConstructHiveHandler.cs가 올바르게 적용되었는지
2. queenBee 참조가 제대로 설정되었는지
3. Renderer 컴포넌트가 존재하는지
```

---

## ?? 추가 기능 아이디어

### 1. 여왕벌 애니메이션

```csharp
// ConstructHiveHandler.cs
if (agent.isQueen)
{
    var animator = agent.GetComponent<Animator>();
    if (animator != null)
    {
        animator.SetTrigger("EnterHive");
    }
    
    // 애니메이션 끝나고 숨기기
    StartCoroutine(HideQueenAfterAnimation(agent, 1f));
}

IEnumerator HideQueenAfterAnimation(UnitAgent queen, float delay)
{
    yield return new WaitForSeconds(delay);
    
    // 숨김 처리...
}
```

### 2. 여왕벌 건강 상태 표시

```csharp
public class QueenHealthIndicator : MonoBehaviour
{
    public GameObject healthBarUI;
    
    void Update()
    {
        var queen = GetComponent<UnitAgent>();
        if (queen != null && queen.isQueen)
        {
            // 하이브 안에 있으면 UI 숨김
            healthBarUI.SetActive(queen.canMove);
        }
    }
}
```

### 3. 하이브 이사 시 시각적 효과

```csharp
// Hive.cs - DestroyHive()
if (queenBee != null)
{
    // 파티클 효과
    var particles = Instantiate(queenExitParticles, queenBee.transform.position, Quaternion.identity);
    Destroy(particles, 2f);
    
    // 사운드 효과
    AudioSource.PlayClipAtPoint(queenExitSound, queenBee.transform.position);
}
```

### 4. 여왕벌 주변 반경 표시

```csharp
// 여왕벌이 자유 상태일 때만 건설 가능 범위 표시
if (selectedQueen != null && selectedQueen.canMove)
{
    // 건설 가능한 타일 하이라이트
    HighlightConstructibleTiles(selectedQueen);
}
```

---

## ?? 체크리스트

### 하이브 건설 테스트
```
[ ] 여왕벌 선택 가능
[ ] "하이브 건설" 명령 표시
[ ] 건설 버튼 클릭
[ ] 여왕벌이 하이브 위치로 이동
[ ] 하이브 생성됨
[ ] 여왕벌이 사라짐
[ ] 여왕벌 클릭 불가
[ ] Console에 "[하이브 건설] ..." 메시지
```

### 하이브 이사 테스트
```
[ ] 하이브 선택
[ ] "하이브 이사" 명령 표시
[ ] 이사 버튼 클릭
[ ] 20초 대기
[ ] 하이브 제거됨
[ ] 여왕벌 다시 나타남
[ ] 여왕벌 선택 가능
[ ] Console에 "[하이브 이사] ..." 메시지
```

### 일꾼 동작 테스트
```
[ ] 하이브 건설 후 일꾼 생성
[ ] 하이브 이사 시 일꾼이 여왕벌 따라감
[ ] 새 하이브 건설 후 일꾼이 다시 일함
```

---

## ?? 완료!

이제 여왕벌이 하이브를 건설하면 자동으로 하이브 안으로 들어가고, 하이브 이사 시 다시 나타납니다!

**핵심 기능:**
- ? 하이브 건설 시 여왕벌 자동 이동
- ? 여왕벌 하이브 안으로 숨김 (선택 불가)
- ? 하이브 이사 시 여왕벌 재활성화
- ? 자연스러운 게임 플레이 흐름

게임 개발 화이팅! ?????
