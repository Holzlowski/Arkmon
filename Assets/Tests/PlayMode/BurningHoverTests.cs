using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BurningHoverTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    [UnityTest] public IEnumerator BurningFence_ShowsCurrentFireSeparatelyFromNextHit()
    {
        var obj=new GameObject("Burnable fence");obj.SetActive(false);
        var obstacle=obj.AddComponent(T("BurningObstacle"));
        T("BurningObstacle").GetMethod("Configure").Invoke(obstacle,new[]{Enum.Parse(T("BurningObstacle+MaterialKind"),"Wood"),(object)45,true,false});
        obj.SetActive(true);yield return null;
        var status=obj.GetComponent(T("BurningStatus"));T("BurningStatus").GetMethod("TryIgnite").Invoke(status,new object[]{1f,3,5});
        string current=(string)T("ObstacleConditionVisual").GetMethod("CurrentCondition").Invoke(null,new[]{obstacle});
        Assert.That(current,Does.Contain("Holzzaun · 45/45 LP"));Assert.That(current,Does.Contain("Brennt: 5 LP/Runde · noch 3 Runden"));
        var rules=ScriptableObject.CreateInstance(T("FireRules"));
        var description=T("AttackPreview").GetMethod("BurnDescription",BindingFlags.NonPublic|BindingFlags.Static);
        string next=(string)description.Invoke(null,new object[]{status,true,rules});
        Assert.That(next,Does.Contain("Brennt bereits"));Assert.That(next,Does.Contain("erneuert"));Assert.That(next,Does.Not.Contain("Entzündet"));
        UnityEngine.Object.Destroy(obj);UnityEngine.Object.Destroy(rules);yield return null;
    }
}
