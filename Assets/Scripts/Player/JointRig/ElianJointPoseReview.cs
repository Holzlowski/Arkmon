using UnityEngine;
using UnityEngine.InputSystem;

// Static review only. Selecting a pose never blends or advances a timeline.
[ExecuteAlways]
public sealed class ElianJointPoseReview : MonoBehaviour
{
    public Camera reviewCamera;
    public ElianJointRig[] figures;
    public int selected=-1;
    public bool mirrored;
    public void Select(int index)
    {
        selected=index;
        for(int i=0;i<figures.Length;i++)
        {
            figures[i].gameObject.SetActive(index<0||i==index);
            figures[i].transform.localRotation=Quaternion.Euler(0,mirrored?180:0,0);
            figures[i].ApplyPose(figures[i].pose);
        }
        reviewCamera.orthographicSize=index<0?2.05f:1.35f;
        reviewCamera.transform.position=new Vector3(index<0?0:figures[index].transform.position.x,1.1f,-10);
    }
    void OnEnable(){if(reviewCamera!=null&&figures!=null&&figures.Length==3)Select(selected);}
    void Update()
    {
        if(!Application.isPlaying)return;
        var keys=Keyboard.current;if(keys==null)return;
        if(keys.digit0Key.wasPressedThisFrame)Select(-1);
        if(keys.digit1Key.wasPressedThisFrame)Select(0);
        if(keys.digit2Key.wasPressedThisFrame)Select(1);
        if(keys.digit3Key.wasPressedThisFrame)Select(2);
        if(keys.fKey.wasPressedThisFrame){mirrored=!mirrored;Select(selected);}
    }
    void OnGUI()
    {
        if(!Application.isPlaying)return;
        GUILayout.BeginArea(new Rect(16,Screen.height-54,670,42),GUI.skin.box);GUILayout.BeginHorizontal();
        if(GUILayout.Button("Alle (0)"))Select(-1);
        if(GUILayout.Button("Hocke (1)"))Select(0);
        if(GUILayout.Button("Tragen (2)"))Select(1);
        if(GUILayout.Button("Anheben (3)"))Select(2);
        if(GUILayout.Button("Richtung (F)")){mirrored=!mirrored;Select(selected);}
        if(GUILayout.Button("Gelenke"))foreach(var rig in figures)rig.showJoints=!rig.showJoints;
        GUILayout.EndHorizontal();GUILayout.EndArea();
    }
}