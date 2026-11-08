using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Corrections from the November courtyard playtest, scoped to the introduction scene.
public sealed class CourtyardFeedbackPolish : MonoBehaviour
{
    readonly List<Material> materials=new List<Material>();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Transform> hedges=new List<Transform>();
    internal static void Install()
    {
        if(GameObject.Find("Courtyard playtest corrections")!=null)return;
        var style=new GameObject("Courtyard playtest corrections").AddComponent<CourtyardFeedbackPolish>();
        style.Facade();style.Terrace();style.SandyPaths();style.Bank();style.Hedgerows();style.TrimOuterGround();
    }
    void Facade()
    {
        GameObject.Find("West cottage")?.SetActive(false);
        foreach(var detail in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if(detail.name.StartsWith("Cottage ") && detail.position.x > -6.5f && detail.position.x < -.8f && detail.position.z > 12.5f && detail.position.z < 18.5f)
                detail.gameObject.SetActive(false);
        foreach(var road in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if(road.name=="Curved courtyard path" && road.bounds.min.x>0 && road.bounds.max.x<6 && road.bounds.max.z>6)road.enabled=false;
        var windows=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Deep castle window").ToArray();
        foreach(var leaf in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Painted ivy leaf"))
        {
            // Leaves may climb masonry, but not windows or their projecting surrounds.
            bool opening=windows.Any(w=>Mathf.Abs(w.transform.position.z-leaf.transform.position.z)<.85f
                && Mathf.Abs(w.transform.position.x-leaf.transform.position.x)<.85f
                && Mathf.Abs(w.transform.position.y-leaf.transform.position.y)<1.15f);
            if(opening || leaf.transform.position.y>3.8f || leaf.transform.position.z>10 && leaf.transform.position.y>1.25f)leaf.enabled=false;
        }
        var front=windows.Where(w=>Mathf.Abs(w.transform.position.z-13.05f)<.8f)
            .Select(w=>w.transform.position.x).Distinct().OrderBy(x=>x).ToArray();
        foreach(var banner in FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Aurenfeld green banner"))
        {
            var at=banner.position;float x=at.x,best=float.MaxValue;
            for(int i=1;i<front.Length;i++)
            {
                if(front[i]-front[i-1]<2)continue;
                float candidate=(front[i]+front[i-1])*.5f;
                if(Mathf.Abs(candidate-at.x)<best){x=candidate;best=Mathf.Abs(candidate-at.x);}
            }
            var crest=FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Aurenfeld banner crest" && Mathf.Abs(t.position.x-at.x)<.1f);
            banner.position=new Vector3(x,at.y,at.z);var scale=banner.localScale;scale.x=.76f;banner.localScale=scale;
            banner.GetComponent<CourtyardSceneryMotion>()?.Rebase();
            if(crest!=null){var pos=crest.position;pos.x=x;crest.position=pos;crest.localScale=new Vector3(.58f,.68f,1);crest.GetComponent<CourtyardSceneryMotion>()?.Rebase();}
        }
    }
    void Terrace()
    {
        var source=GameObject.Find("Terrace coping")?.GetComponent<MeshRenderer>();if(source==null)return;
        // Return the east railing towards the residence, closing the formerly raw end.
        Block("Terrace east return coping",new Vector3(-6,1.43f,10.05f),new Vector3(.55f,.16f,4.4f),source.sharedMaterial,false);
        Block("Terrace east return handrail",new Vector3(-6,2.25f,10.05f),new Vector3(.30f,.15f,4.4f),source.sharedMaterial,true);
        for(int i=0;i<=7;i++)Block("Terrace east return baluster",new Vector3(-6,1.86f,7.94f+i*.60f),new Vector3(.20f,.7f,.17f),source.sharedMaterial,true);
        Block("Terrace corner finial",new Vector3(-6,2.4f,7.94f),new Vector3(.45f,.22f,.45f),source.sharedMaterial,false);
    }
    void Block(string name,Vector3 at,Vector3 size,Material mat,bool solid)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);
        go.transform.position=at;go.transform.localScale=size;
        var c=go.GetComponent<Collider>();c.enabled=solid;if(!solid)Destroy(c);
        var r=go.GetComponent<Renderer>();r.sharedMaterial=mat;
        var b=new MaterialPropertyBlock();b.SetVector("_ObjectScale",size);b.SetFloat("_EdgeWidth",.016f);r.SetPropertyBlock(b);
    }
    void SandyPaths()
    {
        var replacements=new Dictionary<Material,Material>();
        foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            string n=r.name.ToLowerInvariant();var original=r.sharedMaterial;
            if(original==null || !(n.Contains("path") || n.Contains("footpath") || n.Contains("worn") || n.Contains("trail")))continue;
            if(!original.HasProperty("_BaseColor"))continue;
            // Stone stepping slabs and contact shadows keep their existing materials.
            if(original.shader.name!="Arkmon/Courtyard Painted Ground Patch" && original.name!="Courtyard warm earth")continue;
            if(!replacements.TryGetValue(original,out var sand))
            {
                sand=new Material(original){name=original.name+" sandy finish"};materials.Add(sand);replacements.Add(original,sand);
                var color=original.GetColor("_BaseColor");
                sand.SetColor("_BaseColor",new Color(.96f,.89f,.74f,color.a));
                if(sand.HasProperty("_SandBlend"))sand.SetFloat("_SandBlend",.58f);
            }
            r.sharedMaterial=sand;
        }
    }
    void Bank()
    {
        var bank=GameObject.Find("Far stream bank");var horizon=GameObject.Find("Castle horizon and golden fields");
        var original=horizon?.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.sharedMaterial!=null && r.sharedMaterial.shader.name=="Arkmon/Meadow");
        if(bank==null || original==null)return;
        // The bank and surrounding fields share vertex colours and the same lighting shader.
        var filter=bank.GetComponent<MeshFilter>();var mesh=Instantiate(filter.sharedMesh);mesh.name="Golden bank matching field soil";
        var vertices=mesh.vertices;mesh.colors=vertices.Select(v=>CourtyardFieldMargins.GoldenGround(bank.transform.TransformPoint(v))).ToArray();
        meshes.Add(mesh);filter.sharedMesh=mesh;
        var mat=new Material(original.sharedMaterial){name="Golden bank continuous field palette"};mat.SetVector("_ClearArea",Vector4.zero);materials.Add(mat);
        bank.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    void Hedgerows()
    {
        var horizon=GameObject.Find("Castle horizon and golden fields");var meadow=horizon?.GetComponentInChildren<MeadowVegetation>();
        var source=horizon?.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.sprite!=null && (r.name=="Meadow thistles" || r.transform.parent?.name=="Meadow thistles"));
        if(meadow==null || source==null)return;
        Vector3[] groups={new Vector3(-82,0,-19),new Vector3(-103,0,52),new Vector3(-145,0,-20),new Vector3(-174,0,69)};
        for(int group=0;group<groups.Length;group++)for(int i=0;i<7;i++)
        {
            var p=groups[group]+new Vector3((i-3)*2.1f,0,Mathf.Sin(i*.9f+group)*1.3f);
            if(CourtyardFieldMargins.IsBarePath(p) || CourtyardFarmApproach.IsBareGround(p))continue;
            var go=new GameObject("Aurenfeld field-edge green hedge",typeof(SpriteRenderer));go.transform.SetParent(transform,false);
            var r=go.GetComponent<SpriteRenderer>();r.sprite=source.sprite;r.color=new Color(.68f,.79f,.44f);r.flipX=i%2==0;
            float scale=(2.5f+.45f*Mathf.Sin(i*2.1f+group))/r.sprite.bounds.size.y;
            go.transform.localScale=Vector3.one*scale;
            go.transform.position=CourtyardDistantFields.Surface(meadow,p)-Vector3.up*(r.sprite.bounds.min.y*scale-.02f);
            hedges.Add(go.transform);
        }
    }
    void TrimOuterGround()
    {
        // Keep the physical foundation; only its oversized green render mesh stops at the walls.
        var ground=GameObject.Find("Courtyard walkable ground")?.GetComponent<MeshFilter>();
        if(ground!=null)
        {
            var mesh=Instantiate(ground.sharedMesh);var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                var p=ground.transform.TransformPoint(vertices[i]);p.x=Mathf.Clamp(p.x,-43,43);p.z=Mathf.Clamp(p.z,-17,19);
                vertices[i]=ground.transform.InverseTransformPoint(p);
            }
            mesh.name="Courtyard foundation inside walls";mesh.vertices=vertices;mesh.RecalculateBounds();meshes.Add(mesh);ground.sharedMesh=mesh;
        }
        foreach(string name in new[]{"Meadow beyond the west breach","Meadow behind the east wall"})
        {var r=GameObject.Find(name)?.GetComponent<MeshRenderer>();if(r!=null)r.enabled=false;}
    }
    void LateUpdate(){var view=Camera.main;if(view!=null)foreach(var hedge in hedges)hedge.rotation=view.transform.rotation;}
    void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);foreach(var m in meshes)if(m!=null)Destroy(m);}
}
