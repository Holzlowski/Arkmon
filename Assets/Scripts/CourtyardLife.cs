using System.Collections;
using TMPro;
using UnityEngine;

public sealed class CourtyardLife : MonoBehaviour, IInteractableAsPlayer
{
    private enum Role { Gardener, Stablekeeper, Carpenter, Barrow }
    private Role role;
    private Transform barrow;
    private Vector3 originalBarrow;
    private static bool parked;
    private bool moving;

    public static void ResetProgress() => parked = false;
    public Vector3 GetTransform() => transform.position;
    public string Label => role switch
    {
        Role.Gardener => "Mit Marta sprechen",
        Role.Stablekeeper => "Mit Hannes sprechen",
        Role.Carpenter => "Mit Ida sprechen",
        _ => parked ? "Schubkarre ansehen" : "Schubkarre beiseitestellen"
    };

    internal static void Install()
    {
        Sign("Werkstatt", new Vector3(-21,0,3));
        Sign("Stall", new Vector3(-9,0,3));
        Sign("Garten", new Vector3(20,0,2.4f));
        Sign("Burgtor", new Vector3(37,0,4));
        Person(Role.Gardener, new Vector3(19.1f,0,6), new Color(.28f,.44f,.25f));
        Person(Role.Stablekeeper, new Vector3(-10,0,10), new Color(.44f,.31f,.20f));
        Person(Role.Carpenter, new Vector3(-22.8f,0,10), new Color(.48f,.25f,.24f));
        var cart = GameObject.Find("Wheelbarrow");
        if (cart == null) return;
        var point = Add(Role.Barrow, cart.transform.position + Vector3.back * 1.35f);
        point.barrow = cart.transform;
        point.originalBarrow = cart.transform.position;
        if (parked) point.PlaceBarrow(1);
    }

    private static CourtyardLife Add(Role kind, Vector3 position)
    {
        var obj = new GameObject("Courtyard " + kind, typeof(SphereCollider), typeof(CourtyardLife));
        obj.transform.position = position;
        var trigger = obj.GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = .5f;
        var life = obj.GetComponent<CourtyardLife>();
        life.role = kind;
        return life;
    }

    private static void Person(Role role, Vector3 position, Color outfit)
    {
        var root = Add(role, position).transform;
        var silhouette = new GameObject("Illustrated resident placeholder").transform;
        silhouette.SetParent(root, false);
        Shape(silhouette, PrimitiveType.Sphere, new Vector3(0,1.48f,0), new Vector3(.48f,.52f,.13f), new Color(.88f,.65f,.43f));
        Shape(silhouette, PrimitiveType.Sphere, new Vector3(0,1.70f,.015f), new Vector3(.59f,.30f,.16f), outfit * .58f);
        Shape(silhouette, PrimitiveType.Sphere, new Vector3(0,.94f,0), new Vector3(.62f,.80f,.18f), outfit);
        Shape(silhouette, PrimitiveType.Cube, new Vector3(0,.89f,-.104f), new Vector3(.47f,.11f,.035f), new Color(.59f,.43f,.24f));
        foreach (float x in new[]{-.13f,.13f})
            Shape(silhouette, PrimitiveType.Cube, new Vector3(x,.34f,0), new Vector3(.18f,.57f,.16f), new Color(.24f,.16f,.10f));
        var facing = silhouette.gameObject.AddComponent<CourtyardResidentFacing>();
        facing.enabled = true;
    }

    private static void Sign(string label, Vector3 position)
    {
        var root = new GameObject("Wayfinding - " + label).transform;
        root.position = position;
        var wood = new Color(.26f,.14f,.07f);
        Shape(root, PrimitiveType.Cube, new Vector3(0,.75f,0), new Vector3(.12f,1.5f,.12f), wood);
        Shape(root, PrimitiveType.Cube, new Vector3(0,1.32f,0), new Vector3(1.65f,.48f,.12f), wood);
        var text = new GameObject("Painted sign label", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        text.transform.SetParent(root,false);
        text.transform.localPosition = new Vector3(0,1.32f,-.075f);
        text.rectTransform.sizeDelta = new Vector2(1.65f,.45f);
        text.fontSize = 2.15f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1,.87f,.61f);
        text.text = label;
    }

    private static void Shape(Transform parent, PrimitiveType shape, Vector3 position, Vector3 size, Color tint)
    {
        var part = GameObject.CreatePrimitive(shape);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        part.GetComponent<Collider>().enabled = false;
        var renderer = part.GetComponent<Renderer>();
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", tint);
        renderer.SetPropertyBlock(properties);
    }

    public void InteractWithPlayer()
    {
        var dialogue = DialogueBoxController.instance;
        if (dialogue == null || dialogue.IsOpen || DemoOpening.IsPresenting || SceneLoadManager.IsTransitioning || moving) return;
        switch (role)
        {
            case Role.Gardener:
                dialogue.ShowMessage(CourtyardGarden.Completed
                    ? "Vielen Dank, Elian. Jetzt kommen die jungen Pflanzen gut durch den Morgen."
                    : "Guten Morgen, Elian. Die Beete sind trocken. Wenn du magst, die Kanne steht am ersten Beet.", "Marta");
                break;
            case Role.Stablekeeper:
                dialogue.ShowMessage("Die Pferde sind versorgt. Heute Morgen sind sie ruhiger als die Leute am Tor.", "Hannes");
                break;
            case Role.Carpenter:
                dialogue.ShowMessage("Ich habe die Werkstatt schon geöffnet. Wenn du am Garten vorbeikommst, stell die Schubkarre gern an den Rand.", "Ida");
                break;
            case Role.Barrow:
                if (parked) dialogue.ShowMessage("Die Schubkarre steht jetzt am Rand.", IntroChapter.PlayerName);
                else if (barrow != null) StartCoroutine(ParkBarrow());
                break;
        }
    }

    private IEnumerator ParkBarrow()
    {
        moving = true;
        SoundManager.instance?.PlayEffect("IntroFoley/footstep_wood_001", .6f);
        const float duration = .7f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            PlaceBarrow(Mathf.SmoothStep(0, 1, t / duration));
            yield return null;
        }
        PlaceBarrow(1);
        parked = true;
        moving = false;
    }

    private void PlaceBarrow(float progress)
    {
        barrow.position = originalBarrow + Vector3.left * (1.4f * progress);
        transform.position = barrow.position + Vector3.back * 1.35f;
    }
}
