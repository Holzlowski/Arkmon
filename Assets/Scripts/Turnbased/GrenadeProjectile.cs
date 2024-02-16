using System;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    public static event EventHandler OnAnyGrenadeExploded;

    [SerializeField] private Transform grenadeExplodeVfxPrefab;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private AnimationCurve archYAnimationCurve;

    private Action onGrenadeBehavoiurComplete;
    private Vector3 targetPosition;
    private Vector3 positionXZ;
    private float moveSpeed = 15f;
    private float totalDistance;
    float reachedTargetDistance = .2f;
    float damageRadius = 4;

    private void Update()
    {
        Vector3 moveDirection = (targetPosition - positionXZ).normalized;

        positionXZ += moveDirection * moveSpeed * Time.deltaTime;

        float distance = Vector3.Distance(positionXZ, targetPosition);
        float distanceNormilazied = 1 - distance / totalDistance;

        float maxHeight = totalDistance / 4f;
        float positionY = archYAnimationCurve.Evaluate(distanceNormilazied) * maxHeight;
        transform.position = new Vector3(positionXZ.x, positionY, positionXZ.z);

        if (Vector3.Distance(positionXZ, targetPosition) < reachedTargetDistance)
        {
            Collider[] colliderArray = Physics.OverlapSphere(targetPosition, damageRadius);

            foreach (Collider collider in colliderArray)
            {
                if (collider.TryGetComponent<Unit>(out Unit targetUnit))
                {
                    targetUnit.Damage(30);
                }
                if (collider.TryGetComponent<DestructibleCrate>(out DestructibleCrate destructibleCrate))
                {
                    destructibleCrate.Damage();
                }
            }

            OnAnyGrenadeExploded?.Invoke(this, EventArgs.Empty);
            trailRenderer.transform.parent = null;
            Instantiate(grenadeExplodeVfxPrefab, targetPosition + Vector3.up * 1f, Quaternion.identity);
            Destroy(gameObject);

            onGrenadeBehavoiurComplete();
        }
    }
    public void Setup(GridPosition targetGridPosition, Action onGrenadeBehavoiurComplete)
    {
        this.onGrenadeBehavoiurComplete = onGrenadeBehavoiurComplete;
        targetPosition = LevelGrid.Instance.GetWorldPosition(targetGridPosition);

        positionXZ = transform.position;
        positionXZ.y = 0f;
        totalDistance = Vector3.Distance(positionXZ, targetPosition);
    }
}
