#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CourtyardScaleReviewTests
{
    [UnityTest] public IEnumerator SceneLinesUpTheGameFiguresAndTheSeatedCoachman()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/Scenes/AnimationStudies/CourtyardScaleReview.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;

        var camera = Camera.main;
        Assert.That(camera.orthographic,Is.True);
        Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.identity),Is.LessThan(.01f));
        Assert.That(GameObject.FindWithTag("Player"),Is.Null);

        var elian = GameObject.Find("Elian paper walking rig").transform;
        var guard = GameObject.Find("Guard paper walking rig").transform;
        var mentor = GameObject.Find("Arkanmeisterin").GetComponent<SpriteRenderer>();
        var horse = GameObject.Find("Carriage horse illustration").GetComponent<SpriteRenderer>();
        var coachman = GameObject.Find("Coachman illustration").GetComponent<SpriteRenderer>();
        var seat = GameObject.Find("Coachman's seat").GetComponent<Renderer>();

        foreach (var sole in new[]{RenderedBounds(elian).min.y,RenderedBounds(guard).min.y,mentor.bounds.min.y,horse.bounds.min.y})
            Assert.That(sole,Is.EqualTo(0).Within(.07f));
        Assert.That(RenderedBounds(elian).size.y,Is.LessThan(mentor.bounds.size.y));
        Assert.That(RenderedBounds(guard).size.y,Is.EqualTo(mentor.bounds.size.y).Within(.14f));
        Assert.That(coachman.transform.position.y,Is.EqualTo(seat.bounds.max.y).Within(.015f));
        var footboard = GameObject.Find("Coachman's footboard").GetComponent<Renderer>();
        // The visible lower sole reaches 3.8 percent of the source texture's height.
        float soleOffset = (.038f*coachman.sprite.rect.height-coachman.sprite.pivot.y)/coachman.sprite.pixelsPerUnit;
        float bootY = coachman.transform.TransformPoint(Vector3.up*soleOffset).y;
        Assert.That(bootY,Is.EqualTo(footboard.bounds.max.y).Within(.02f));
        Assert.That(horse.sprite.name,Is.EqualTo("Carriage Draft Horse"));
        Assert.That(coachman.sprite.name,Is.EqualTo("Carriage Coachman"));

        camera.aspect=16f/9f;
        yield return null;
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)) label.ForceMeshUpdate();
        typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static)
            .Invoke(null,new object[]{"groessenprobe"});

        var original=GameObject.Find("Hof · Größenprobe");
        var reviewType=original.GetComponents<MonoBehaviour>().Single().GetType();
        original.SetActive(false);
        var custom=new GameObject("Probe mit Vergleichsfigur");
        custom.SetActive(false);
        var component=custom.AddComponent(reviewType);
        var fields=BindingFlags.Instance|BindingFlags.NonPublic;
        reviewType.GetField("comparisonSprite",fields).SetValue(component,mentor.sprite);
        reviewType.GetField("comparisonScale",fields).SetValue(component,.85f);
        custom.SetActive(true);
        yield return null;
        yield return null;
        var comparison=GameObject.Find("Vergleichsfigur").GetComponent<SpriteRenderer>();
        Assert.That(comparison.sprite,Is.SameAs(mentor.sprite));
        Assert.That(comparison.bounds.min.y,Is.EqualTo(0).Within(.01f));
        Assert.That(comparison.transform.localScale,Is.EqualTo(Vector3.one*.85f));
        var comparisonCamera=custom.GetComponentInChildren<Camera>();
        Assert.That(comparisonCamera.WorldToViewportPoint(comparison.bounds.max).x,Is.LessThan(1));
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator GuardFaceComparisonKeepsTheSameScaleAndPose()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/Scenes/AnimationStudies/GuardFaceReview.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;

        var camera = Camera.main;
        Assert.That(camera.orthographic,Is.True);
        var original = GameObject.Find("Burgwache · Original").transform;
        var variant = GameObject.Find("Burgwache · Variante").transform;
        var originalRig = original.Find("Guard illustrated figure/Guard paper walking rig");
        var variantRig = variant.Find("Guard illustrated figure/Guard paper walking rig");
        Assert.That(variantRig.lossyScale,Is.EqualTo(originalRig.lossyScale));
        Assert.That(Mathf.Abs(originalRig.lossyScale.x),Is.EqualTo(originalRig.lossyScale.y).Within(.001f));
        Assert.That(RenderedBounds(variantRig).size.y,Is.EqualTo(RenderedBounds(originalRig).size.y).Within(.001f));
        Assert.That(RenderedBounds(variantRig).min.y,Is.EqualTo(RenderedBounds(originalRig).min.y).Within(.001f));
        Assert.That(variant.position.z,Is.EqualTo(original.position.z));

        foreach (Transform part in originalRig)
        {
            var counterpart = variantRig.Find(part.name);
            var sprite = part.GetComponent<SpriteRenderer>().sprite;
            var variantSprite = counterpart.GetComponent<SpriteRenderer>().sprite;
            Assert.That(counterpart.localPosition,Is.EqualTo(part.localPosition));
            Assert.That(counterpart.localRotation,Is.EqualTo(part.localRotation));
            if (part.name == "Guard upper body")
            {
                Assert.That(variantSprite.name,Is.EqualTo("Guard narrower jaw"));
                AssertNativeTextureSize(sprite.texture);
                AssertNativeTextureSize(variantSprite.texture);
                Assert.That(variantSprite.rect.size,Is.EqualTo(sprite.rect.size));
                Assert.That(variantSprite.pivot,Is.EqualTo(sprite.pivot));
                Assert.That(variantSprite.pixelsPerUnit,Is.EqualTo(sprite.pixelsPerUnit));
            }
            else Assert.That(variantSprite,Is.SameAs(sprite));
        }

        camera.aspect=16f/9f;
        yield return null;
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)) label.ForceMeshUpdate();
        var capture = typeof(GameIntroTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);
        capture.Invoke(null,new object[]{"burgwache-vergleich"});
        original.position += Vector3.right*.23f;
        variant.position -= Vector3.right*.23f;
        foreach (var line in UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
            line.enabled = false;
        camera.transform.position = new Vector3(.06f,1.98f,-10);
        camera.orthographicSize = .72f;
        capture.Invoke(null,new object[]{"burgwache-gesichter"});
        LogAssert.NoUnexpectedReceived();
    }

    private static void AssertNativeTextureSize(Texture2D texture)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        importer.GetSourceTextureWidthAndHeight(out int width,out int height);
        Assert.That(texture.width,Is.EqualTo(width),texture.name);
        Assert.That(texture.height,Is.EqualTo(height),texture.name);
    }

    private static Bounds RenderedBounds(Transform root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>().Where(renderer=>renderer.enabled && !renderer.forceRenderingOff).ToArray();
        var result=renderers[0].bounds;
        foreach(var renderer in renderers)result.Encapsulate(renderer.bounds);
        return result;
    }
}
#endif
