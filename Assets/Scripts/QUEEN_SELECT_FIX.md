# ?? 하이브 파괴 시 여왕벌 선택 활성화 수정 가이드

## 완료된 개선사항

### ? 하이브 파괴 시 여왕벌 선택 가능
- canMove = true 설정
- 렌더러 활성화
- 콜라이더 활성화
- 1번 키로 선택 가능

---

## ?? 수정된 파일

### Hive.cs
```csharp
? DestroyHive() 수정
   - queenBee.canMove = true ?
   - Renderer/SpriteRenderer 활성화 ?
   - Collider2D/Collider 활성화 ?
   - 디버그 로그 추가
```

---

## ?? 시스템 작동 방식

### 문제 상황 (이전)

```
[하이브 존재]
??
 |
?? (canMove = false) ?
   ↓
[하이브 파괴]
   ↓
[여왕벌 상태]
canMove: false ?
Renderer: enabled
Collider: enabled
   ↓
[1번 키 누름]
   ↓
[TileClickMover.SelectUnit()]
   ↓
if (unit.isQueen && !unit.canMove) ?
{
    return; // 선택 불가 ?
}
```

---

### 해결 (현재)

```
[하이브 존재]
??
 |
?? (canMove = false)
   ↓
[하이브 파괴]
DestroyHive()
   ↓
[여왕벌 활성화] ?
queenBee.canMove = true ?
Renderer.enabled = true ?
Collider.enabled = true ?
   ↓
[여왕벌 상태]
canMove: true ?
Renderer: enabled ?
Collider: enabled ?
   ↓
[1번 키 누름]
   ↓
[HotkeyManager.SelectHiveOrQueen()]
   ↓
FindPlayerHive() → null (하이브 없음)
   ↓
FindPlayerQueen() → queen ?
   ↓
[TileClickMover.SelectUnit(queen)]
   ↓
if (unit.isQueen && !unit.canMove) ?
{
    // canMove = true이므로 통과 ?
}
   ↓
[여왕벌 선택 성공] ?
selectedUnitInstance = queen
UnitCommandPanel.Show(queen)
```

---

## ?? 핵심 코드

