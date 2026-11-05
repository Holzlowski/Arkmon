using System.Collections.Generic;
using UnityEngine;

// Dress the authored workshop without rebuilding the courtyard or its collision layout.
[DefaultExecutionOrder(9500)]
public sealed class CourtyardWorkshopDetails : MonoBehaviour
{
    readonly List<Transform> paperProps=new List<Transform>();
    internal static void Install()
    {
        var workshop=GameObject.Find("Ida workshop")?.transform;
        if(workshop==null || workshop.Find("Workshop details")!=null)return;
        var holder=new GameObject("Workshop details");holder.transform.SetParent(workshop,false);
        var details=holder.AddComponent<CourtyardWorkshopDetails>();
        var bench=workshop.Find("Workbench");
        foreach(string name in new[]{"Mallet handle","Mallet head","Pot on workbench"})
            if(bench!=null)bench.Find(name)?.gameObject.SetActive(false);
        workshop.Find("Workshop tool rack")?.gameObject.SetActive(false);
        workshop.Find("Workshop offcut bin")?.gameObject.SetActive(false);
        // Fit collision to the smaller illustrated stack instead of leaving an invisible long rack.
        var timber=workshop.Find("Workshop timber rack");
        if(timber!=null)
        {
            foreach(var renderer in timber.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            foreach(var collider in timber.GetComponentsInChildren<Collider>())collider.enabled=false;
            var body=timber.GetComponent<BoxCollider>();
            if(body!=null)
            {
                body.center=new Vector3(0,.29f,-.50f);body.size=new Vector3(1.02f,.58f,.64f);body.enabled=true;
            }
        }
        foreach(Transform child in workshop)
            if(child.name=="Curled wood shaving")child.gameObject.SetActive(false);
        var blank=workshop.Find("Ida planing blank");
        if(blank!=null)
        {
            blank.localPosition=new Vector3(-1.85f,.91f,-3.95f);
            blank.localScale=new Vector3(1.55f,.075f,.22f);
        }
        details.Prop("Illustrated hanging tools","Tools",new Vector3(-1.55f,.69f,-2.12f),false);
        details.Prop("Marta trowel repair","Repair",new Vector3(.10f,.91f,-3.55f),true);
        details.Prop("Carpenter tool caddy","Caddy",new Vector3(-.60f,.91f,-3.12f),true);
        var stack=details.Prop("Workshop spare timber","Planks",new Vector3(3.65f,.04f,-.60f),true);
        if(stack!=null)stack.localScale=Vector3.one*.85f;
        details.Prop("Workshop offcut basket","Offcuts",new Vector3(-3.65f,.02f,-2.15f),true);
        for(int i=0;i<2;i++)
        {
            var shavings=details.Prop("Illustrated wood shavings","Shavings",new Vector3(-1.75f+i*.42f,.045f,-4.72f-(i%2)*.30f),false);
            if(shavings!=null)
            {
                shavings.localRotation=Quaternion.Euler(90,0,i*37);
                shavings.localScale=Vector3.one*(1.15f+i*.12f);
            }
        }
        var point=new GameObject("Inspect Ida workpiece",typeof(SphereCollider));
        point.transform.SetParent(workshop,false);point.transform.localPosition=new Vector3(.1f,.05f,-4.8f);
        var trigger=point.GetComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.4f;
        point.AddComponent<CourtyardWorkshopWorkpiece>();
        CourtyardWorkshopStyle.Apply(workshop);
    }
    Transform Prop(string name,string resource,Vector3 local,bool billboard)
    {
        var sprite=Resources.Load<Sprite>("WorkshopDetails/"+resource);if(sprite==null)return null;
        var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(transform,false);
        obj.transform.localPosition=local;obj.GetComponent<SpriteRenderer>().sprite=sprite;
        if(billboard)paperProps.Add(obj.transform);
        return obj.transform;
    }
    void LateUpdate()
    {
        var camera=Camera.main;if(camera==null)return;
        foreach(var prop in paperProps)prop.rotation=camera.transform.rotation;
    }
}
