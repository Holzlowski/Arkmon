using System.Collections;
using Cinemachine;
using UnityEngine;

public class CourtyardArrivalPresentation : MonoBehaviour
{
    private static CourtyardArrivalPresentation current;
    public static bool IsPresenting => current != null && current.active;
    private bool active;
    private CinemachineFramingTransposer framing;
    private Vector3 originalFocus;
    private float originalDistance;
    public IEnumerator Reveal(Transform player, Transform john)
    {
        var camera=FindFirstObjectByType<CinemachineVirtualCamera>();
        framing=camera!=null?camera.GetCinemachineComponent<CinemachineFramingTransposer>():null;
        if(framing==null || player==null || john==null)yield break;
        current=this;active=true;originalFocus=framing.m_TrackedObjectOffset;originalDistance=framing.m_CameraDistance;
        Vector3 focus=john.position-player.position+Vector3.up*.8f;
        try
        {
            yield return Ease(originalFocus,focus,originalDistance,originalDistance*1.15f,.8f);
            yield return new WaitForSecondsRealtime(.6f);
            yield return Ease(focus,originalFocus,originalDistance*1.15f,originalDistance,.75f);
        }
        finally{Restore();}
    }
    private IEnumerator Ease(Vector3 from,Vector3 to,float near,float far,float duration)
    {
        for(float t=0;t<duration;t+=Time.unscaledDeltaTime)
        {float u=Mathf.SmoothStep(0,1,t/duration);framing.m_TrackedObjectOffset=Vector3.Lerp(from,to,u);framing.m_CameraDistance=Mathf.Lerp(near,far,u);yield return null;}
        framing.m_TrackedObjectOffset=to;framing.m_CameraDistance=far;
    }
    private void Restore()
    {if(active && framing!=null){framing.m_TrackedObjectOffset=originalFocus;framing.m_CameraDistance=originalDistance;}active=false;if(current==this)current=null;}
    private void OnDisable(){StopAllCoroutines();Restore();}
}
