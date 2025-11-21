using UnityEngine;

public class CombatUnit : MonoBehaviour
{
    [Header("기본 능력치")]
    public int maxHealth = 10;
    public int health;
    public int attack = 2;
    public float attackRange = 0.1f; // 공격 범위 (타일 거리 기준)

    [Header("공격 쿨타임")]
    public float attackCooldown = 2f; // 공격 쿨타임 (초)
    
    [Header("무적 상태")]
    public bool isInvincible = false; // 무적 상태 ?
    
    [Header("피격 이펙트")]
    [Tooltip("피격 시 색상")]
    public Color hitFlashColor = Color.white;
    [Tooltip("플래시 지속 시간")]
    public float hitFlashDuration = 0.2f;
    
    private float lastAttackTime = -999f; // 마지막 공격 시간
    private UnitAgent agent;
    private SpriteRenderer spriteRenderer;
    private Renderer meshRenderer;
    private Color originalColor;
    private MaterialPropertyBlock mpb;
    private Coroutine hitFlashCoroutine;
    
    // 체력/공격력 변경 이벤트
    public event System.Action OnStatsChanged;

    void Awake()
    {
        agent = GetComponent<UnitAgent>();
        health = maxHealth;
        
        // 렌더러 초기화
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            // 자식에서 찾기
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }
        
        // 3D Renderer도 지원
        if (spriteRenderer == null)
        {
            meshRenderer = GetComponent<Renderer>();
            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<Renderer>();
            }
            
            if (meshRenderer != null)
            {
                mpb = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(mpb);
                originalColor = mpb.GetColor("_Color");
                if (originalColor == Color.clear)
                {
                    originalColor = Color.white;
                }
            }
        }
    }

    /// <summary>
    /// 공격 가능한지 확인
    /// </summary>
    public bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    /// <summary>
    /// 다음 공격까지 남은 시간
    /// </summary>
    public float GetAttackCooldownRemaining()
    {
        float remaining = attackCooldown - (Time.time - lastAttackTime);
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// 타겟 공격 (쿨타임 체크 포함)
    /// </summary>
    public bool TryAttack(CombatUnit target)
    {
        if (target == null) return false;
        
        // 무적 상태 체크 ?
        if (target.isInvincible)
        {
            Debug.Log($"[전투] {target.name}은(는) 무적 상태입니다. 공격 불가!");
            return false;
        }
        
        // 쿨타임 체크
        if (!CanAttack()) return false;
        
        // 공격 실행
        target.TakeDamage(attack);
        lastAttackTime = Time.time;
        
        return true;
    }

    public void TakeDamage(int dmg)
    {
        // 무적 상태면 데미지 무시 ?
        if (isInvincible)
        {
            Debug.Log($"[전투] {gameObject.name}은(는) 무적 상태라 데미지를 받지 않습니다!");
            return;
        }
        
        health -= dmg;
        
        // ✅ 피격 색상 플래시 효과
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }
        hitFlashCoroutine = StartCoroutine(HitFlashEffect());
        
        // 이벤트 발생
        OnStatsChanged?.Invoke();
        
        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }
    
    /// <summary>
    /// 피격 색상 플래시 효과 ✅
    /// </summary>
    System.Collections.IEnumerator HitFlashEffect()
    {
        // SpriteRenderer 사용
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            
            // ✅ 선택 상태 및 자원 보유 상태 확인 후 색상 복원
            if (agent != null && agent.GetComponent<UnitAgent>() != null)
            {
                var unitAgent = agent.GetComponent<UnitAgent>();
                if (unitAgent != null)
                {
                    // 현재 선택 상태 확인
                    bool isSelected = false;
                    
                    if (TileClickMover.Instance != null && TileClickMover.Instance.GetSelectedUnit() == unitAgent)
                    {
                        isSelected = true;
                    }
                    else if (DragSelector.Instance != null && DragSelector.Instance.IsUnitSelected(unitAgent))
                    {
                        isSelected = true;
                    }
                    
                    // SetSelected 호출 (내부에서 자원 보유 상태도 고려)
                    unitAgent.SetSelected(isSelected);
                }
            }
            else
            {
                // UnitAgent가 없으면 원래 색상으로 복원
                spriteRenderer.color = originalColor;
            }
        }
        // Mesh Renderer 사용
        else if (meshRenderer != null && mpb != null)
        {
            mpb.SetColor("_Color", hitFlashColor);
            meshRenderer.SetPropertyBlock(mpb);
            
            yield return new WaitForSeconds(hitFlashDuration);
            
            // ✅ 선택 상태 및 자원 보유 상태 확인 후 색상 복원
            if (agent != null && agent.GetComponent<UnitAgent>() != null)
            {
                var unitAgent = agent.GetComponent<UnitAgent>();
                if (unitAgent != null)
                {
                    bool isSelected = false;
                    
                    if (TileClickMover.Instance != null && TileClickMover.Instance.GetSelectedUnit() == unitAgent)
                    {
                        isSelected = true;
                    }
                    else if (DragSelector.Instance != null && DragSelector.Instance.IsUnitSelected(unitAgent))
                    {
                        isSelected = true;
                    }
                    
                    unitAgent.SetSelected(isSelected);
                }
            }
            else
            {
                mpb.SetColor("_Color", originalColor);
                meshRenderer.SetPropertyBlock(mpb);
            }
        }
        
        hitFlashCoroutine = null;
    }

    /// <summary>
    /// 무적 상태 설정 ?
    /// </summary>
    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;

        // 현재 체력 회복
        health = maxHealth;
        
        if (invincible)
            Debug.Log($"[무적] {gameObject.name} 무적 상태 활성화");
        else
            Debug.Log($"[무적] {gameObject.name} 무적 상태 해제");
    }

    /// <summary>
    /// 체력 설정 (이벤트 발생)
    /// </summary>
    public void SetHealth(int newHealth)
    {
        health = Mathf.Clamp(newHealth, 0, maxHealth);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 최대 체력 설정 (이벤트 발생)
    /// </summary>
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        health = Mathf.Clamp(health, 0, maxHealth);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 공격력 설정 (이벤트 발생)
    /// </summary>
    public void SetAttack(int newAttack)
    {
        attack = newAttack;
        OnStatsChanged?.Invoke();
    }

    void Die()
    {
        // 일꾼인지 확인
        bool isWorker = false;
        UnitAgent workerAgent = null;
        
        // 플레이어 하이브인 경우 DestroyHive() 호출
        var hive = GetComponent<Hive>();
        if (hive != null)
        {
            // Hive의 DestroyHive() 메서드를 리플렉션으로 호출
            var destroyMethod = typeof(Hive).GetMethod("DestroyHive", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (destroyMethod != null)
            {
                destroyMethod.Invoke(hive, null);
            }
            else
            {
                // DestroyHive 메서드가 없으면 직접 파괴
                Destroy(gameObject);
            }
        }
        // 적 하이브인 경우 EnemyHive.DestroyHive() 호출
        else
        {
            var enemyHive = GetComponent<EnemyHive>();
            if (enemyHive != null)
            {
                enemyHive.DestroyHive(); // public 메서드이므로 직접 호출
            }
            else
            {
                // 일반 유닛 (일꾼)
                isWorker = true;
                workerAgent = GetComponent<UnitAgent>();
                
                // HiveManager에서 일꾼 해제 ✅
                if (workerAgent != null && HiveManager.Instance != null)
                {
                    HiveManager.Instance.UnregisterWorker(workerAgent);
                }
                
                // 일반 유닛은 바로 파괴
                Destroy(gameObject);
            }
        }
    }
}
