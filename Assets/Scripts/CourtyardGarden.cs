using System;
using System.Collections;
using UnityEngine;

public sealed class CourtyardGarden : MonoBehaviour, IInteractableAsPlayer
{
    private static bool hasCan;
    private static int watered;
    private const int AllBeds = 7;
    private int bedIndex = -1;
    private Renderer soil;
    private GameObject canModel;

    public static bool Completed => watered == AllBeds;

    public static void ResetProgress() { hasCan = false; watered = 0; }

    public string Label => bedIndex < 0
        ? hasCan ? "Gießkanne ansehen" : "Gießkanne nehmen"
        : IsWatered ? "Beet ansehen" : hasCan ? "Beet gießen" : "Trockenes Beet ansehen";

    private bool IsWatered => bedIndex >= 0 && (watered & (1 << bedIndex)) != 0;
    public Vector3 GetTransform() => transform.position;

    internal static void Install()
    {
        var beds = Array.FindAll(FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None),
            renderer => renderer.name == "Garden soil bed");
        if (beds.Length != 3) return;
        Array.Sort(beds, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        for (int i = 0; i < beds.Length; i++)
        {
            var bed = Spawn("Garden watering point", beds[i].transform.position + new Vector3(0, 0, -2.05f));
            bed.bedIndex = i;
            bed.soil = beds[i];
            bed.RefreshAppearance();
        }
        var station = Spawn("Courtyard watering can", beds[0].transform.position + new Vector3(-1.8f, 0, -2.4f));
        station.canModel = new GameObject("Watering can model");
        station.canModel.transform.SetParent(station.transform, false);

        // Reuse courtyard material; the can is a prop, not another obstacle in the path.
        var metal = GameObject.Find("Garden border")?.GetComponent<Renderer>()?.sharedMaterial;
        Part(station.canModel.transform, PrimitiveType.Cylinder, new Vector3(0,.22f,0), new Vector3(.31f,.20f,.31f), Quaternion.identity, metal);
        Part(station.canModel.transform, PrimitiveType.Cylinder, new Vector3(.22f,.30f,0), new Vector3(.075f,.16f,.075f), Quaternion.Euler(0,0,-58), metal);
        Part(station.canModel.transform, PrimitiveType.Cube, new Vector3(-.18f,.27f,0), new Vector3(.045f,.22f,.07f), Quaternion.identity, metal);
        station.RefreshAppearance();
    }

    private static CourtyardGarden Spawn(string name, Vector3 point)
    {
        var obj = new GameObject(name, typeof(SphereCollider), typeof(CourtyardGarden));
        obj.transform.position = point;
        var trigger = obj.GetComponent<SphereCollider>();
        trigger.radius = .45f; trigger.isTrigger = true;
        return obj.GetComponent<CourtyardGarden>();
    }

    private static void Part(Transform parent, PrimitiveType primitive, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var part = GameObject.CreatePrimitive(primitive);
        part.transform.SetParent(parent, false);
        part.transform.SetLocalPositionAndRotation(position, rotation);
        part.transform.localScale = scale;
        part.GetComponent<Collider>().enabled = false;
        if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
    }

    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        if (dialogue == null || dialogue.IsOpen || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning) return;
        string message;
        if (bedIndex < 0)
        {
            message = hasCan ? "Die Kanne habe ich schon."
                : "Die Beete sind ganz trocken. Mit der Gießkanne kann ich ihnen etwas Wasser geben.";
            hasCan = true;
        }
        else if (IsWatered) message = "Dieses Beet hat schon genug Wasser.";
        else if (!hasCan) message = "Die Erde ist trocken. Neben dem ersten Beet steht eine Gießkanne.";
        else
        {
            watered |= 1 << bedIndex;
            PlayWatering();
            message = watered == AllBeds
                ? "Alle drei Beete sind gegossen. Jetzt kann ich weiter zum Burgtor."
                : "Die Erde ist jetzt feucht. Die anderen Beete brauchen vielleicht auch Wasser.";
        }
        foreach (var point in FindObjectsByType<CourtyardGarden>(FindObjectsSortMode.None))
            point.RefreshAppearance();
        dialogue.ShowMessage(message, IntroChapter.PlayerName);
    }

    private void PlayWatering()
    {
        SoundManager.instance?.PlayEffect("IntroFoley/garden-water", .55f);
        foreach (var point in FindObjectsByType<CourtyardGarden>(FindObjectsSortMode.None))
        {
            if (point.canModel == null) continue;
            var can = Instantiate(point.canModel, transform);
            can.SetActive(true);
            StartCoroutine(Pour(can));
            return;
        }
    }

    private IEnumerator Pour(GameObject can)
    {
        var drops = new Transform[8];
        for (int i = 0; i < drops.Length; i++)
        {
            var drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.transform.SetParent(can.transform.parent, false);
            drop.transform.localScale = Vector3.one * .055f;
            drop.GetComponent<Collider>().enabled = false;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(.42f,.76f,.91f));
            drop.GetComponent<Renderer>().SetPropertyBlock(properties);
            drops[i] = drop.transform;
        }
        can.transform.localPosition = new Vector3(0, .82f, .48f);
        for (float time = 0; time < .9f; time += Time.deltaTime)
        {
            float tilt = Mathf.Clamp01(Mathf.Min(time, .9f - time) / .18f);
            can.transform.localRotation = Quaternion.Euler(0,-90,-55 * tilt);
            for (int i = 0; i < drops.Length; i++)
            {
                float phase = Mathf.Repeat(time * 3.2f + i / (float)drops.Length, 1);
                drops[i].localPosition = new Vector3((i % 3 - 1) * .045f,
                    .96f - .76f * phase * phase, .76f + .92f * phase);
                drops[i].gameObject.SetActive(tilt > .4f);
            }
            yield return null;
        }
        foreach (var drop in drops) Destroy(drop.gameObject);
        Destroy(can);
    }

    private void RefreshAppearance()
    {
        if (canModel != null) canModel.SetActive(!hasCan);
        if (soil != null && IsWatered)
        {
            var properties = new MaterialPropertyBlock();
            soil.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", soil.sharedMaterial.GetColor("_BaseColor") * .56f);
            soil.SetPropertyBlock(properties);
        }
    }
}
