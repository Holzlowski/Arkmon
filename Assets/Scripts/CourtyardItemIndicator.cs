using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-owned item feedback for the borrowed courtyard tools. It never poses or attaches to Elian.
public sealed class CourtyardItemIndicator : MonoBehaviour
{
    GameObject panel;
    TextMeshProUGUI label,activity;
    Image icon;
    Sprite bucketIcon, canIcon;
    Material iconMaterial;
    public bool IsVisible => panel!=null && panel.activeSelf;
    public string ItemText => label!=null?label.text:null;
    void Awake()
    {
        var root=new GameObject("Courtyard carried item",typeof(Canvas));root.transform.SetParent(transform,false);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=22;
        PaperUIFactory.Scale(canvas);var style=PaperUIStyle.Load();
        var paper=PaperUIFactory.Image("Courtyard item card",root.transform,style.panel,Vector2.one,Vector2.one,new Vector2(-312,-122),new Vector2(-24,-24));
        panel=paper.gameObject;
        bucketIcon=Resources.Load<Sprite>("StableCare/Bucket");
        canIcon=Resources.Load<Sprite>("Garden/Can");
        icon=PaperUIFactory.Image("Courtyard item icon",paper.transform,bucketIcon,Vector2.zero,Vector2.zero,new Vector2(15,12),new Vector2(72,85),false);
        icon.preserveAspect=true;
        iconMaterial=new Material(Resources.Load<Shader>("UIBucketContents"));
        PaperUIFactory.Text("Item heading",paper.transform,"Bei dir",15,new Vector2(0,1),Vector2.one,new Vector2(84,-33),new Vector2(-12,-14));
        label=PaperUIFactory.Text("Courtyard item state",paper.transform,"",23,Vector2.zero,Vector2.one,new Vector2(84,29),new Vector2(-12,-29));
        activity=PaperUIFactory.Text("Courtyard item activity",paper.transform,"",14,Vector2.zero,new Vector2(1,0),new Vector2(84,10),new Vector2(-12,29));
        Refresh();
    }
    void Update()=>Refresh();
    void Refresh()
    {
        bool hasCan=CourtyardGarden.HasCan;
        bool hasBucket=CourtyardWaterTask.HasBucket;
        panel.SetActive(hasCan || hasBucket);
        if(hasCan)
        {
            icon.sprite=canIcon;
            icon.material=null;
            int water=CourtyardGarden.WaterCharges;
            label.text=water==0?"Leere Gießkanne":"Gießkanne";
            activity.text=water switch
            {
                0 when CourtyardWaterTask.IsBusy => "Wasser schöpfen",
                0 when CourtyardGarden.Completed => "Zum Gartenweg zurück",
                0 => "Am Brunnen füllen",
                1 => "Wasser für 1 Beet",
                _ => $"Wasser für {water} Beete"
            };
        }
        else if(hasBucket)
        {
            icon.sprite=bucketIcon;
            icon.material=iconMaterial;
            label.text=CourtyardWaterTask.CarriedItemText;
            activity.text=CourtyardWaterTask.ItemActivityText;
            iconMaterial.SetFloat("_Filled",CourtyardWaterTask.BucketFull?1:0);
        }
    }
    void OnDestroy(){if(iconMaterial!=null)Destroy(iconMaterial);}
}