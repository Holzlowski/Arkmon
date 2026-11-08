using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Final visual pass after layout/material assignment. Original colliders and interaction anchors are retained.
public sealed class CourtyardBuildingCraft : MonoBehaviour
{
    readonly List<(MeshFilter filter,Mesh original,Mesh replacement)> bevels=new List<(MeshFilter,Mesh,Mesh)>();
    readonly List<GameObject> overlays=new List<GameObject>();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly Dictionary<(Material,CourtyardCraftMesh.Finish),Material> paints=new Dictionary<(Material,CourtyardCraftMesh.Finish),Material>();
    readonly List<(Transform part,Vector3 position,Quaternion rotation)> ridges=new List<(Transform,Vector3,Quaternion)>();
    Material pegPaint;
    Mesh pegMesh;
    static readonly HashSet<string> castleRoots=new HashSet<string>{"Player residence tower","Castle west wing","Aurenfeld upper residence",
        "Tower connecting wing","Castle hall","Castle upper keep","Castle keep west tower","Castle keep east tower","Aurenfeld raised approach"};
    static readonly HashSet<string> homeRoots=new HashSet<string>{"Garden cottage","East cottage","Ida workshop","Stable"};
    internal static void Install()
    {
        if(GameObject.Find("Courtyard crafted buildings")!=null)return;
        var style=new GameObject("Courtyard crafted buildings").AddComponent<CourtyardBuildingCraft>();
        style.Build();
    }
    static Transform Root(Transform part,HashSet<string> names)
    {for(var t=part;t!=null;t=t.parent)if(names.Contains(t.name))return t;return null;}
    static Vector3 Scale(Transform t){var s=t.lossyScale;return new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));}
    void Build()
    {
        var sources=FindObjectsByType<MeshFilter>(FindObjectsSortMode.InstanceID);

        foreach(var source in sources)
        {
            var renderer=source.GetComponent<MeshRenderer>();var mesh=source.sharedMesh;
            if(renderer==null || !renderer.enabled || mesh==null || renderer.sharedMaterial==null || !source.gameObject.activeInHierarchy)continue;
            var material=renderer.sharedMaterial;string n=source.name.ToLowerInvariant();
            int seed=StableSeed(source.transform);
            var castle=Root(source.transform,castleRoots);var home=Root(source.transform,homeRoots);
            bool illustrated=material.shader.name=="Arkmon/Courtyard Illustrated Surface";
            bool roof=(castle!=null || home!=null) && (n.Contains("roof") || n.Contains("tiled canopy") || n.Contains("shingled canopy")) && !n.Contains("gables") && !n.Contains("backing") && !n.Contains("ridge") && !n.Contains("eaves");
            bool paving=n=="raised tower terrace" || n=="residence terrace extension" || n=="tower stair tread"
                || n.Contains("approach stone") || n.Contains("flagstone") || n=="castle worn apron stone" || n.Contains("stair tread");
            bool masonry=illustrated && (castle!=null || home!=null) && (n.EndsWith(" structure") && material.GetVector("_Tile").z>.4f && material.GetVector("_Tile").w<.1f
                || n=="hall battlement" || n=="stone foundation course" || n=="octagonal tower" || n=="dormer stone face" || n.Contains("stone footing") || n.Contains("chimney") && !n.Contains("cap"));
            bool wood=(material.HasProperty("_Wood") && material.GetFloat("_Wood")>.5f)
                || material.name.ToLowerInvariant().Contains("oak") || material.name.ToLowerInvariant().Contains("timber");
            bool trim=illustrated && (castle!=null || home!=null || n.StartsWith("portal "))
                && (n.Contains("jamb") || n.Contains("sill") || n.Contains("corner") || n.Contains("cornice") || n.Contains("lintel") || n.Contains("cap") || n.Contains("plaster") || n.Contains("limewash"));
            if(roof)Overlay(source,renderer,CourtyardCraftMesh.Finish.Roof,seed++,null);
            else if(paving && illustrated)Overlay(source,renderer,CourtyardCraftMesh.Finish.Paving,seed++,null);
            else if(masonry)
            {
                // Glazing and door recesses keep their existing visible depth and clear silhouettes.
                var apertures=(castle!=null?castle:home).GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r=>r.name=="Arched dark glazing" || r.name=="Deep castle window" || r.name.Contains("door recess"))
                    .Select(r=>{var b=r.bounds;b.Expand(new Vector3(.04f,.04f,.20f));return b;}).ToArray();
                var scale=Scale(source.transform);
                Overlay(source,renderer,CourtyardCraftMesh.Finish.Masonry,seed++,p=>
                {
                    var world=source.transform.TransformPoint(new Vector3(p.x/scale.x,p.y/scale.y,p.z/scale.z));
                    return apertures.Any(b=>b.Contains(world));
                });
            }
            if((home!=null || castle!=null) && n.Contains("ridge cap") && source.GetComponent<Collider>()==null)
            {
                var part=source.transform;ridges.Add((part,part.localPosition,part.localRotation));
                var random=new System.Random(seed);
                part.localPosition+=Vector3.up*((float)random.NextDouble()*.018f-.009f);
                part.localRotation*=Quaternion.Euler(0,0,(float)random.NextDouble()*1.3f-.65f);
            }
            bool iron=n.Contains("iron strap") || n.Contains("strap rivet") || n.Contains("shoe bolt");
            if(mesh.name=="Cube" && wood && home!=null && n.Contains("post") && !n.Contains("foot") && Scale(source.transform).y>.7f)Pegs(source);
            if(mesh.name=="Cube" && (wood || trim || paving || iron || n.Contains("ridge cap")))
            {
                // A bevel stays inside the existing bounds: feet and interaction reach remain reliable.
                var softened=CourtyardCraftMesh.BevelBox(mesh,Scale(source.transform),iron?.006f:wood?.018f:.027f);
                meshes.Add(softened);bevels.Add((source,mesh,softened));source.sharedMesh=softened;
            }
        }
    }
    static int StableSeed(Transform part)
    {
        unchecked
        {
            int seed=4831;
            for(var t=part;t!=null;t=t.parent)foreach(char c in t.name)seed=seed*31+c;
            var p=part.position;seed=seed*31+Mathf.RoundToInt(p.x*100);seed=seed*31+Mathf.RoundToInt(p.y*100);seed=seed*31+Mathf.RoundToInt(p.z*100);
            return seed;
        }
    }
    void Pegs(MeshFilter source)
    {
        if(pegPaint==null)
        {
            pegPaint=new Material(Resources.Load<Material>("CourtyardStyle/Oak")){name="Dark end-grain joinery pegs"};
            pegPaint.SetColor("_BaseColor",new Color(.35f,.26f,.16f));
            pegMesh=CourtyardCraftMesh.BevelBox(source.sharedMesh,Vector3.one,.16f);meshes.Add(pegMesh);
        }
        var scale=Scale(source.transform);
        foreach(float height in new[]{-.30f,.30f})
        {
            var obj=new GameObject("Craft timber peg",typeof(MeshFilter),typeof(MeshRenderer));
            obj.transform.SetParent(source.transform,false);obj.transform.localPosition=new Vector3(0,height,-.5f-.012f/scale.z);
            obj.transform.localScale=new Vector3(.055f/scale.x,.055f/scale.y,.025f/scale.z);
            obj.GetComponent<MeshFilter>().sharedMesh=pegMesh;obj.GetComponent<MeshRenderer>().sharedMaterial=pegPaint;overlays.Add(obj);
        }
    }
    void Overlay(MeshFilter source,MeshRenderer renderer,CourtyardCraftMesh.Finish finish,int seed,Func<Vector3,bool> reserved)
    {
        var original=renderer.sharedMaterial;bool slate=original.shader.name!="Arkmon/Courtyard Illustrated Surface";
        var key=(original,finish);
        if(!paints.TryGetValue(key,out var paint))
        {
            var baseMaterial=slate?Resources.Load<Material>("CourtyardStyle/Plaster"):finish==CourtyardCraftMesh.Finish.Paving?Resources.Load<Material>("CourtyardStyle/Stone"):original;
            paint=new Material(baseMaterial){name="Crafted "+original.name};
            if(slate)paint.SetColor("_BaseColor",new Color(.20f,.38f,.85f));
            paint.SetFloat("_SingleStone",1);paint.SetFloat("_StoneMeshUV",1);paint.SetFloat("_EdgeWidth",0);
            paints.Add(key,paint);
        }
        Vector4 patch=new Vector4(.16f,.065f,.63f,.405f);
        if(finish==CourtyardCraftMesh.Finish.Roof)
        {
            var tile=paint.GetVector("_Tile");
            patch=slate?new Vector4(.16f,.10f,.63f,.70f):tile.z>.4f && tile.w>.4f?
                new Vector4(.065f,.045f,.675f,.825f):new Vector4(.10f,.05f,.16f,.34f);
        }
        var mesh=CourtyardCraftMesh.Surface(source.sharedMesh,Scale(source.transform),finish,patch,seed,reserved);
        if(mesh.vertexCount==0){Destroy(mesh);return;}
        meshes.Add(mesh);
        var obj=new GameObject("Craft detail "+finish,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(source.transform,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;
        var art=obj.GetComponent<MeshRenderer>();art.sharedMaterial=paint;
        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
        // Per-object overrides belong to the source; switch only the surface sampling.
        block.SetFloat("_SingleStone",1);block.SetFloat("_StoneMeshUV",1);block.SetFloat("_EdgeWidth",0);
        block.SetVector("_StoneWallJoin",Vector4.zero);
        if(slate)block.SetColor("_BaseColor",new Color(.20f,.38f,.85f));
        if(finish==CourtyardCraftMesh.Finish.Masonry)block.SetVector("_FootMoss",new Vector4(0,.55f,.40f,seed*.3f));
        art.SetPropertyBlock(block);overlays.Add(obj);
    }
    void OnDestroy()
    {
        foreach(var r in ridges)if(r.part!=null){r.part.localPosition=r.position;r.part.localRotation=r.rotation;}
        foreach(var b in bevels)if(b.filter!=null && b.filter.sharedMesh==b.replacement)b.filter.sharedMesh=b.original;
        foreach(var obj in overlays)if(obj!=null)Destroy(obj);
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        foreach(var paint in paints.Values)if(paint!=null)Destroy(paint);
        if(pegPaint!=null)Destroy(pegPaint);
    }
}
