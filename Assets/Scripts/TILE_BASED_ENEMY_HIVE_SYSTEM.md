# ?? 적 하이브 타일 기반 시스템 개선 가이드

## 완료된 개선사항

### ? 타일에 EnemyHive 직접 부착
- HexTile.enemyHive 필드 추가
- EnemyHive를 타일의 자식으로 설정
- FogOfWarManager에서 시야 제어
- 위치 문제 완전 해결

---

## ?? 수정된 파일

### 1. HexTile.cs
```csharp
? enemyHive 필드 추가
   public EnemyHive enemyHive;
```

### 2. EnemyHive.cs
```csharp
? Initialize() 수정
   - 타일 찾기
   - 타일의 자식으로 설정
   - 타일에 등록
   - localPosition = Vector3.zero
```

### 3. FogOfWarManager.cs
```csharp
? RecalculateVisibility() 수정
   - 타일의 enemyHive 체크
   - 시야 진입 시 렌더러 활성화
   
? EnableEnemyHiveRenderers() 추가
   - 렌더러 활성화/비활성화
```

---

## ?? 시스템 작동 방식

### 이전 방식의 문제

```
[이전 방식]
EnemyHive GameObject
├─ transform.position = HexToWorld(q, r) ?
│  (독립적인 월드 좌표)
└─ 부모 없음 ?
   ↓
[문제점]
1. 위치가 (0, 0)으로 초기화 ?
2. Gizmo가 엉뚱한 곳에 표시 ?
3. 타일과 연동 어려움 ?
4. Collider 위치 문제 ?
```

---

### 새로운 방식

```
[새로운 방식]
HexTile GameObject at (5, 3)
├─ EnemyHive (자식) ?
│  ├─ transform.localPosition = Vector3.zero ?
│  ├─ q = 5, r = 3 ?
│  └─ tile.enemyHive = this ?
└─ fogState = Visible/Revealed/Hidden
   ↓
[FogOfWarManager.RecalculateVisibility()]
foreach (var tile in GetAllTiles())
{
    if (isVisible)
    {
        tile.SetFogState(Visible)
        
        if (tile.enemyHive != null)
        {
            EnableEnemyHiveRenderers(tile.enemyHive, true) ?
        }
    }
    else if (tile.fogState == Revealed)
    {
        // 한 번 발견된 하이브는 계속 보임 ?
        if (tile.enemyHive != null)
        {
            EnableEnemyHiveRenderers(tile.enemyHive, true) ?
        }
    }
}
   ↓
[장점]
1. 위치 문제 완전 해결 ?
2. Gizmo 정확한 위치 ?
3. 타일과 완벽 연동 ?
4. Collider 올바른 위치 ?
5. Fog of War와 자동 연동 ?
```

---

## ?? 핵심 코드

### 1. HexTile.cs 수정

```csharp
// HexTile.cs
public class HexTile : MonoBehaviour
{
    public int q;
    public int r;
    
    public TerrainType terrain;
    public int resourceAmount = 0;
    
    public enum FogState { Hidden, Revealed, Visible }
    public FogState fogState = FogState.Hidden;
    
    // 타일 위의 EnemyHive (있으면 설정) ?
    public EnemyHive enemyHive;
    
    // ...existing code...
}
```

---

### 2. EnemyHive.Initialize() 수정

```csharp
// EnemyHive.cs
/// <summary>
/// 말벌집 초기화
/// </summary>
public void Initialize(int q, int r, int maxHealth = 250)
{
    this.q = q;
    this.r = r;

    // 타일 찾기 ?
    if (TileManager.Instance != null)
    {
        var tile = TileManager.Instance.GetTile(q, r);
        if (tile != null)
        {
            // 타일의 자식으로 설정 ?
            transform.SetParent(tile.transform);
            
            // 타일에 등록 ?
            tile.enemyHive = this;
            
            // 타일 중심 위치 (로컬 좌표) ?
            transform.localPosition = Vector3.zero;
            
            if (showDebugLogs)
                Debug.Log($"[적 하이브] 타일에 부착: ({q}, {r})");
        }
        else
        {
            Debug.LogError($"[적 하이브] 타일을 찾을 수 없습니다: ({q}, {r})");
            
            // 타일이 없으면 월드 좌표로 설정
            Vector3 worldPos = TileHelper.HexToWorld(q, r, 0.5f);
            transform.position = worldPos;
        }
    }

    // UnitAgent 설정
    if (hiveAgent != null)
    {
        hiveAgent.SetPosition(q, r);
        hiveAgent.faction = Faction.Enemy;
        hiveAgent.visionRange = visionRange;
        hiveAgent.canMove = false;
    }

    // CombatUnit 설정
    if (combat != null)
    {
        combat.maxHealth = maxHealth;
        combat.health = maxHealth;
        combat.attack = 0;
    }

    // 초기에는 모든 렌더러 비활성화
    SetAllRenderersEnabled(false);

    // EnemyVisibilityController에 즉시 업데이트 요청
    if (EnemyVisibilityController.Instance != null)
    {
        StartCoroutine(InitializeVisibilityCheck());
    }

    if (showDebugLogs)
        Debug.Log($"[적 하이브] 초기화 완료: ({q}, {r})");
}
```

