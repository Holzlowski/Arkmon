using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler<Transform> OnDead;
    public event EventHandler OnDamage;

    [SerializeField] private int health = 100;

    private int healthMax;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        Unit unit = GetComponent<Unit>();
        if (unit != null && unit.Definition != null) health = unit.Definition.maxHealth;
        healthMax = health;
    }

    public void Damage(int damage, Transform damageTransform = null)
    {
        if (IsDead) return;
        health -= damage;

        if (health < 0)
        {
            health = 0;
        }

        if (health == 0)
        {
            IsDead = true;
            Die(damageTransform);
        }

        // Resolve death before cosmetic damage/UI listeners can run.
        OnDamage?.Invoke(this, EventArgs.Empty);

        Debug.Log(health);
    }

    private void Die(Transform damageTransform = null)
    {
        OnDead?.Invoke(this, damageTransform);
    }

    public float GetHealthNormalized()
    {
        return (float)health / healthMax;
    }
}
