using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PaperHitFeedback : MonoBehaviour
{
    private HealthSystem health;
    private PaperCharacterVisual visual;
    private int previousHealth;
    private void OnEnable()
    {
        health = GetComponent<HealthSystem>(); visual = GetComponent<PaperCharacterVisual>();
        if (health == null) return;
        previousHealth = health.Health; health.OnDamage += OnDamage;
    }
    private void OnDamage(object sender, EventArgs args)
    {
        int damage = Mathf.Max(0, previousHealth - health.Health); previousHealth = health.Health;
        if (damage == 0) return;
        if (visual != null) visual.PlayHitReaction();
        Vector3 position = visual != null ? visual.GetRecallOrigin() + Vector3.up * 1.1f : transform.position + Vector3.up * 1.5f;
        if (Camera.main != null) position -= Camera.main.transform.forward * .25f;
        PaperDamageNumber.Show(position, damage);
    }
    private void OnDisable() { if (health != null) health.OnDamage -= OnDamage; }
}
