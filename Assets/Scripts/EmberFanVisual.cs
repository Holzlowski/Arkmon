using System.Collections.Generic;
using UnityEngine;

// Flat, illustrated sparks fan out from the blade into three individual lanes.
public class EmberFanVisual : MonoBehaviour
{
    public const float TravelTime = .32f;
    private Vector3 origin;
    private Vector3[] destinations;
    private SpriteRenderer[] sparks;
    private FireVisualStyle style;
    private float elapsed;
    public static EmberFanVisual Play(Vector3 start,List<GridPosition> cells)
    {
        var effect=new GameObject("Glutfunke three-field fan").AddComponent<EmberFanVisual>();
        effect.origin=start;effect.destinations=cells.ConvertAll(c=>LevelGrid.Instance.GetWorldPosition(c)+Vector3.up*.5f).ToArray();
        return effect;
    }
    private void Start()
    {
        style=Resources.Load<FireVisualStyle>("Paper Fire");
        if(style==null || style.frames.Length==0){Destroy(gameObject);return;}
        sparks=new SpriteRenderer[destinations.Length*12];
        for(int i=0;i<sparks.Length;i++)
        {
            var root=new GameObject("Flying ember",typeof(SpriteRenderer));root.transform.SetParent(transform,false);
            sparks[i]=root.GetComponent<SpriteRenderer>();sparks[i].sprite=style.frames[i%style.frames.Length];
            sparks[i].sharedMaterial=style.material;sparks[i].sortingOrder=42;
        }
    }
    private void LateUpdate()
    {
        if(sparks==null)return;
        elapsed+=Time.deltaTime;
        Camera camera=Camera.main;
        for(int i=0;i<sparks.Length;i++)
        {
            float delay=(i%12)*.004f;
            float p=Mathf.Clamp01((elapsed-delay)/TravelTime);
            float spread=Mathf.Sin(p*Mathf.PI);
            Vector3 sideways=camera!=null?camera.transform.right:Vector3.right;
            sparks[i].transform.position=Vector3.Lerp(origin,destinations[i/12],p)
                +sideways*Mathf.Sin(i*2.4f)*spread*.38f+Vector3.up*spread*(.15f+(i%4)*.06f);
            sparks[i].sprite=style.frames[(Mathf.FloorToInt(elapsed*24)+i)%style.frames.Length];
            if(camera!=null)sparks[i].transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,Mathf.Sin(i)*35f);
            float size=.18f+(i%4)*.055f;
            sparks[i].transform.localScale=Vector3.one*size/sparks[i].sprite.bounds.size.y;
            sparks[i].color=new Color(1.3f,1f,.65f,p<1?1:0);
        }
        if(elapsed>=TravelTime+.06f)Destroy(gameObject);
    }
}