### DestroyHive() 수정

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
    
    // 여왕벌 부활 ?
    if (queenBee != null)
    {
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
    
    // 일꾼들이 여왕벌을 따라다니게 설정
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

| 항목 | 이전 | 현재 |
|------|------|------|
| **canMove** | false | true ? |
| **Renderer** | enabled | enabled ? |
| **Collider** | enabled | enabled ? |
| **1번 키 선택** | 불가 | 가능 ? |
| **클릭 선택** | 불가 | 가능 ? |

---

## ?? 시각화

### 하이브 파괴 전후

```
[하이브 파괴 전]
      ??
       |
      ??
      
canMove: false ?
Renderer: enabled
Collider: enabled

[1번 키]
→ 선택 불가 ?

         ↓
     [하이브 파괴]
         ↓

[하이브 파괴 후]
      ??
      
canMove: true ?
Renderer: enabled ?
Collider: enabled ?

[1번 키]
→ 선택 가능 ?
```

---

### 선택 흐름

```
[1번 키 누름]
   ↓
[HotkeyManager]
   ↓
FindPlayerHive()?
   NO → null
   ↓
FindPlayerQueen()?
   YES → queen ?
   ↓
[SelectQueen(queen)]
   ↓
[TileClickMover.SelectUnit(queen)]
   ↓
canMove 체크
   true → 선택 가능 ?
   ↓
[여왕벌 선택 완료]
selectedUnitInstance = queen
UnitCommandPanel.Show(queen)
```

---

## ?? 문제 해결

### Q: 하이브 파괴 후에도 여왕벌이 선택 안 돼요

**확인:**
```
[ ] queenBee != null 확인
[ ] queenBee.canMove = true 설정 확인
[ ] Console에서 "[하이브 파괴] 여왕벌 활성화 완료" 로그 확인
[ ] Collider 활성화 확인
```

**해결:**
```
1. Console에서 DestroyHive() 실행 확인
2. queenBee 변수가 null이 아닌지 확인
3. canMove 값 확인 (Inspector 또는 디버그)
4. Collider 컴포넌트 존재 확인
```

---

### Q: 1번 키를 눌러도 아무 일도 안 일어나요

**확인:**
```
[ ] HotkeyManager GameObject 존재
[ ] HotkeyManager.Instance != null
[ ] FindPlayerQueen() 작동
[ ] TileClickMover.Instance != null
```

**해결:**
```
1. Hierarchy에서 HotkeyManager 확인
2. Console에서 "[핫키] ..." 로그 확인
3. 여왕벌이 씬에 존재하는지 확인
4. 여왕벌 faction = Player 확인
```

---

### Q: 클릭해도 여왕벌이 선택 안 돼요

**확인:**
```
[ ] Collider2D 또는 Collider 활성화
[ ] canMove = true
[ ] TileClickMover.SelectUnit() 실행
[ ] isQueen && !canMove 체크 통과
```

**해결:**
```
1. Collider 컴포넌트 확인
2. Collider.enabled = true 확인
3. canMove 값 확인
4. Console에서 선택 로그 확인
```

---

## ?? 추가 개선 아이디어

### 1. 여왕벌 부활 애니메이션

```csharp
// DestroyHive() 수정
if (queenBee != null)
{
    // 부활 애니메이션
    var animator = queenBee.GetComponent<Animator>();
    if (animator != null)
    {
        animator.SetTrigger("Revive");
    }
    
    // 파티클 효과
    var particles = Instantiate(reviveParticles, queenBee.transform.position, Quaternion.identity);
    Destroy(particles, 2f);
    
    // 사운드 효과
    AudioSource.PlayClipAtPoint(reviveSound, queenBee.transform.position);
    
    // canMove 활성화
    queenBee.canMove = true;
}
```

---

### 2. 여왕벌 자동 선택

```csharp
// DestroyHive() 수정
if (queenBee != null)
{
    queenBee.canMove = true;
    // ...렌더러/콜라이더 활성화...
    
    // 여왕벌 자동 선택 ?
    if (TileClickMover.Instance != null)
    {
        StartCoroutine(SelectQueenAfterDelay(0.5f));
    }
}

IEnumerator SelectQueenAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (queenBee != null && TileClickMover.Instance != null)
    {
        TileClickMover.Instance.SelectUnit(queenBee);
        Debug.Log("[하이브 파괴] 여왕벌 자동 선택 완료");
    }
}
```

---

### 3. 여왕벌 위치 마커

```csharp
// 여왕벌 위치 강조
if (queenBee != null)
{
    // 위치 마커 생성
    var marker = Instantiate(queenMarkerPrefab, queenBee.transform.position, Quaternion.identity);
    marker.transform.SetParent(queenBee.transform);
    
    // 3초 후 제거
    Destroy(marker, 3f);
}
```

---

### 4. UI 알림

```csharp
// DestroyHive() 수정
if (queenBee != null)
{
    queenBee.canMove = true;
    
    // UI 알림 표시
    if (NotificationUI.Instance != null)
    {
        NotificationUI.Instance.ShowMessage(
            "하이브가 파괴되었습니다!\n여왕벌이 활성화되었습니다.",
            3f
        );
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 건설
[ ] 하이브 이사 명령
[ ] 10초 대기
[ ] 하이브 파괴
[ ] Console에서 "[하이브 파괴] 여왕벌 활성화 완료" 확인
[ ] 1번 키 누름
[ ] 여왕벌 선택 확인
[ ] 명령 패널 표시 확인
[ ] 여왕벌 클릭
[ ] 여왕벌 이동 가능 확인
```

---

## ?? 완료!

**핵심 수정:**
- ? DestroyHive()에서 canMove = true
- ? Renderer/Collider 활성화
- ? 1번 키로 선택 가능

**결과:**
- 하이브 파괴 후 여왕벌 선택 가능
- 1번 키로 즉시 선택
- 클릭으로도 선택 가능
- 명령 패널 정상 표시

**게임 플레이:**
- 하이브 파괴 → 여왕벌 선택
- 여왕벌로 새 위치 이동
- 다시 하이브 건설
- 자연스러운 플레이 흐름

게임 개발 화이팅! ??????
