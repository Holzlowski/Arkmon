using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer m_Renderer;

    public void Show(Material material)
    {
        m_Renderer.enabled = true;
        m_Renderer.material = material;
    }

    public void Hide()
    {
        m_Renderer.enabled = false;
    }
}
