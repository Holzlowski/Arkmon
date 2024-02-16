using UnityEngine;

//because of Coil Whine
public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] private int frameRate = 60;
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = frameRate;
    }
}
