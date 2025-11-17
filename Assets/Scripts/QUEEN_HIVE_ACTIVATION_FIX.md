# ?? 하이브 건설/파괴 시 여왕벌 활성화/비활성화 가이드

## 완료된 개선사항

### ? 하이브 건설 시 여왕벌 비활성화
- 여왕벌을 하이브 위치로 이동
- canMove = false 설정
- 렌더러/콜라이더 비활성화
- 선택 해제

### ? 하이브 파괴 시 여왕벌 재활성화
- 여왕벌을 하이브 위치에 배치
- canMove = true 설정
- 렌더러/콜라이더 활성화
- 일꾼들이 여왕벌 추종

---

## ?? 수정된 파일

### Hive.cs
```csharp
? Initialize() 수정
   - 여왕벌 하이브 위치로 이동
   - queenBee.canMove = false
   - Renderer/Collider 비활성화
   - SetSelected(false)
   
? DestroyHive() 수정
   - 여왕벌 하이브 위치에 배치
   - queenBee.canMove = true
   - Renderer/Collider 활성화
   - 일꾼들 추종 모드
```

---

## ?? 시스템 작동 방식

### 1. 하이브 건설 시 여왕벌 비활성화

```
[여왕벌 선택]
?? (활성화)
   ↓
[하이브 건설 명령]
"하이브 건설" 버튼 클릭
   ↓
[하이브 생성]
Initialize(q, r)
   ↓
[여왕벌 비활성화] ?
1. 여왕벌을 하이브 위치로 이동
   queenBee.SetPosition(q, r)
   queenBee.transform.position = hivePos
   
2. 이동 불가
   queenBee.canMove = false
   
3. 렌더러 비활성화
   queenRenderer.enabled = false
   queenSprite.enabled = false
   
4. 콜라이더 비활성화 (클릭 불가)
   queenCollider.enabled = false
   queenCollider3D.enabled = false
   
5. 선택 해제
   queenBee.SetSelected(false)
   ↓
[결과]
?? (여왕벌이 하이브 안에 숨음) ?
- 여왕벌 보이지 않음
- 클릭 불가
- 이동 불가
```

---

### 2. 하이브 파괴 시 여왕벌 재활성화

#### 시나리오 A: 이사로 인한 파괴

```
[하이브 이사]
"하이브 이사" 버튼 클릭
   ↓
[10초 카운트다운]
RelocationCountdown()
   ↓
[하이브 파괴]
DestroyHive()
   ↓
[여왕벌 재활성화] ?
1. 여왕벌을 하이브 위치에 배치
   queenBee.SetPosition(q, r)
   queenBee.transform.position = hivePos
   
2. 이동 가능
   queenBee.canMove = true
   
3. 렌더러 활성화
   queenRenderer.enabled = true
   queenSprite.enabled = true
   
4. 콜라이더 활성화 (클릭 가능)
   queenCollider.enabled = true
   queenCollider3D.enabled = true
   ↓
[일꾼들 추종]
worker.isFollowingQueen = true
   ↓
[결과]
?? (하이브 위치에서 재등장) ?
- 여왕벌 보임
- 클릭 가능
- 이동 가능
- 일꾼들이 따라다님
```

#### 시나리오 B: 적의 공격으로 파괴

```
[적 공격]
말벌이 하이브 공격
   ↓
[하이브 체력 0]
combat.health <= 0
   ↓
[DestroyHive() 호출]
CombatUnit.Die()
   ↓
[여왕벌 재활성화] ?
(위와 동일한 프로세스)
   ↓
[결과]
?? (하이브 자리에서 등장) ?
- 일꾼들이 여왕벌 주변 집결
- 새로운 위치로 이동 가능
- 다시 하이브 건설 가능
```

---

## ?? 핵심 코드

### 1. Initialize() - 여왕벌 비활성화

```csharp
// Hive.cs
public void Initialize(int q, int r)
{
    this.q = q; 
    this.r = r;
    
    // 여왕벌 비활성화 (하이브 안으로 들어감) ?
    if (queenBee != null)
    {
        // 여왕벌을 하이브 위치로 이동 ?
        queenBee.SetPosition(q, r);
        Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
        queenBee.transform.position = hivePos;
        
        // 이동 불가능하게 ?
        queenBee.canMove = false;
        
        // 렌더러 비활성화 ?
        var queenRenderer = queenBee.GetComponent<Renderer>();
        if (queenRenderer != null)
            queenRenderer.enabled = false;
        
        var queenSprite = queenBee.GetComponent<SpriteRenderer>();
        if (queenSprite != null)
            queenSprite.enabled = false;
        
        // 콜라이더 비활성화 (클릭 불가) ?
        var queenCollider = queenBee.GetComponent<Collider2D>();
        if (queenCollider != null)
            queenCollider.enabled = false;
        
        var queenCollider3D = queenBee.GetComponent<Collider>();
        if (queenCollider3D != null)
            queenCollider3D.enabled = false;
        
        // 선택 해제 ?
        queenBee.SetSelected(false);
        
        if (showDebugLogs)
            Debug.Log("[하이브 초기화] 여왕벌 비활성화 (하이브 안으로 들어감)");
    }
    
    // ...나머지 초기화 코드...
}
```

