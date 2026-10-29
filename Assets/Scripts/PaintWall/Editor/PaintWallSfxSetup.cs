#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>One-off editor utility to wire feedback SFX AudioSources onto GameManager.</summary>
public static class PaintWallSfxSetup
{
    [MenuItem("PaintWall/Setup Feedback SFX")]
    public static void Setup()
    {
        GameObject gm = GameObject.Find("GameManager");
        if (gm == null) { Debug.LogError("[PaintWallSfxSetup] GameManager not found."); return; }

        CreateSfx(gm.transform, "SFX_Milestone", "Assets/SFX/chime.mp3", 0.6f);
        CreateSfx(gm.transform, "SFX_Complete", "Assets/SFX/vitcory.mp3", 0.8f);

        Debug.Log("[PaintWallSfxSetup] Feedback SFX wired.");
    }

    static void CreateSfx(Transform parent, string name, string clipPath, float volume)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);

        var src = go.GetComponent<AudioSource>();
        if (src == null) src = go.AddComponent<AudioSource>();
        src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D, HUD-style feedback
        src.volume = volume;

        if (src.clip == null)
            Debug.LogWarning($"[PaintWallSfxSetup] Clip not found at {clipPath}");
    }
}
#endif
