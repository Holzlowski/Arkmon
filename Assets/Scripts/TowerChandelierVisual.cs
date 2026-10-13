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
