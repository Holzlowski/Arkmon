using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CommandNavigationTests
{
    static Type T(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n)).First(t=>t!=null);
    static object I(string n)=>T(n).GetProperty("Instance").GetValue(null);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n).Invoke(o,a);
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    [UnityTest]public IEnumerator FirstTurn_CommandsAreAvailableAndBackCancelsTargetingInBothCategories()
    {
        yield return SceneManager.LoadSceneAsync("BattleScene");for(int i=0;i<5;i++)yield return null;
        ((Behaviour)UnityEngine.Object.FindFirstObjectByType(T("EnemyAI"))).enabled=false;
        var deployment=I("BattleDeployment");var cells=(IList)deployment.GetType().GetField("available",F).GetValue(deployment);
        Call(deployment,"TryPlace",cells[0]);float deadline=Time.realtimeSinceStartup+12;
        while(!(bool)I("UnitManager").GetType().GetProperty("IsBattleRunning").GetValue(I("UnitManager"))&&Time.realtimeSinceStartup<deadline)yield return null;
        yield return null;yield return null;
        var ui=(Component)UnityEngine.Object.FindFirstObjectByType(T("UnitActionSystemUI"));
        var cards=(Transform)ui.GetType().GetField("actionButtonContainerTransform",F).GetValue(ui);
        Assert.That(cards.gameObject.activeSelf,Is.False,"Start with a clear category choice.");
        Assert.That(Call(I("UnitActionSystem"),"GetSelectedAction"),Is.Null);
        Call(ui,"ShowCommandCategory",false);yield return null;
        Assert.That(Call(I("UnitActionSystem"),"GetSelectedAction"),Is.Null,"Opening a category must not select a targetable command.");
        var buttons=cards.GetComponentsInChildren<Button>();Assert.That(buttons.Length,Is.EqualTo(3));
        foreach(var button in buttons)Assert.That(button.IsInteractable(),Is.True,button.name);
        Canvas.ForceUpdateCanvases();
        foreach(var button in buttons)
        {
            var image=button.GetComponent<Image>();
            Assert.That(image.canvasRenderer.GetColor(),Is.EqualTo(Color.white),"Available card renderer must not retain prefab tint: "+button.name);
            Assert.That(button.GetComponent<CanvasGroup>().alpha,Is.EqualTo(1f));
        }
        Assert.That(cards.GetComponentInChildren(T("PaperGuardIcon")),Is.Not.Null);
        var shield=(UnityEngine.UI.Graphic)cards.GetComponentInChildren(T("PaperGuardIcon"));
        Assert.That(shield.GetComponent<CanvasRenderer>(),Is.Not.Null,"Guard needs its own UI renderer.");
        var shieldMesh=shield.canvasRenderer.GetMesh();
        Assert.That(shieldMesh.vertexCount,Is.GreaterThan(0),"Guard shield must produce visible geometry.");
        buttons[0].onClick.Invoke();Assert.That(Call(I("UnitActionSystem"),"GetSelectedAction"),Is.Not.Null);
        Call(ui,"BackToCategories");Assert.That(cards.gameObject.activeSelf,Is.True);Assert.That(Call(I("UnitActionSystem"),"GetSelectedAction"),Is.Null);
        Call(ui,"BackToCategories");Assert.That(cards.gameObject.activeSelf,Is.False);
        Call(ui,"ShowCommandCategory",true);yield return null;
        Assert.That(cards.gameObject.activeSelf,Is.True);Assert.That(cards.GetComponentsInChildren<Button>().Length,Is.EqualTo(3));
        Assert.That(Call(I("UnitActionSystem"),"GetSelectedAction"),Is.Null);
        cards.GetComponentsInChildren<Button>()[0].onClick.Invoke();
        Call(ui,"BackToCategories");Assert.That(Call(I("UnitActionSystem"),"GetSelectedAction"),Is.Null);
        Assert.That(cards.gameObject.activeSelf,Is.True);
        Call(ui,"BackToCategories");Assert.That(cards.gameObject.activeSelf,Is.False);
        Call(ui,"ShowCommandCategory",false);yield return null;
        foreach(var button in cards.GetComponentsInChildren<Button>())Assert.That(button.IsInteractable(),Is.True);
        foreach(var button in cards.GetComponentsInChildren<Button>())
            Assert.That(button.GetComponent<Image>().canvasRenderer.GetColor(),Is.EqualTo(Color.white));
        LogAssert.NoUnexpectedReceived();
    }
}