---

### 3. FogOfWarManager.RecalculateVisibility() 수정

```csharp
// FogOfWarManager.cs
public void RecalculateVisibility()
{
    var tm = TileManager.Instance;
    if (tm == null) return;

    // compute union of visible tile coordinates
    var visibleCoords = new HashSet<Vector2Int>();
    foreach (var kv in unitSight)
    {
        var center = kv.Value.pos;
        int vision = kv.Value.vision;
        var tilesInRange = GetTilesCoordsInRange(center.x, center.y, vision);
        foreach (var c in tilesInRange) visibleCoords.Add(c);
    }

    // apply fog state changes
    foreach (var tile in tm.GetAllTiles())
    {
        var coord = new Vector2Int(tile.q, tile.r);
        bool isVisible = visibleCoords.Contains(coord);
        
        if (isVisible)
        {
            tile.SetFogState(HexTile.FogState.Visible);
            
            // 타일에 EnemyHive가 있으면 렌더러 활성화 ?
            if (tile.enemyHive != null)
            {
                EnableEnemyHiveRenderers(tile.enemyHive, true);
            }
        }
        else
        {
            if (tile.fogState == HexTile.FogState.Visible)
            {
                tile.SetFogState(HexTile.FogState.Revealed);
            }
            
            // 한 번 발견된 EnemyHive는 계속 보이게 ?
            if (tile.enemyHive != null && tile.fogState == HexTile.FogState.Revealed)
            {
                EnableEnemyHiveRenderers(tile.enemyHive, true);
            }
        }
    }
}
```

---

### 4. EnableEnemyHiveRenderers() 추가

```csharp
// FogOfWarManager.cs
/// <summary>
/// EnemyHive의 렌더러 활성화/비활성화 ?
/// </summary>
void EnableEnemyHiveRenderers(EnemyHive hive, bool enabled)
{
    if (hive == null) return;
    
    // 자신의 렌더러
    var sprite = hive.GetComponent<SpriteRenderer>();
    if (sprite != null) sprite.enabled = enabled;
    
    var renderer = hive.GetComponent<Renderer>();
    if (renderer != null) renderer.enabled = enabled;
    
    // 자식 렌더러
    var childRenderers = hive.GetComponentsInChildren<Renderer>(true);
    foreach (var r in childRenderers)
    {
        r.enabled = enabled;
    }
    
    var childSprites = hive.GetComponentsInChildren<SpriteRenderer>(true);
    foreach (var s in childSprites)
    {
        s.enabled = enabled;
    }
    
    if (enabled)
    {
        Debug.Log($"[Fog] 적 말벌집 발견: ({hive.q}, {hive.r})");
    }
}
```

---

## ?? 비교표

| 항목 | 이전 | 현재 |
|------|------|------|
| **부모 관계** | 없음 ? | HexTile ? |
| **위치 설정** | transform.position ? | localPosition ? |
| **타일 연동** | 없음 ? | tile.enemyHive ? |
| **Fog 연동** | 별도 시스템 ? | 자동 연동 ? |
| **Gizmo 위치** | (0,0) ? | 정확 ? |
| **Collider 위치** | (0,0) ? | 정확 ? |

---

## ?? 시각화

### 계층 구조

```
[씬 구조]
TileManager
├─ HexTile (0, 0)
│  └─ (비어있음)
├─ HexTile (5, 3)
│  └─ EnemyHive ?
│     ├─ q = 5, r = 3
│     ├─ localPosition = (0, 0, 0) ?
│     ├─ SpriteRenderer (enabled = false → true)
│     └─ UnitAgent (faction = Enemy)
└─ HexTile (8, 7)
   └─ (비어있음)
```

### 발견 흐름

```
[플레이어 유닛 이동]
UnitAgent at (4, 3)
visionRange = 3
   ↓
[FogOfWarManager.RecalculateVisibility()]
visibleCoords = [(3,2), (4,2), (5,2), (3,3), (4,3), (5,3), ...]
   ↓
[타일 (5, 3) 체크]
isVisible = true ?
tile.SetFogState(Visible)
   ↓
[타일에 enemyHive 있는지 체크]
if (tile.enemyHive != null) ?
{
    EnableEnemyHiveRenderers(tile.enemyHive, true)
    {
        SpriteRenderer.enabled = true ?
        Renderer.enabled = true ?
        자식 렌더러.enabled = true ?
        
        Debug.Log("적 말벌집 발견!") ?
    }
}
   ↓
[결과]
??? 말벌집 보임! ?
Gizmo 올바른 위치 ?
Collider 올바른 위치 ?
```

