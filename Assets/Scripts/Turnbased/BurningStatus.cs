using System;
using System.Collections.Generic;
using UnityEngine;

// Shared by units and single-cell obstacles. A spread never refreshes an existing fire.
public class BurningStatus : MonoBehaviour
{
    private static readonly HashSet<BurningStatus> active = new HashSet<BurningStatus>();
    public static IEnumerable<BurningStatus> Active => active;
    public int RemainingRounds { get; private set; }
    public int DamagePerRound { get; private set; }
    public bool IsBurning => RemainingRounds > 0 && CanBurn;
    public Unit Unit => GetComponent<Unit>();
    public BurningObstacle Obstacle => GetComponent<BurningObstacle>();
    public bool CanBurn => isActiveAndEnabled && (Unit != null
        ? !Unit.IsDead && !(Unit.Definition != null && Unit.Definition.immuneToBurning)
        : Obstacle != null && !Obstacle.IsDestroyed && Obstacle.IsVegetation);
    public GridPosition Position => Unit != null ? Unit.GetGridPosition() : Obstacle.GetGridPosition();
    private HealthSystem health;
    private GameObject visual;

    private void OnEnable()
    {
        active.Add(this);
        health = GetComponent<HealthSystem>();
        if (health != null) health.OnDead += OnDead;
    }

    public bool TryIgnite(float chance, int rounds, int damage)
    {
        if (!CanBurn || chance <= 0f || rounds <= 0 || damage <= 0) return false;
        if (chance < 1f && UnityEngine.Random.value >= chance) return false;
        RemainingRounds = rounds;
        DamagePerRound = damage;
        if (Application.isPlaying && visual == null)
        {
            visual = new GameObject("Burning flames");
            visual.transform.SetParent(transform, false);
            visual.AddComponent<BurningVisual>();
        }
        return true;
    }

    public void Tick()
    {
        if (!IsBurning) { Extinguish(); return; }
        int damage = DamagePerRound;
        RemainingRounds--;
        if (Unit != null) Unit.Damage(damage);
        else Obstacle.Damage(damage);
        if (RemainingRounds == 0 || !CanBurn) Extinguish();
    }

    public void Extinguish()
    {
        RemainingRounds = 0;
        if (visual != null) { visual.SetActive(false); Destroy(visual); }
        visual = null;
    }

    private void OnDead(object sender, Transform source) => Extinguish();
    private void OnDisable()
    {
        active.Remove(this);
        if (health != null) health.OnDead -= OnDead;
        Extinguish();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => active.Clear();
}
