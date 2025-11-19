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
    
    private float lastAttackTime = -999f; // 마지막 공격 시간
    private UnitAgent agent;
    
    // 체력/공격력 변경 이벤트
    public event System.Action OnStatsChanged;

    void Awake()
    {
        agent = GetComponent<UnitAgent>();
        health = maxHealth;
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
        
        // 이벤트 발생
        OnStatsChanged?.Invoke();
        
        if (health <= 0)
        {
            health = 0;
            Die();
        }
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
                // 일반 유닛은 바로 파괴
                Destroy(gameObject);
            }
        }
    }
}