---

### 2. DestroyHive() - 여왕벌 재활성화

```csharp
// Hive.cs
void DestroyHive()
{
    Debug.Log("[하이브 파괴] 하이브가 파괴됩니다.");
    
    // 경계선 제거
    if (HexBoundaryHighlighter.Instance != null)
    {
        HexBoundaryHighlighter.Instance.Clear();
    }
    
    // 여왕벌 재활성화 (하이브 위치에서) ?
    if (queenBee != null)
    {
        // 여왕벌을 하이브 위치에 배치 ?
        queenBee.SetPosition(q, r);
        Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
        queenBee.transform.position = hivePos;
        
        if (showDebugLogs)
            Debug.Log($"[하이브 파괴] 여왕벌을 하이브 위치 ({q}, {r})에 배치");
        
        // 이동 가능하게 ?
        queenBee.canMove = true;
        
        // 렌더러 활성화 ?
        var queenRenderer = queenBee.GetComponent<Renderer>();
        if (queenRenderer != null)
            queenRenderer.enabled = true;
        
        var queenSprite = queenBee.GetComponent<SpriteRenderer>();
        if (queenSprite != null)
            queenSprite.enabled = true;
        
        // 콜라이더 활성화 (클릭 가능) ?
        var queenCollider = queenBee.GetComponent<Collider2D>();
        if (queenCollider != null)
            queenCollider.enabled = true;
        
        var queenCollider3D = queenBee.GetComponent<Collider>();
        if (queenCollider3D != null)
            queenCollider3D.enabled = true;
        
        Debug.Log("[하이브 파괴] 여왕벌 활성화 완료");
    }
    
    // 일꾼들이 여왕벌을 따라다니게 설정 ?
    foreach (var worker in workers)
    {
        if (worker != null)
        {
            worker.isFollowingQueen = true;
            worker.hasManualOrder = false;
            
            // 현재 작업 취소
            var behavior = worker.GetComponent<UnitBehaviorController>();
            if (behavior != null)
            {
                behavior.CancelCurrentTask();
            }
        }
    }
    
    // HiveManager에서 등록 해제
    if (HiveManager.Instance != null)
    {
        HiveManager.Instance.UnregisterHive(this);
    }
    
    // GameObject 파괴
    Destroy(gameObject);
}
```

---

## ?? 비교표

### 여왕벌 상태

| 시점 | 위치 | canMove | 렌더러 | 콜라이더 | 선택 |
|------|------|---------|--------|----------|------|
| **하이브 건설 전** | 자유 | true | ON | ON | 가능 |
| **하이브 건설 후** | 하이브 | false ? | OFF ? | OFF ? | 불가 ? |
| **하이브 파괴 후** | 하이브 자리 | true ? | ON ? | ON ? | 가능 ? |

---

## ?? 시각화

### 하이브 생애주기

```
[게임 시작]
?? ??????  (여왕벌 + 일꾼들)
   ↓
[하이브 건설]
   ↓
   ??        (하이브만 보임, 여왕벌 숨음) ?
  ??????     (일꾼들만 활동)
   ↓
[하이브 이사/파괴]
   ↓
   ??        (여왕벌 재등장) ?
  ??????     (일꾼들이 여왕벌 추종)
   ↓
[새 위치로 이동]
   ↓
[다시 하이브 건설]
   ↓
   ??        (여왕벌 다시 숨음) ?
  ??????
```

---

### 하이브 파괴 시나리오

```
시나리오 1: 이사
   ?? → (10초) → ?? ?

시나리오 2: 적 공격
   ?? → ?????? → ?? → ?? ?
```

---

## ?? 문제 해결

### Q: 하이브 건설 후에도 여왕벌이 보여요

**확인:**
```
[ ] Initialize() 실행 확인
[ ] queenBee != null 확인
[ ] Renderer.enabled = false 설정
[ ] Collider.enabled = false 설정
```

**해결:**
```
1. Console에서 "[하이브 초기화] 여왕벌 비활성화" 로그 확인
2. queenBee 변수가 할당되었는지 확인
3. Renderer/SpriteRenderer 컴포넌트 존재 확인
```

---

### Q: 하이브 파괴 후 여왕벌이 안 나타나요

**확인:**
```
[ ] DestroyHive() 실행 확인
[ ] queenBee != null 확인
[ ] Renderer.enabled = true 설정
[ ] Collider.enabled = true 설정
[ ] transform.position 설정
```

