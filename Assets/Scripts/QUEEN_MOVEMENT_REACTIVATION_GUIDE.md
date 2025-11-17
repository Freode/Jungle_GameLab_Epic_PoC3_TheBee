# ?? 여왕벌 재활성화 시 이동 가능 수정 가이드

## 완료된 개선사항

### ? DestroyHive() 수정
- 렌더러 재활성화
- 컬라이더 재활성화 (클릭 가능)
- canMove = true 설정
- SetSelected(false) 호출
- 완전한 이동 가능 상태

---

## ?? 수정된 파일

### Hive.cs
```csharp
? DestroyHive() 수정
   1. SetPosition(q, r) - 타일 좌표
   2. transform.position - 월드 좌표
   3. canMove = true - 이동 가능
   4. 렌더러 재활성화 ?
   5. 컬라이더 재활성화 ?
   6. gameObject.SetActive(true)
   7. RegisterWithFog()
   8. SetSelected(false) ?
```

---

## ?? 시스템 작동 방식

### 이전 방식의 문제

```
[하이브 파괴]
DestroyHive()
{
    queenBee.SetPosition(q, r)
    queenBee.transform.position = hivePos
    queenBee.canMove = true ?
    
    queenBee.gameObject.SetActive(true)
    queenBee.RegisterWithFog()
}
   ↓
[문제점]
1. 렌더러 비활성화 상태 ?
   (하이브 건설 시 비활성화됨)
   
2. 컬라이더 비활성화 상태 ?
   (클릭 불가능)
   
3. canMove = true이지만
   실제로 선택/이동 불가능 ?
   ↓
[결과]
여왕벌이 보이지만
클릭도 안 되고
이동 명령도 안 먹힘 ?
```

---

### 새로운 방식

```
[하이브 파괴]
DestroyHive()
{
    // 1. 위치 설정
    queenBee.SetPosition(q, r)
    queenBee.transform.position = hivePos
    
    // 2. 이동 가능 설정
    queenBee.canMove = true ?
    
    // 3. 렌더러 재활성화 ?
    queenRenderer.enabled = true
    queenSprite.enabled = true
    
    // 4. 컬라이더 재활성화 ?
    queenCollider2D.enabled = true
    queenCollider3D.enabled = true
    
    Debug.Log("렌더러 및 컬라이더 재활성화 완료") ?
    
    // 5. GameObject 활성화
    queenBee.gameObject.SetActive(true)
    
    // 6. FogOfWar 등록
    queenBee.RegisterWithFog()
    
    // 7. 선택 상태 리셋 ?
    queenBee.SetSelected(false)
    
    Debug.Log("여왕벌 완전 활성화 완료 - 이동 가능 상태") ?
}
   ↓
[결과]
여왕벌 완전 활성화! ?
- 렌더링 됨 ?
- 클릭 가능 ?
- 선택 가능 ?
- 이동 명령 가능 ?
- 완벽한 상태 ?
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
    
    // 여왕벌 재활성화 ?
    if (queenBee != null)
    {
        if (showDebugLogs)
            Debug.Log($"[하이브 파괴] 여왕벌 위치 초기화 시작: ({q}, {r})");
        
        // 1. 타일 좌표 설정
        queenBee.SetPosition(q, r);
        
        // 2. 월드 좌표 설정
        Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
        queenBee.transform.position = hivePos;
        
        // 3. 이동 가능 설정 ?
        queenBee.canMove = true;
        
        // 4. 렌더러 재활성화 ?
        var queenRenderer = queenBee.GetComponent<Renderer>();
        if (queenRenderer != null) queenRenderer.enabled = true;
        
        var queenSprite = queenBee.GetComponent<SpriteRenderer>();
        if (queenSprite != null) queenSprite.enabled = true;
        
        // 5. 컬라이더 재활성화 (클릭 가능하게) ?
        var queenCollider2D = queenBee.GetComponent<Collider2D>();
        if (queenCollider2D != null) queenCollider2D.enabled = true;
        
        var queenCollider3D = queenBee.GetComponent<Collider>();
        if (queenCollider3D != null) queenCollider3D.enabled = true;
        
        if (showDebugLogs)
            Debug.Log("[하이브 파괴] 여왕벌 렌더러 및 컬라이더 재활성화 완료");
        
        // 6. GameObject 재활성화 ?
        queenBee.gameObject.SetActive(true);
        
        if (showDebugLogs)
            Debug.Log($"[하이브 파괴] 여왕벌 활성화 완료 - 위치: ({queenBee.q}, {queenBee.r})");
        
        // 7. FogOfWar에 등록 ?
        queenBee.RegisterWithFog();
        
        // 8. 선택 가능 상태로 리셋 ?
        queenBee.SetSelected(false);
        
        Debug.Log("[하이브 파괴] 여왕벌 완전 활성화 완료 - 이동 가능 상태");
    }
    else
    {
        Debug.LogWarning("[하이브 파괴] 여왕벌 참조가 없습니다!");
    }
    
    // 일꾼들이 여왕벌을 따라다니게 설정
    foreach (var worker in workers)
    {
        if (worker != null)
        {
            worker.isFollowingQueen = true;
            worker.hasManualOrder = false;
            
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
| **canMove** | true ? | true ? |
| **렌더러** | 비활성화 ? | 활성화 ? |
| **컬라이더** | 비활성화 ? | 활성화 ? |
| **클릭 가능** | 불가능 ? | 가능 ? |
| **선택 상태** | 리셋 안 됨 ? | 리셋 됨 ? |
| **이동 명령** | 안 먹힘 ? | 작동 ? |

---

## ?? 시각화

### 여왕벌 재활성화 흐름

```
[하이브 파괴 시작]
   ↓
