using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal sealed class GoldenFieldArenaPreview : MonoBehaviour
{
    internal const string SessionKey = "Arkmon.GoldenFieldReview";
    [System.Serializable]
    internal sealed class Art
    {
        public Material wheat;
        public Shader groundShader;
        public Texture2D earth, panorama;
        public Sprite castle, rock, bush;
    }
    private Material soil, foliage, panoramaMaterial;
    private Transform sealMarker;
    private Material crystalMaterial;
    private Mesh crystalMesh;
    private VolumeProfile grading;
    private UniversalAdditionalCameraData cameraData;
    private bool previousPostProcessing;
    private static readonly int CloudWeather = Shader.PropertyToID("_AurenfeldArenaWeather");
    private Vector2 cloudOrigin;
    private float cloudTime;

    internal static GoldenFieldArenaPreview Create(Art art)
    {
        var preview = new GameObject("Goldene Felder · Arenaprobe").AddComponent<GoldenFieldArenaPreview>();
        preview.Build(art);
        return preview;
    }

    private void Build(Art art)
    {
        var grid = LevelGrid.Instance;
        var center = (grid.GetWorldPosition(new GridPosition(0,0))
            + grid.GetWorldPosition(new GridPosition(grid.GetWidth()-1,grid.GetHeight()-1)))*.5f;
        cloudOrigin = new Vector2(center.x - 15f, center.z - 26f);
        UpdateWeather();
        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string name = renderer.name;
            if (name == "Arena paving" || name.EndsWith("parapet",System.StringComparison.OrdinalIgnoreCase)
                                || name == "Arena border flowers" || name == "Meadow tree" || name == "Paper courtyard tree"
                || name == "Meadow cloud" || name == "Stone pillar" || name == "Pillar cap" || name == "Garden edging" || name == "Garden soil"
                || renderer.transform.parent?.name == "Meadow tree"
                || renderer.transform.parent?.name == "Paper courtyard tree")
                renderer.enabled = false;
        }
        AddGrading();
        var grass = FindFirstObjectByType<MeadowVegetation>();
        var wheat = art.wheat;
        if (grass != null && wheat != null)
            grass.SetGoldenArenaPreview(wheat,center);

        soil = new Material(art.groundShader) { name = "Mown wheat clearing" };
        soil.SetColor("_BaseColor",new Color(.94f,.75f,.43f));
        soil.SetTexture("_BaseMap",art.earth);
        soil.SetVector("_FieldCenter", new Vector4(center.x, center.z, 0, 0));
        var meadow = GameObject.Find("Idyllic meadow");
        if (meadow != null)
            foreach (var renderer in meadow.GetComponentsInChildren<MeshRenderer>())
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "Arkmon/Meadow")
                {
                    renderer.sharedMaterial = soil;
                    renderer.gameObject.name = "Gemähte Lichtung";
                }

        foliage = new Material(Resources.Load<Shader>("AurenfeldScenicPaper"));
        ArrangeCover(grid, art);
        Grove(grass, center);
        var panorama = art.panorama;
        if (panorama != null)
        {
            panoramaMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            panoramaMaterial.SetTexture("_BaseMap", panorama);
            const float height = 108f;
            Part("Aurenfeld · Blick durch die Mauerlücke", PrimitiveType.Quad,
                center + new Vector3(0, 11f, 87f), new Vector3(height * panorama.width / panorama.height, height, 1), panoramaMaterial);
        }

        var castle = art.castle;
        if (castle != null)
            PlaceIllustration("Burg Aurenfeld · 2D-Kulisse", castle, center + new Vector3(-46f, 5.5f, 62f), 13f);
    }

    internal static float PathDistance(Vector2 point)
    {
        float pathZ = 14.6f + 2.8f * Mathf.Sin(point.x * .145f) + .009f * point.x * point.x;
        return Mathf.Abs(point.y - pathZ);
    }

    internal static float ClearingDistance(Vector2 point)
    {
        var q = new Vector2(Mathf.Abs(point.x) / 13.8f, Mathf.Abs(point.y + 1f) / 13.5f);
        return Mathf.Pow(Mathf.Pow(q.x, 4) + Mathf.Pow(q.y, 4), .25f)
            + .045f * Mathf.Sin(point.x * .85f + point.y * .49f)
            + .035f * Mathf.Sin(point.y * .9f - point.x * .48f);
    }

    private void ArrangeCover(LevelGrid grid, Art art)
    {
        var rock = art.rock;
        var bush = art.bush;
        int stones = 0;
        int shrubs = 0;
        foreach (var obstacle in FindObjectsByType<BurningObstacle>(FindObjectsSortMode.InstanceID))
        {
            if (obstacle.name == "Fireproof stone")
            {
                bool first = stones++ == 0;
                PlaceCover(obstacle, rock, grid, first ? new GridPosition(3, 3) : new GridPosition(7, 2),
                    first ? 1.58f : 3.45f, !first);
                if (first)
                {
                    var cluster = Instantiate(obstacle, obstacle.transform.parent);
                    PlaceCover(cluster, rock, grid, new GridPosition(2, 0), 3.25f, false);
                    ExtendCover(cluster, grid, "Kalksteinblock", new Vector2Int(2, 2), 1.04f);
                    AddRearRock(cluster, rock);
                }
                else ExtendCover(obstacle, grid, "Kalksteinrücken", new Vector2Int(2, 1), 1.08f);
            }
            else if (obstacle.name == "Burnable bush")
            {
                bool first = shrubs++ == 0;
                PlaceCover(obstacle, bush, grid, first ? new GridPosition(8, 6) : new GridPosition(2, 6),
                    first ? 1.48f : 1.22f, !first);
                if (first)
                {
                    var hedge = Instantiate(obstacle, obstacle.transform.parent);
                    PlaceCover(hedge, bush, grid, new GridPosition(3, 8), 3.38f, true);
                    ExtendCover(hedge, grid, "Feldhecke", new Vector2Int(2, 1), .83f);
                }
            }
            else obstacle.gameObject.SetActive(false);
        }

        Physics.SyncTransforms();
    }

    private static void ExtendCover(BurningObstacle obstacle, LevelGrid grid, string name, Vector2Int footprint, float height)
    {
        obstacle.name = name;
        obstacle.SetFootprint(footprint);
        Vector3 span = grid.GetWorldPosition(new GridPosition(footprint.x - 1, footprint.y - 1))
            - grid.GetWorldPosition(new GridPosition(0, 0));
        var collider = obstacle.GetComponent<BoxCollider>();
        collider.size += span;
        collider.center += span * .5f;

        var body = obstacle.GetComponentInChildren<SpriteRenderer>();
        if (body == null) return;
        body.transform.localPosition += span * .5f;
        if (body.sprite == null) return;
        var scale = body.transform.localScale;
        scale.y = height / body.sprite.bounds.size.y;
        body.transform.localScale = scale;
    }

    private void AddRearRock(BurningObstacle obstacle, Sprite sprite)
    {
        if (sprite == null) return;
        var front = obstacle.GetComponentInChildren<SpriteRenderer>();
        if (front == null) return;
        var rear = new GameObject("Hinterer Kalkstein", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        rear.transform.SetParent(obstacle.transform, false);
        rear.transform.localPosition = front.transform.localPosition + new Vector3(.12f, 0, .82f);
        rear.transform.localRotation = front.transform.localRotation;
        rear.transform.localScale = Vector3.one * (1.82f / sprite.bounds.size.x);
        rear.sprite = sprite;
        rear.flipX = true;
        rear.sharedMaterial = foliage;
        rear.color = new Color(.87f, .89f, .86f);
        rear.sortingLayerID = front.sortingLayerID;
        rear.sortingOrder = front.sortingOrder - 1;
    }
    private void PlaceCover(BurningObstacle obstacle, Sprite sprite, LevelGrid grid, GridPosition cell,
        float width, bool mirrored)
    {
        obstacle.transform.position = grid.GetWorldPosition(cell);
        var renderer = obstacle.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null || sprite == null) return;
        renderer.sprite = sprite;
        renderer.sharedMaterial = foliage;
        renderer.flipX = mirrored;
        renderer.color = mirrored ? new Color(.90f, .93f, .93f) : Color.white;
        renderer.transform.localPosition = new Vector3(0, -.14f, 0);
        renderer.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
        PaperGroundShadow.Attach(renderer.gameObject, renderer);
    }
    internal void RefreshCoverWalkability()
    {
        var grid = LevelGrid.Instance;
        for (int x = 0; x < grid.GetWidth(); x++)
            for (int z = 0; z < grid.GetHeight(); z++)
                Pathfinding.Instance.RefreshWalkability(new GridPosition(x, z));
    }

    private void Grove(MeadowVegetation meadow, Vector3 center)
    {
        Tree("Oak", center + new Vector3(-19f, 0, -.5f), 6.4f, meadow);
        Tree("Birch", center + new Vector3(-13.7f, 0, 6.5f), 5.2f, meadow);
        Tree("Birch", center + new Vector3(-16.6f, 0, 9.8f), 4.5f, meadow);
        Tree("Oak", center + new Vector3(-19f, 0, 21f), 6.9f, meadow);

        Tree("Willow", center + new Vector3(16.7f, 0, 9f), 6.6f, meadow);
        Tree("Birch", center + new Vector3(22f, 0, 23f), 5.5f, meadow);
        Tree("Birch", center + new Vector3(17f, 0, 29f), 5.3f, meadow);
    }

    private void Tree(string species, Vector3 point, float height, MeadowVegetation meadow)
    {
        var sprite = Resources.Load<Sprite>("CourtyardStyle/Trees/" + species);
        if (sprite == null) return;
        if (meadow != null) point = meadow.OriginalSurfacePoint(point);
        var tree = PlaceIllustration(species == "Birch" ? "Feldbirke" : "Feldbaum · " + species, sprite, point, height);
        tree.position -= Vector3.up * (sprite.bounds.min.y * tree.localScale.y);
        tree.rotation = Quaternion.Euler(0, 10, 0);
    }

    private void UpdateWeather()
    {
        var drift = cloudOrigin + new Vector2(.69f, .38f) * cloudTime;
        Shader.SetGlobalVector(CloudWeather, new Vector4(drift.x, drift.y, .92f, 0));
    }

    private void Update()
    {
        if (sealMarker == null || !sealMarker.gameObject.activeInHierarchy)
            soil.SetVector("_SealGround", Vector4.zero);
        cloudTime += Time.deltaTime;
        UpdateWeather();
    }

    private void OnDisable() => Shader.SetGlobalVector(CloudWeather, Vector4.zero);
    private Transform PlaceIllustration(string name, Sprite sprite, Vector3 position, float height)
    {
        var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        renderer.transform.SetParent(transform, false);
        renderer.transform.position = position;
        renderer.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
        renderer.sprite = sprite;
        renderer.sharedMaterial = foliage;
        return renderer.transform;
    }

    private void AddGrading()
    {
        var camera = Camera.main;
        if (camera == null) return;
        cameraData = camera.GetUniversalAdditionalCameraData();
        previousPostProcessing = cameraData.renderPostProcessing;
        cameraData.renderPostProcessing = true;

        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1000;
        grading = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = grading;
        var color = grading.Add<ColorAdjustments>();
        color.contrast.Override(18f);
        color.saturation.Override(6f);
        color.postExposure.Override(-.12f);
        var tones = grading.Add<ShadowsMidtonesHighlights>();
        tones.shadows.Override(new Vector4(.93f, .98f, 1.07f, 0));
        tones.highlights.Override(new Vector4(1.065f, 1.018f, .94f, 0));
        var bloom = grading.Add<Bloom>();
        bloom.intensity.Override(.43f);
        bloom.threshold.Override(1.08f);
        bloom.scatter.Override(.59f);
        var vignette = grading.Add<Vignette>();
        vignette.intensity.Override(.22f);
        vignette.smoothness.Override(.52f);
        grading.Add<DepthOfField>().mode.Override(DepthOfFieldMode.Off);
    }

    internal void StageSeal(Transform original, GridPosition[] cells)
    {
        if (original == null) return;
        foreach (var renderer in original.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        var grid = LevelGrid.Instance;
        Vector3 center = Vector3.zero;
        foreach (var cell in cells) center += grid.GetWorldPosition(cell);
        center /= cells.Length;
        sealMarker = original;
        soil.SetVector("_SealGround", new Vector4(center.x, center.z, 1, 0));

        var crystal = new GameObject("Siegel · Arkanring", typeof(MeshFilter), typeof(MeshRenderer));
        crystal.transform.SetParent(original, false);
        crystal.transform.position = center + Vector3.up * .32f;
        crystal.transform.rotation = Quaternion.Euler(9f, 27f, -7f);
        crystalMesh = CreateCrystal();
        crystal.GetComponent<MeshFilter>().sharedMesh = crystalMesh;
        crystalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        crystalMaterial.SetColor("_BaseColor", new Color(.40f, .13f, .77f));
        crystalMaterial.SetColor("_EmissionColor", new Color(.30f, .045f, .67f));
        crystalMaterial.EnableKeyword("_EMISSION");
        crystal.GetComponent<MeshRenderer>().sharedMaterial = crystalMaterial;
    }

    private static Mesh CreateCrystal()
    {
        var points = new[] { new Vector3(0, .73f, 0), new Vector3(.34f, .03f, 0),
            new Vector3(0, .03f, .34f), new Vector3(-.34f, .03f, 0),
            new Vector3(0, .03f, -.34f), new Vector3(0, -.60f, 0) };
        int[] faces = { 0,2,1, 0,3,2, 0,4,3, 0,1,4, 5,1,2, 5,2,3, 5,3,4, 5,4,1 };
        var vertices = new Vector3[faces.Length];
        var triangles = new int[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            vertices[i] = points[faces[i]];
            triangles[i] = i;
        }
        var mesh = new Mesh { name = "Faceted seal crystal", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    private GameObject Part(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material material)
    {
        var part=GameObject.CreatePrimitive(shape);
        part.name=name;
        part.transform.SetParent(transform,false);
        part.transform.position=position;
        part.transform.localScale=scale;
        part.GetComponent<Collider>().enabled=false;
        var renderer=part.GetComponent<MeshRenderer>();
        if (material != null) renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        return part;
    }

    private void OnDestroy()
    {
        if (cameraData != null) cameraData.renderPostProcessing = previousPostProcessing;
        if (grading != null)
        {
            foreach (var component in grading.components) Destroy(component);
            Destroy(grading);
        }
        Destroy(crystalMesh);
        Destroy(crystalMaterial);
        Destroy(soil);
        Destroy(foliage);
        Destroy(panoramaMaterial);
    }
}