**해결:**
```
1. Console에서 "[하이브 파괴] 여왕벌 활성화 완료" 로그 확인
2. queenBee GameObject가 파괴되지 않았는지 확인
3. 하이브 위치 (q, r) 확인
```

---

### Q: 여왕벌이 다른 위치에 나타나요

**확인:**
```
[ ] SetPosition(q, r) 호출 확인
[ ] transform.position = hivePos 설정 확인
[ ] TileHelper.HexToWorld() 계산 확인
```

**해결:**
```
1. 하이브의 (q, r) 좌표 확인
2. HexToWorld 변환 확인
3. hexSize 값 확인
```

---

## ?? 추가 개선 아이디어

### 1. 여왕벌 등장 애니메이션

```csharp
// DestroyHive() 수정
if (queenBee != null)
{
    // 위치 설정
    queenBee.SetPosition(q, r);
    queenBee.transform.position = hivePos;
    
    // 등장 애니메이션 ?
    var animator = queenBee.GetComponent<Animator>();
    if (animator != null)
    {
        animator.SetTrigger("Emerge");
    }
    
    // 파티클 효과 ?
    var particles = Instantiate(emergeParticles, hivePos, Quaternion.identity);
    Destroy(particles, 2f);
    
    // 사운드 효과 ?
    AudioSource.PlayClipAtPoint(emergeSound, hivePos);
    
    // 활성화
    queenBee.canMove = true;
    // ...
}
```

---

### 2. 여왕벌 입장 애니메이션

```csharp
// Initialize() 수정
if (queenBee != null)
{
    // 하이브로 이동 애니메이션 ?
    StartCoroutine(MoveQueenToHive(queenBee, q, r));
}

IEnumerator MoveQueenToHive(UnitAgent queen, int targetQ, int targetR)
{
    Vector3 startPos = queen.transform.position;
    Vector3 endPos = TileHelper.HexToWorld(targetQ, targetR, queen.hexSize);
    
    float duration = 1f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        queen.transform.position = Vector3.Lerp(startPos, endPos, t);
        yield return null;
    }
    
    // 위치 확정
    queen.SetPosition(targetQ, targetR);
    queen.transform.position = endPos;
    
    // 비활성화
    queen.canMove = false;
    // ...
}
```

---

### 3. UI 알림

```csharp
// Initialize()
if (queenBee != null)
{
    // 여왕벌 비활성화
    // ...
    
    // UI 알림 ?
    if (NotificationUI.Instance != null)
    {
        NotificationUI.Instance.ShowMessage(
            "여왕벌이 하이브로 들어갔습니다.",
            2f
        );
    }
}

// DestroyHive()
if (queenBee != null)
{
    // 여왕벌 재활성화
    // ...
    
    // UI 알림 ?
    if (NotificationUI.Instance != null)
    {
        NotificationUI.Instance.ShowMessage(
            "하이브가 파괴되었습니다!\n여왕벌이 나타났습니다!",
            3f
        );
    }
}
```

---

### 4. 여왕벌 상태 표시

```csharp
// 여왕벌 위에 상태 아이콘 표시
public class QueenStatusIndicator : MonoBehaviour
{
    public GameObject inHiveIcon;  // 하이브 안 아이콘
    public GameObject activeIcon;   // 활성 상태 아이콘
    
    private UnitAgent queen;
    
    void Update()
    {
        if (queen != null)
        {
            // canMove로 상태 판단
            inHiveIcon.SetActive(!queen.canMove);
            activeIcon.SetActive(queen.canMove);
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 게임 시작 → 여왕벌 보임
[ ] 하이브 건설 → 여왕벌 숨음 ?
[ ] 1번 키 → 하이브 선택 (여왕벌 아님)
[ ] 하이브 이사 → 여왕벌 나타남 ?
[ ] 여왕벌 위치 = 하이브 자리 ?
[ ] 일꾼들이 여왕벌 추종
[ ] 새 위치로 이동 가능
[ ] 다시 하이브 건설 → 여왕벌 숨음 ?
[ ] 적 공격으로 하이브 파괴 → 여왕벌 나타남 ?
```

---

## ?? 완료!

**핵심 수정:**
- ? 하이브 건설 → 여왕벌 비활성화
- ? 하이브 파괴 → 여왕벌 재활성화
- ? 여왕벌 위치 = 하이브 자리

**결과:**
- 자연스러운 게임 플레이
- 여왕벌이 하이브에 숨음
- 파괴 시 하이브 자리에서 등장
- 일꾼들이 여왕벌 추종

**게임 플레이:**
- 하이브 건설 → 여왕벌 보호
- 하이브 파괴 → 여왕벌 재등장
- 이동 → 재건설 → 반복

게임 개발 화이팅! ????????
