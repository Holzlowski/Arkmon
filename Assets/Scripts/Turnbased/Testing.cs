using UnityEngine;

public class Testing : MonoBehaviour
{
    [SerializeField] private Unit unit;

    // Start is called before the first frame update
    void Start()
    {

    }

    private void Update()
    {
        if (ExplorationInput.Pressed(UnityEngine.InputSystem.Key.T))
        {

        }
    }

}