---

## ?? 문제 해결

### Q: 타일이 없다는 에러가 나와요

**확인:**
```
[ ] TileManager.Instance != null
[ ] TileManager.GetTile(q, r) != null
[ ] 타일이 생성되었는지 확인
```

**해결:**
```
1. GameManager에서 타일 생성 확인
2. EnemyHive.Initialize()에서 에러 로그 확인
   - "[적 하이브] 타일을 찾을 수 없습니다"
3. 타일이 없으면 월드 좌표로 폴백
```

---

### Q: 말벌집이 여전히 (0,0)에 있어요

**확인:**
```
[ ] Initialize() 호출 확인
[ ] transform.SetParent(tile.transform) 실행 확인
[ ] transform.localPosition = Vector3.zero 실행 확인
```

**해결:**
```
1. Console 로그 확인
   - "[적 하이브] 타일에 부착: (5, 3)"
2. Unity Editor에서 Hierarchy 확인
   - EnemyHive가 HexTile의 자식인지 확인
3. Inspector에서 Transform 확인
   - Local Position = (0, 0, 0)
```

---

### Q: 말벌집이 안 보여요

**확인:**
```
[ ] FogOfWarManager.RecalculateVisibility() 실행
[ ] tile.enemyHive != null
[ ] EnableEnemyHiveRenderers() 호출
```

**해결:**
```
1. Console 로그 확인
   - "[Fog] 적 말벌집 발견: (5, 3)"
2. 렌더러 확인
   - SpriteRenderer.enabled = true
   - Renderer.enabled = true
3. Fog State 확인
   - tile.fogState = Visible 또는 Revealed
```

---

## ?? 추가 개선 아이디어

### 1. 타일 하이라이트

```csharp
// HexTile.cs
public void HighlightEnemyHive(bool highlight)
{
    if (enemyHive == null) return;
    
    if (highlight)
    {
        // 빨간색 테두리 표시
        var outline = enemyHive.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = Color.red;
        }
    }
    else
    {
        var outline = enemyHive.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }
}
```

---

### 2. 발견 시 카메라 이동

```csharp
// FogOfWarManager.cs
void EnableEnemyHiveRenderers(EnemyHive hive, bool enabled)
{
    if (hive == null) return;
    
    // ...existing code...
    
    if (enabled)
    {
        Debug.Log($"[Fog] 적 말벌집 발견: ({hive.q}, {hive.r})");
        
        // 카메라 이동 (선택적) ?
        if (CameraController.Instance != null)
        {
            Vector3 hivePos = TileHelper.HexToWorld(hive.q, hive.r, 0.5f);
            CameraController.Instance.MoveTo(hivePos, 1f);
        }
    }
}
```

---

### 3. 타일 정보 UI

```csharp
// TileInfoUI.cs
public class TileInfoUI : MonoBehaviour
{
    public void ShowTileInfo(HexTile tile)
    {
        if (tile == null) return;
        
        string info = $"타일: ({tile.q}, {tile.r})\n";
        info += $"지형: {tile.terrain?.name ?? "없음"}\n";
        info += $"자원: {tile.resourceAmount}\n";
        
        if (tile.enemyHive != null) ?
        {
            info += $"?? 적 말벌집\n";
            info += $"체력: {tile.enemyHive.GetComponent<CombatUnit>()?.health ?? 0}\n";
        }
        
        infoText.text = info;
    }
}
```

---

## ? 테스트 체크리스트

```
[ ] EnemyHive 생성
[ ] Console에 "타일에 부착" 로그 ?
[ ] Hierarchy에서 타일 자식 확인 ?
[ ] Transform Local Position = (0,0,0) ?
[ ] Gizmo가 올바른 위치에 표시 ?
[ ] 플레이어 유닛 시야 진입
[ ] Console에 "적 말벌집 발견" 로그 ?
[ ] 말벌집 렌더러 활성화 ?
[ ] 말벌집 보임 ?
[ ] 시야 벗어남
[ ] 말벌집 계속 보임 (Revealed) ?
```

---

## ?? 완료!

**핵심 개선:**
- ? 타일에 EnemyHive 직접 부착
- ? localPosition으로 위치 문제 해결
- ? FogOfWarManager에서 자동 제어
- ? 완벽한 타일 연동

**결과:**
- Gizmo 정확한 위치
- Collider 올바른 위치
- Fog of War와 완벽 연동
- 깔끔한 계층 구조

**게임 플레이:**
- 말벌집이 타일에 정확히 배치
- 플레이어 시야 진입 시 발견
- 한 번 발견하면 계속 보임
- 자연스러운 탐색 경험

게임 개발 화이팅! ?????????
