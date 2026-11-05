using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PaperShadowTests
{
    [UnityTest]
    public IEnumerator ContactShadow_FollowsGroundAvoidsOwnColliderAndTracksVisibility()
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PaperGroundShadow"))
            .First(t => t != null);
        MethodInfo attach = type.GetMethod("Attach");
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(1000, 3.75f, 1000);
        floor.transform.localScale = new Vector3(8, .5f, 8);
        GameObject figure = new GameObject("Shadow test figure", typeof(SpriteRenderer), typeof(BoxCollider));
        figure.transform.position = new Vector3(1000, 4, 1000);
        figure.transform.localScale = Vector3.one * 2;
        SpriteRenderer body = figure.GetComponent<SpriteRenderer>();
        body.sprite = Resources.Load<Sprite>("Player-Summoning");
        Assert.That(body.sprite, Is.Not.Null);
        figure.GetComponent<BoxCollider>().center = Vector3.up;
        figure.GetComponent<BoxCollider>().size = new Vector3(1, 2, 1);
        GameObject overhead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overhead.transform.position = new Vector3(1000, 6, 1000);
        overhead.transform.localScale = new Vector3(6, .2f, 6);
        attach.Invoke(null, new object[] { figure, body });
        attach.Invoke(null, new object[] { figure, body });
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        Assert.That(figure.GetComponents(type).Length, Is.EqualTo(1));
        Transform plane = figure.transform.Find("Paper contact shadow");
        Assert.That(plane, Is.Not.Null);
        Assert.That(plane.position.y, Is.EqualTo(4.008f).Within(.001f));
        Assert.That(plane.GetComponent<Collider>(), Is.Null);
        Assert.That(plane.GetComponent<Renderer>().sharedMaterial.shader.name, Is.EqualTo("Arkmon/Paper Ground Shadow"));
        figure.transform.position += Vector3.right;
        Physics.SyncTransforms(); yield return null;
        Assert.That(plane.position.x, Is.EqualTo(figure.transform.position.x));
        floor.transform.rotation = Quaternion.Euler(0, 0, 8);
        figure.transform.position += Vector3.right * .1f;
        Physics.SyncTransforms(); yield return null;
        Assert.That(Vector3.Angle(plane.up, floor.transform.up), Is.LessThan(.1f), "Contact should follow a sloped floor.");
        floor.SetActive(false); figure.transform.position += Vector3.right * .1f;
        Physics.SyncTransforms(); yield return null;
        Assert.That(plane.GetComponent<Renderer>().enabled, Is.False, "No ground means no floating shadow; the overhead surface is not a floor.");
        floor.SetActive(true); figure.transform.position -= Vector3.right * .1f;
        Physics.SyncTransforms(); yield return null;
        Assert.That(plane.GetComponent<Renderer>().enabled, Is.True);
        body.color = new Color(1, 1, 1, 0);
        yield return null;
        var properties = new MaterialPropertyBlock();
        plane.GetComponent<Renderer>().GetPropertyBlock(properties);
        Assert.That(properties.GetColor("_ShadowColor").a, Is.Zero);
        body.enabled = false; yield return null;
        Assert.That(plane.GetComponent<Renderer>().enabled, Is.False);
        UnityEngine.Object.Destroy(figure); UnityEngine.Object.Destroy(floor); UnityEngine.Object.Destroy(overhead);
        yield return null;
        Assert.That(plane == null, Is.True);
        LogAssert.NoUnexpectedReceived();
    }
}
