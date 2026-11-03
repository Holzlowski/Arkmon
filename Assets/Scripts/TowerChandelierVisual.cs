using UnityEngine;

// Only the little paper flames and one local light flicker; the fixture stays fixed.
public class TowerChandelierVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] flames;
    [SerializeField] private Light candleLight;
    private FireVisualStyle style;
    private float baseIntensity;
    private void Awake()
    {
        style=Resources.Load<FireVisualStyle>("Paper Fire");
        if(candleLight!=null)baseIntensity=candleLight.intensity;
    }
    private void Start()
    {
        // The same flame component is used on other candles; only this fixture emits the loop.
        if (gameObject.name != "Tower candle chandelier") return;
        var clip = Resources.Load<AudioClip>("Audio/IntroFoley/chandelier-crackle");
        if (clip == null) return;
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 4f;
        source.maxDistance = 20f;
        source.dopplerLevel = 0;
        source.volume = .16f;
        source.Play();
    }
    private void LateUpdate()
    {
        float time=Time.time;
        if(candleLight!=null)candleLight.intensity=baseIntensity*(1f+.035f*Mathf.Sin(time*3.1f)+.02f*Mathf.Sin(time*7.3f));
        if(style==null || style.frames==null || style.frames.Length==0)return;
        for(int i=0;i<flames.Length;i++)
        {
            SpriteRenderer flame=flames[i];if(flame==null)continue;
            flame.sprite=style.frames[(Mathf.FloorToInt(time*9)+i)%style.frames.Length];
            if(Camera.main!=null)flame.transform.rotation=Camera.main.transform.rotation;
            float height=.13f*(1f+.07f*Mathf.Sin(time*6+i*1.9f));
            flame.transform.localScale=Vector3.one*height/flame.sprite.bounds.size.y;
        }
    }
}
