using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler<Transform> OnDead;
    public event EventHandler OnDamage;

    [SerializeField] private int health = 100;

    private int healthMax;

    private void Awake()
    {
        healthMax = health;
    }

    public void Damage(int damage, Transform damageTransform = null)
    {
        health -= damage;

        if (health < 0)
        {
            health = 0;
        }

        OnDamage?.Invoke(this, EventArgs.Empty);

        if (health == 0)
        {
            Die(UnitActionSystem.Instance.GetSelectedUnit().transform);
        }

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
