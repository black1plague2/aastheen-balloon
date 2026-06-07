using UnityEngine;
using UnityEngine.Rendering;

// Attach to any persistent GameObject (e.g. GameManager).
// Replaces the open-sky skybox with a calm dark clinical environment.
public class SceneVisualSetup : MonoBehaviour
{
    void Start()
    {
        ApplySkybox();
        ApplyAmbientAndFog();
    }

    void ApplySkybox()
    {
        var shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
        {
            // Fallback: tint camera background
            if (Camera.main != null)
            {
                Camera.main.clearFlags       = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor  = new Color(0.06f, 0.08f, 0.14f, 1f);
            }
            return;
        }

        var mat = new Material(shader);
        // No sun, dark therapeutic sky
        mat.SetFloat("_SunDisk",            0);                               // sun off
        mat.SetColor("_SkyTint",            new Color(0.09f, 0.13f, 0.22f)); // deep blue-grey
        mat.SetColor("_GroundColor",        new Color(0.07f, 0.08f, 0.11f)); // near-black ground
        mat.SetFloat("_AtmosphereThickness", 0.25f);
        mat.SetFloat("_Exposure",            0.42f);

        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
    }

    void ApplyAmbientAndFog()
    {
        // Soft trilight ambient — cooler overhead, darker floor
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.22f, 0.30f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.18f, 0.28f);
        RenderSettings.ambientGroundColor  = new Color(0.06f, 0.07f, 0.11f);

        // Subtle depth fog — makes distant geometry fade into the dark sky
        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogColor         = new Color(0.07f, 0.10f, 0.18f);
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance   = 22f;
    }
}
