using System.Collections.Generic;
using UnityEngine;

// Dress and arrange only the stable; preserve the authored courtyard and garden work.
[DefaultExecutionOrder(9500)]
public sealed class CourtyardStableCare : MonoBehaviour
{
    readonly List<Transform> paperProps=new List<Transform>();
    internal static void Install()
    {
        var stable=GameObject.Find("Stable")?.transform;
        if(stable==null || stable.Find("Stable care details")!=null)return;
        var holder=new GameObject("Stable care details");holder.transform.SetParent(stable,false);
        var care=holder.AddComponent<CourtyardStableCare>();
        var workSpot=stable.Find("Hannes work spot");
        if(workSpot!=null)workSpot.localPosition=new Vector3(.7f,0,-4.0f);
        var dun=stable.Find("Dun horse");
        if(dun!=null)
        {
            dun.localPosition=new Vector3(2.05f,.005f,-3.85f);
            var body=dun.gameObject.AddComponent<BoxCollider>();
            body.center=new Vector3(0,1.05f,0);body.size=new Vector3(1.8f,2.1f,.75f);
        }
        var rail=stable.Find("Stable tack rail");
        if(rail!=null)rail.localPosition=new Vector3(-4.1f,0,-1.7f);
        care.Prop("Hanging stable bridle","Bridle",new Vector3(3.38f,1.05f,-2.08f),false);
        care.Prop("Stable grooming caddy","Caddy",new Vector3(4.05f,.02f,-2.25f),true);
        care.Prop("Stable water bucket","Bucket",new Vector3(4.7f,.02f,-.9f),true);
        care.Prop("Spare golden hay","Hay",new Vector3(-2.3f,.02f,-2.65f),true);
        foreach(var horse in stable.GetComponentsInChildren<CourtyardHorse>())
        {
            var at=horse.IsFeeding?new Vector3(-1.05f,.05f,-3.15f):new Vector3(3.25f,.05f,-4.7f);
            var point=new GameObject(horse.IsFeeding?"Greet chestnut horse":"Greet dun horse",typeof(SphereCollider));
            point.transform.SetParent(stable,false);point.transform.localPosition=at;
            var trigger=point.GetComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.4f;
            point.AddComponent<CourtyardHorseGreeting>().Configure(horse);
        }
        CourtyardStableStyle.Install(stable);
    }
    void Prop(string name,string resource,Vector3 local,bool billboard)
    {
        var sprite=Resources.Load<Sprite>("StableCare/"+resource);if(sprite==null)return;
        var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
        obj.transform.localPosition=local;obj.GetComponent<SpriteRenderer>().sprite=sprite;
        if(billboard)paperProps.Add(obj.transform);
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var prop in paperProps)prop.rotation=camera.transform.rotation;
    }
}