[1. 위치 설정]
SetPosition(5, 3)
transform.position = (2.5, 0, 1.5)
   ↓
[2. 이동 가능 설정]
canMove = true ?
   ↓
[3. 렌더러 재활성화]
queenRenderer.enabled = true ?
queenSprite.enabled = true ?
   ↓
[4. 컬라이더 재활성화]
queenCollider2D.enabled = true ?
queenCollider3D.enabled = true ?
Debug.Log("렌더러 및 컬라이더 재활성화 완료") ?
   ↓
[5. GameObject 활성화]
gameObject.SetActive(true) ?
   ↓
[6. FogOfWar 등록]
RegisterWithFog() ?
   ↓
[7. 선택 상태 리셋]
SetSelected(false) ?
Debug.Log("여왕벌 완전 활성화 완료 - 이동 가능 상태") ?
   ↓
[결과]
? 여왕벌 보임
? 클릭 가능
? 선택 가능
? 이동 명령 가능
? 완벽한 상태!
```

---

## ?? 문제 해결

### Q: 여왕벌이 보이는데 클릭이 안 돼요

**확인:**
```
[ ] queenCollider2D.enabled = true
[ ] queenCollider3D.enabled = true
[ ] Console에 "컬라이더 재활성화 완료"
```

**해결:**
```
1. Console 로그 확인
   - "[하이브 파괴] 여왕벌 렌더러 및 컬라이더 재활성화 완료"

2. Inspector 확인
   - Collider2D enabled = true
   - Collider3D enabled = true

3. 안 되면 컬라이더 컴포넌트 확인
   - GetComponent<Collider2D>() != null
```

---

### Q: 여왕벌을 선택해도 이동 명령이 안 먹혀요

**확인:**
```
[ ] queenBee.canMove = true
[ ] SetSelected(false) 호출
[ ] Console에 "이동 가능 상태"
```

**해결:**
```
1. Inspector 확인
   - canMove = true
   - isQueen = true

2. UnitController 확인
   - UnitController 컴포넌트 존재
   - IsMoving() 정상 작동

3. TileClickMover 확인
   - 이동 명령 처리 로직
   - UnitController.SetPath() 호출
```

---

### Q: 여왕벌이 안 보여요

**확인:**
```
[ ] queenRenderer.enabled = true
[ ] queenSprite.enabled = true
[ ] gameObject.SetActive(true)
```

**해결:**
```
1. Console 로그 확인
   - "[하이브 파괴] 여왕벌 렌더러 및 컬라이더 재활성화 완료"

2. Inspector 확인
   - Renderer enabled = true
   - SpriteRenderer enabled = true
   - GameObject active = true

