using UnityEngine;

// A brief, restrained response in the existing parchment frame.
public sealed class DialoguePortraitMotion : MonoBehaviour
{
    private float elapsed=1;
    private float emphasis;
    public void Play(DialogueEmotion emotion)
    {
        emphasis=emotion==DialogueEmotion.Surprised?.055f:emotion==DialogueEmotion.Friendly?.025f:.012f;
        elapsed=0;
    }
    private void Update()
    {
        elapsed+=Time.unscaledDeltaTime;
        float wave=Mathf.Sin(Mathf.Clamp01(elapsed/.28f)*Mathf.PI);
        transform.localScale=Vector3.one*(1+wave*emphasis);
    }
    private void OnDisable(){elapsed=1;transform.localScale=Vector3.one;}
}