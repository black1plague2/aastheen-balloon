using UnityEngine;

/// <summary>
/// Paints directly onto a per-surface RenderTexture canvas wherever a paint
/// tool tip touches this object, and periodically samples coverage back to
/// PaintTaskManager. Attach to Wall_Front / Ceiling by PaintWallAutoWire.
/// </summary>
[RequireComponent(typeof(MeshRenderer), typeof(Collider))]
public class PaintableSurface : MonoBehaviour
{
    [Header("Canvas")]
    public int canvasSize = 1024;
    public Color primerColor = new Color(0.92f, 0.90f, 0.85f, 1f);
    public Color paintColor  = new Color(0.75f, 0.15f, 0.2f, 1f);
    [Range(4f, 60f)] public float brushRadiusPixels = 26f;

    [Header("Coverage Sampling")]
    public int samplePixelSize = 32;
    public float sampleInterval = 0.4f;

    [Header("Prescription Guide")]
    // Closer to primerColor than paintColor under ColorDistanceSqr, so an
    // un-painted guide stroke doesn't get counted as coverage by the sampler.
    public Color guideColor = new Color(1f, 0.82f, 0.25f, 1f);
    [Range(4f, 30f)] public float guideLineThicknessPixels = 12f;

    [HideInInspector] public PaintTaskManager taskManager;

    private RenderTexture canvasRT;
    private Material stampMaterial;
    private Material surfaceMaterialInstance;
    private Collider _collider;
    private Texture2D sampleTex;
    private float _lastSampleTime;
    private ParticleSystem splatterFx;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        splatterFx = CreateSplatterFx();

        canvasRT = new RenderTexture(canvasSize, canvasSize, 0, RenderTextureFormat.ARGB32);
        canvasRT.name = gameObject.name + "_PaintCanvas";
        canvasRT.Create();

        var prevActive = RenderTexture.active;
        RenderTexture.active = canvasRT;
        GL.Clear(true, true, primerColor);
        RenderTexture.active = prevActive;

        var meshRenderer = GetComponent<MeshRenderer>();
        surfaceMaterialInstance = meshRenderer.material; // instance copy, safe to mutate
        surfaceMaterialInstance.SetTexture("_BaseMap", canvasRT);
        surfaceMaterialInstance.SetColor("_BaseColor", Color.white);

        // Loaded from Resources (not Shader.Find("Hidden/Internal-Colored")) — that internal
        // shader is DontSave-flagged and gets stripped from device builds despite working in-editor.
        stampMaterial = new Material(Resources.Load<Shader>("Shaders/PaintStamp"));
        stampMaterial.hideFlags = HideFlags.HideAndDontSave;