3. 안 되면 렌더러 컴포넌트 확인
```

---

## ?? 추가 개선 아이디어

### 1. 여왕벌 등장 이펙트

```csharp
// Hive.cs
IEnumerator AnimateQueenRevive()
{
    if (queenBee == null) yield break;
    
    // 위치 설정
    queenBee.SetPosition(q, r);
    Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
    queenBee.transform.position = hivePos;
    
    // 크기 0에서 시작
    queenBee.transform.localScale = Vector3.zero;
    
    // 렌더러/컬라이더 활성화
    EnableQueenComponents(true);
    
    // GameObject 활성화
    queenBee.gameObject.SetActive(true);
    
    // 크기 증가 애니메이션 ?
    float duration = 0.5f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        queenBee.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
        yield return null;
    }
    
    queenBee.transform.localScale = Vector3.one;
    
    // FogOfWar 등록
    queenBee.RegisterWithFog();
    queenBee.SetSelected(false);
}

void DestroyHive()
{
    // ...existing code...
    
    if (queenBee != null)
    {
        StartCoroutine(AnimateQueenRevive()); ?
    }
}
```

---

### 2. 여왕벌 부활 이펙트

```csharp
// Hive.cs
[Header("Effects")]
public GameObject reviveEffect;
public AudioClip reviveSound;

void DestroyHive()
{
    // ...existing code...
    
    if (queenBee != null)
    {
        // 위치 설정
        queenBee.SetPosition(q, r);
        Vector3 hivePos = TileHelper.HexToWorld(q, r, queenBee.hexSize);
        queenBee.transform.position = hivePos;
        
        // 부활 이펙트 ?
        if (reviveEffect != null)
        {
            var effect = Instantiate(reviveEffect, hivePos, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // 부활 사운드 ?
        if (reviveSound != null)
        {
            AudioSource.PlayClipAtPoint(reviveSound, hivePos);
        }
        
        // 컴포넌트 활성화
        EnableQueenComponents(true);
        queenBee.gameObject.SetActive(true);
        queenBee.RegisterWithFog();
        queenBee.SetSelected(false);
    }
}

void EnableQueenComponents(bool enabled)
{
    // 렌더러
    var queenRenderer = queenBee.GetComponent<Renderer>();
    if (queenRenderer != null) queenRenderer.enabled = enabled;
    
    var queenSprite = queenBee.GetComponent<SpriteRenderer>();
    if (queenSprite != null) queenSprite.enabled = enabled;
    
    // 컬라이더
    var queenCollider2D = queenBee.GetComponent<Collider2D>();
    if (queenCollider2D != null) queenCollider2D.enabled = enabled;
    
    var queenCollider3D = queenBee.GetComponent<Collider>();
    if (queenCollider3D != null) queenCollider3D.enabled = enabled;
}
```

---

### 3. 알림 시스템

```csharp
// Hive.cs
void DestroyHive()
{
    // ...existing code...
    
    if (queenBee != null)
    {
        // 컴포넌트 활성화
        EnableQueenComponents(true);
        queenBee.gameObject.SetActive(true);
        queenBee.RegisterWithFog();
        queenBee.SetSelected(false);
        
        // 알림 표시 ?
        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.ShowNotification(
                "여왕벌이 하이브에서 나왔습니다!",
                3f,
                queenBee.transform.position
            );
        }
        
        // 카메라 이동 ?
        if (CameraController.Instance != null)
        {
            CameraController.Instance.FocusOn(queenBee.transform.position, 1f);
        }
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] 하이브 이사 명령
[ ] 10초 대기
[ ] 하이브 파괴
[ ] Console: "렌더러 및 컬라이더 재활성화 완료" ?
[ ] Console: "여왕벌 완전 활성화 완료 - 이동 가능 상태" ?
[ ] 여왕벌 보임 ?
[ ] 여왕벌 클릭 가능 ?
[ ] 여왕벌 선택 가능 ?
[ ] 타일 클릭 → 여왕벌 이동 ?
[ ] Inspector: canMove = true ?
[ ] Inspector: Renderer enabled = true ?
[ ] Inspector: Collider enabled = true ?
```

---

## ?? 완료!

**핵심 수정:**
- ? 렌더러 재활성화
- ? 컬라이더 재활성화
- ? canMove = true 설정
- ? SetSelected(false) 호출

**결과:**
- 여왕벌 완전 활성화
- 클릭 가능
- 선택 가능
- 이동 명령 가능
- 완벽한 게임 플레이

**게임 플레이:**
- 하이브 파괴 시 여왕벌 등장
- 여왕벌 클릭 가능
- 이동 명령 정상 작동
- 자연스러운 게임 흐름

게임 개발 화이팅! ???????
