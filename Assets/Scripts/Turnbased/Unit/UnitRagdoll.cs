using UnityEngine;

public class UnitRagdoll : MonoBehaviour
{
    [SerializeField] private Transform ragdollRootBone;

    public void Setup(Transform originalRootBone, Transform damageSourceTransform = null)
    {
        MatchAllChildTransforms(originalRootBone, ragdollRootBone);

        if (damageSourceTransform == null)
        {
            ApplyExplosionToRagdoll(ragdollRootBone, 300f, transform.position, 10f);
        }
        else
        {
            var dmgDirectionOffest = (damageSourceTransform.position - transform.position).normalized;
            var dmgDirection = transform.position + dmgDirectionOffest;
            ApplyExplosionToRagdoll(ragdollRootBone, 500f, dmgDirection, 10f);
        }

    }


    private void MatchAllChildTransforms(Transform root, Transform clone)
    {
        foreach (Transform child in root)
        {
            Transform cloneChild = clone.Find(child.name);
            if (cloneChild != null)
            {
                cloneChild.position = cloneChild.position;
                cloneChild.rotation = cloneChild.rotation;

                MatchAllChildTransforms(child, cloneChild);
            }
        }
    }

    private void ApplyExplosionToRagdoll(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRadius)
    {
        foreach (Transform child in root)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                childRigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
            }

            ApplyExplosionToRagdoll(child, explosionForce, explosionPosition, explosionRadius);
        }
    }
}