        sampleTex = new Texture2D(samplePixelSize, samplePixelSize, TextureFormat.RGBA32, false);
    }

    void OnDestroy()
    {
        if (canvasRT != null) canvasRT.Release();
    }

    /// <summary>Called by PaintTipContact while a tool tip overlaps this surface.</summary>
    public void PaintAtWorldPoint(Vector3 worldPoint, Vector3 fromDirection)
    {
        Ray ray = new Ray(worldPoint - fromDirection * 0.15f, fromDirection);
        if (!_collider.Raycast(ray, out RaycastHit hit, 0.5f)) return;

        StampAt(hit.textureCoord);
        EmitSplatter(hit.point, hit.normal);
        taskManager?.ReportStroke();
        MaybeSampleCoverage();
    }

    /// <summary>Draws the prescribed stroke pattern onto the canvas as a guide.
    /// Painting over a guide stroke naturally covers it, doubling as a progress cue.</summary>
    public void DrawPatternGuide(PaintPatternType pattern)
    {
        var prevActive = RenderTexture.active;
        RenderTexture.active = canvasRT;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, canvasSize, canvasSize, 0);
        stampMaterial.SetPass(0);

        switch (pattern)
        {
            case PaintPatternType.Vertical: DrawStripeGuide(vertical: true);  break;
            case PaintPatternType.Circular: DrawRingGuide();                  break;
            case PaintPatternType.Zigzag:   DrawZigzagGuide();                break;
            default:                        DrawStripeGuide(vertical: false); break; // Horizontal
        }

        GL.PopMatrix();
        RenderTexture.active = prevActive;
    }

    void DrawStripeGuide(bool vertical)
    {
        const int stripeCount = 5;
        float margin = canvasSize * 0.08f;
        float usable = canvasSize - margin * 2f;
        for (int i = 0; i < stripeCount; i++)
        {
            float pos = margin + (i + 0.5f) / stripeCount * usable;
            if (vertical) DrawGuideLine(pos, margin, pos, canvasSize - margin);
            else          DrawGuideLine(margin, pos, canvasSize - margin, pos);
        }
    }

    void DrawRingGuide()
    {
        const int ringCount = 4;
        float maxRadius = canvasSize * 0.42f;
        Vector2 center = new Vector2(canvasSize * 0.5f, canvasSize * 0.5f);
        for (int r = 1; r <= ringCount; r++)
            DrawGuideRing(center, maxRadius * r / ringCount);
    }

    void DrawZigzagGuide()
    {
        const int segments = 6;
        float margin = canvasSize * 0.1f;
        float usableW = canvasSize - margin * 2f;
        float usableH = canvasSize - margin * 2f;
        Vector2 prev = new Vector2(margin, margin);
        for (int i = 1; i <= segments; i++)
        {
            var cur = new Vector2(margin + i / (float)segments * usableW,
                                   margin + (i % 2 == 0 ? 0f : usableH));
            DrawGuideLine(prev.x, prev.y, cur.x, cur.y);
            prev = cur;
        }
    }

    void DrawGuideLine(float x0, float y0, float x1, float y1)
    {
        float dist  = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
        int   steps = Mathf.Max(1, Mathf.CeilToInt(dist / (guideLineThicknessPixels * 0.5f)));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            DrawDisc(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), guideLineThicknessPixels * 0.5f, guideColor);
        }
    }

    void DrawGuideRing(Vector2 center, float radius)
    {
        int steps = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * radius / (guideLineThicknessPixels * 0.5f)));
        for (int i = 0; i <= steps; i++)
        {
            float a = i / (float)steps * Mathf.PI * 2f;
            DrawDisc(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius,
                     guideLineThicknessPixels * 0.5f, guideColor);
        }
    }

    ParticleSystem CreateSplatterFx()
    {
        var go = new GameObject("SplatterFx");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.4f;
        main.startSize = 0.02f;
        main.startColor = paintColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false; // we call Emit() manually

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.01f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        // Resources-loaded (not Shader.Find("Particles/Standard Unlit")) — that built-in shader
        // gets stripped from device builds, and threw here before canvasRT/stampMaterial were
        // ever initialized in Awake(), silently breaking painting entirely.
        renderer.material = new Material(Resources.Load<Shader>("Shaders/PaintSplatter"));
        renderer.material.SetColor("_Color", paintColor);

        return ps;
    }

    void EmitSplatter(Vector3 worldPoint, Vector3 normal)
    {
        if (splatterFx == null) return;
        splatterFx.transform.position = worldPoint;
        splatterFx.transform.rotation = Quaternion.LookRotation(normal);
        splatterFx.Emit(4);
    }

    void StampAt(Vector2 uv)
    {
        var prevActive = RenderTexture.active;
        RenderTexture.active = canvasRT;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, canvasSize, canvasSize, 0);
        stampMaterial.SetPass(0);

        float px = uv.x * canvasSize;
        float py = (1f - uv.y) * canvasSize;
        DrawDisc(px, py, brushRadiusPixels, paintColor);

        GL.PopMatrix();
        RenderTexture.active = prevActive;
    }

    void MaybeSampleCoverage()
    {
        if (Time.unscaledTime - _lastSampleTime < sampleInterval) return;
        _lastSampleTime = Time.unscaledTime;

        var prevActive = RenderTexture.active;
        var tempRT = RenderTexture.GetTemporary(samplePixelSize, samplePixelSize, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(canvasRT, tempRT);
        RenderTexture.active = tempRT;
        sampleTex.ReadPixels(new Rect(0, 0, samplePixelSize, samplePixelSize), 0, 0);
        sampleTex.Apply(false);
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(tempRT);

        var pixels = sampleTex.GetPixels32();
        int painted = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (ColorDistanceSqr(pixels[i], paintColor) < ColorDistanceSqr(pixels[i], primerColor))
                painted++;
        }

        taskManager?.ReportCoverage((float)painted / pixels.Length);
    }

    static float ColorDistanceSqr(Color32 a, Color b)
    {
        float dr = a.r / 255f - b.r;
        float dg = a.g / 255f - b.g;
        float db = a.b / 255f - b.b;
        return dr * dr + dg * dg + db * db;
    }

    static void DrawDisc(float cx, float cy, float radius, Color color)
    {
        GL.Begin(GL.TRIANGLES);
        GL.Color(color);
        const int segments = 20;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.PI * 2f / segments;
            float a1 = (i + 1) * Mathf.PI * 2f / segments;
            GL.Vertex3(cx, cy, 0);
            GL.Vertex3(cx + Mathf.Cos(a0) * radius, cy + Mathf.Sin(a0) * radius, 0);
            GL.Vertex3(cx + Mathf.Cos(a1) * radius, cy + Mathf.Sin(a1) * radius, 0);
        }
        GL.End();
    }
}
