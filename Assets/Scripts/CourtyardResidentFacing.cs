using UnityEngine;

public sealed class CourtyardResidentFacing : MonoBehaviour
{
    private void LateUpdate()
    {
        var view = Camera.main;
        if (view == null) return;
        var direction = view.transform.forward;
        direction.y = 0;
        if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction);
    }
}
