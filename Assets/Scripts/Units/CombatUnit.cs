using UnityEngine;

public class CombatUnit : MonoBehaviour
{
    public int maxHealth = 10;
    public int health;
    public int attack = 2;
    public float attackRange = 1f; // in tiles (axial distance)

    void Awake()
    {
        health = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // simple destroy for now
        Destroy(gameObject);
    }
}
