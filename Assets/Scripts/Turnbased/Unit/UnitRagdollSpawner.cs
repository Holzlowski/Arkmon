using UnityEngine;

public class UnitRagdollSpawner : MonoBehaviour
{
    [SerializeField] private Transform ragdollPrefab;
    [SerializeField] private Transform originalRagdollRootBone;

    private HealthSystem healthSystem;

    private void OnEnable()
    {
        healthSystem = GetComponent<HealthSystem>();

        healthSystem.OnDead += HealthSystem_OnDead;
    }

    private void HealthSystem_OnDead(object sender, Transform e)
    {
        // Paper characters have no compatible skeleton. Never let an old visual
        // effect interrupt the Unit's death listener with a missing-bone error.
        if (!isActiveAndEnabled || GetComponent<PaperCharacterVisual>() != null
            || originalRagdollRootBone == null || ragdollPrefab == null
            || ragdollPrefab.GetComponent<UnitRagdoll>() == null) return;
        Transform ragdollTransform = Instantiate(ragdollPrefab, transform.position, transform.rotation);
        UnitRagdoll unitRagdoll = ragdollTransform.GetComponent<UnitRagdoll>();
        unitRagdoll.Setup(originalRagdollRootBone, e);
    }

    private void OnDisable()
    {
        if (healthSystem != null) healthSystem.OnDead -= HealthSystem_OnDead;
    }
}
