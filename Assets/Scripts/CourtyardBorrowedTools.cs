using UnityEngine;

// Each borrowed tool stays near its owner and the shared well. No tool, no boundary.
public sealed class CourtyardBorrowedTools : MonoBehaviour
{
    bool warned;
    int lastTool;
    static int Tool => CourtyardGarden.HasCan ? 1 : CourtyardWaterTask.HasBucket ? 2 : 0;
    // Garden beds/side path plus the well and a little space beyond it.
    static Rect Area(int tool) => tool == 1 ? Rect.MinMaxRect(7,-17.5f,35,-2)
        : Rect.MinMaxRect(-17,-12.5f,13,6.5f);
    public static bool InsideArea(Vector3 point)
    {
        int tool=Tool;if(tool==0)return true;
        var a=Area(tool);return point.x>=a.xMin && point.x<=a.xMax && point.z>=a.yMin && point.z<=a.yMax;
    }
    static float Limit(float position,float velocity,float min,float max,float dt)
    {
        // A displaced player can always return; never snap a position back across obstacles.
        if(position<min)return Mathf.Max(0,velocity);
        if(position>max)return Mathf.Min(0,velocity);
        return Mathf.Clamp(velocity,(min-position)/dt,(max-position)/dt);
    }
    public static Vector3 ConstrainMovement(Vector3 position,Vector3 movement,float dt)
    {
        int tool=Tool;if(tool==0 || dt<=0)return movement;
        var a=Area(tool);
        movement.x=Limit(position.x,movement.x,a.xMin,a.xMax,dt);
        movement.z=Limit(position.z,movement.z,a.yMin,a.yMax,dt);
        return movement;
    }
    void Update()
    {
        int tool=Tool;
        if(tool!=lastTool){warned=false;lastTool=tool;}
        if(tool==0)return;
        var p=transform.position;var a=Area(tool);
        float margin=Mathf.Min(p.x-a.xMin,a.xMax-p.x,p.z-a.yMin,a.yMax-p.z);
        if(margin>1.8f){warned=false;return;}
        if(margin>.65f || warned || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning)return;
        var dialogue=DialogueBoxController.instance;if(dialogue==null || dialogue.IsOpen)return;
        warned=true;
        dialogue.ShowMessage(tool==1
            ? CourtyardGarden.Completed ? "Elian, stell die Gießkanne bitte wieder an ihren Platz im Garten."
                : "Bleib mit der Kanne bitte zwischen Garten und Brunnen, Elian. Die Beete warten noch auf ihr Wasser."
            : CourtyardWaterTask.Completed ? "Stell den Eimer bitte wieder neben die Tränke, Elian."
                : "Elian, bleib mit dem Eimer bitte zwischen Stall und Brunnen. Die Pferde warten auf ihr Wasser.",tool==1?"Marta":"Hannes");
    }
}
