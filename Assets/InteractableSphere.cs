using UnityEngine;

public class InteractableSphere : MonoBehaviour, IInteractableArk
{
    [SerializeField] Material blue;
    [SerializeField] Material red;
    [SerializeField] MeshRenderer meshRenderer;
    private bool isBlue = true;

    public void InteractWithPlayer()
    {
        if (isBlue)
        {
            SetColorRed();
        }
        else
        {
            SetColorBlue();
        }
    }

    Vector3 IInteractableArk.GetTransform()
    {
        return transform.position;
    }

    private void SetColorBlue()
    {
        isBlue = true;
        meshRenderer.material = blue;
    }

    private void SetColorRed()
    {
        isBlue = false;
        meshRenderer.material = red;
    }
}
