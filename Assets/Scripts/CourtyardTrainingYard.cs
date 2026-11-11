using TMPro;
using UnityEngine;

public sealed class CourtyardTrainingYard : MonoBehaviour
{
    internal static readonly Vector3 PracticeSpot = new Vector3(-3.9f, 0, 6f);

    Transform dummy;
    Material wood;
    float lastHit = float.NegativeInfinity;
    public bool IsReacting => Time.time - lastHit < .55f;

    internal static CourtyardTrainingYard Install()
    {
        var oak = Resources.Load<Material>("CourtyardStyle/Stable oak");
        var target = CourtyardGuard.Grounded(PracticeSpot + new Vector3(-.76f, 0, .04f));
        if (oak == null || target.y < -90) return null;

        var yard = new GameObject("Guard training yard").AddComponent<CourtyardTrainingYard>();
        yard.transform.position = target;
        yard.wood = oak;
        yard.BuildDummy();
        yard.BuildEquipment();
        return yard;
    }

    void BuildDummy()
    {
        Part(transform, "Dummy foot", new Vector3(0,.05f,0), new Vector3(.65f,.1f,.16f));
        Part(transform, "Dummy foot", new Vector3(0,.055f,0), new Vector3(.16f,.11f,.64f));
        Part(transform, "Dummy post", new Vector3(0,.32f,0), new Vector3(.12f,.62f,.12f));
        dummy = new GameObject("Guard training dummy").transform;
        dummy.SetParent(transform, false);
        dummy.localPosition = Vector3.up * .58f;
        Part(dummy, "Padded torso", new Vector3(0,.28f,0), new Vector3(.39f,.25f,.31f), PrimitiveType.Cylinder);
        Part(dummy, "Shoulder bar", new Vector3(0,.42f,.005f), new Vector3(.69f,.095f,.105f));
        Part(dummy, "Wooden head", new Vector3(0,.65f,0), new Vector3(.26f,.3f,.26f), PrimitiveType.Sphere);

        var shield = Part(dummy, "Practice shield", new Vector3(.08f,.28f,-.16f), new Vector3(.34f,.035f,.34f), PrimitiveType.Cylinder);
        shield.localRotation = Quaternion.Euler(90, -24, 0);
        var boss = Part(shield, "Painted bullseye", new Vector3(0,-1.1f,0), new Vector3(.42f,.24f,.42f), PrimitiveType.Cylinder);
        var paint = new MaterialPropertyBlock();
        paint.SetColor("_BaseColor", new Color(1f,.42f,.23f));
        boss.GetComponent<Renderer>().SetPropertyBlock(paint);
    }

    void BuildEquipment()
    {
        var equipment = new GameObject("Guard practice equipment").transform;
        equipment.SetParent(transform, false);
        equipment.position = CourtyardGuard.Grounded(transform.position + new Vector3(1.5f,0,1.65f));
        Part(equipment, "Sign post", new Vector3(0,.49f,0), new Vector3(.10f,.98f,.1f));
        var board = Part(equipment, "Training sign", new Vector3(0,.94f,-.035f), new Vector3(1.42f,.3f,.10f));
        var label = new GameObject("Training sign lettering").AddComponent<TextMeshPro>();
        label.transform.SetParent(equipment, false);
        label.transform.localPosition = board.localPosition + Vector3.back * .056f;
        label.rectTransform.sizeDelta = new Vector2(1.32f,.28f);
        label.font = PaperUIStyle.Load()?.font;
        label.fontSize = 1.85f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1,.85f,.57f);
        label.text = "Wachtraining";

        Part(equipment, "Practice rack beam", new Vector3(-2.02f,.31f,-.3f), new Vector3(.92f,.095f,.1f));
        foreach (float x in new[] { -2.38f, -2.14f, -1.90f, -1.66f })
            Part(equipment, "Practice rack stake", new Vector3(x,.22f,-.3f), new Vector3(.08f,.44f,.10f));
        foreach (float x in new[] { -2.26f, -1.78f })
        {
            var sword = Part(equipment, "Wooden practice sword", new Vector3(x,.35f,-.385f), new Vector3(.065f,.62f,.045f));
            sword.localRotation = Quaternion.Euler(-8,0,-12);
            Part(sword, "Wooden crossguard", new Vector3(0,.28f,0), new Vector3(2.9f,.085f,1.3f));
        }
    }

    Transform Part(Transform parent, string label, Vector3 position, Vector3 size, PrimitiveType shape = PrimitiveType.Cube)
    {
        var part = GameObject.CreatePrimitive(shape);
        part.name = label;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        var collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        var renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = wood;
        var properties = new MaterialPropertyBlock();
        properties.SetVector("_ObjectScale", part.transform.lossyScale);
        properties.SetFloat("_EdgeWidth", shape == PrimitiveType.Cube ? .012f : 0);
        properties.SetFloat("_GrainAxis", size.x > size.y ? 0 : 1);
        renderer.SetPropertyBlock(properties);
        return part.transform;
    }

    internal void Hit() => lastHit = Time.time;

    void LateUpdate()
    {
        float progress = Mathf.Clamp01((Time.time - lastHit) / .55f);
        float sway = Mathf.Sin(progress * 3f * Mathf.PI) * (1f - progress) * 18f;
        dummy.localRotation = Quaternion.Euler(0, sway * .4f, sway);
    }
}
