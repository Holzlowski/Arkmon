using System.Collections.Generic;
using UnityEngine;

// Scene-owned facade palette. Roofs, wood, characters and interactive soil keep their own materials.
public sealed class CourtyardArchitecturePalette : MonoBehaviour
{
    readonly List<(MeshRenderer renderer,Material original,Material replacement)> bindings=new List<(MeshRenderer,Material,Material)>();
    Material stone,plaster;
    internal static void Install()
    {
        if(GameObject.Find("Courtyard coherent architecture")!=null)return;
        var originalStone=Resources.Load<Material>("CourtyardStyle/Stone");
        var originalPlaster=Resources.Load<Material>("CourtyardStyle/Plaster");
        if(originalStone==null || originalPlaster==null)return;
        var style=new GameObject("Courtyard coherent architecture").AddComponent<CourtyardArchitecturePalette>();
        style.stone=new Material(originalStone){name="Resident local limestone"};
        style.stone.SetFloat("_TextureScale",.58f);style.stone.SetFloat("_FaceAlignedMasonry",1);
        style.stone.SetColor("_BaseColor",new Color(.81f,.82f,.79f));
        style.plaster=new Material(originalPlaster){name="Resident warm limewash"};
        style.plaster.SetColor("_BaseColor",new Color(.84f,.88f,.92f));
        style.plaster.SetFloat("_Saturation",.93f);style.plaster.SetFloat("_AmbientFill",.48f);
        foreach(string name in new[]{"Ida workshop","Stable","Garden cottage","East cottage"})
        {
            var home=GameObject.Find(name);if(home==null)continue;
            foreach(var renderer in home.GetComponentsInChildren<MeshRenderer>(true))
            {
                var original=renderer.sharedMaterial;
                var replacement=original==originalStone?style.stone:original==originalPlaster?style.plaster:null;
                if(replacement==null)continue;
                style.bindings.Add((renderer,original,replacement));renderer.sharedMaterial=replacement;
            }
        }
    }
    void OnEnable(){foreach(var b in bindings)if(b.renderer!=null && b.renderer.sharedMaterial==b.original)b.renderer.sharedMaterial=b.replacement;}
    void OnDisable(){foreach(var b in bindings)if(b.renderer!=null && b.renderer.sharedMaterial==b.replacement)b.renderer.sharedMaterial=b.original;}
    void OnDestroy(){OnDisable();if(stone!=null)Destroy(stone);if(plaster!=null)Destroy(plaster);}
}
