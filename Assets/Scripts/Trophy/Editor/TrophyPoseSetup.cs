using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;

public static class TrophyPoseSetup
{
    [MenuItem("Trophy/1. Add XRHandTrackingEvents to Scene")]
    static void AddHandTrackingEvents()
    {
        var existing = Object.FindFirstObjectByType<XRHandTrackingEvents>();
        if (existing != null)
        {
            Debug.Log($"[TrophySetup] XRHandTrackingEvents already exists: {existing.gameObject.name}");
            return;
        }

        var go  = new GameObject("XRHandTrackingEvents_Left");
        var evt = go.AddComponent<XRHandTrackingEvents>();
        var so  = new SerializedObject(evt);
        var hp  = so.FindProperty("m_Handedness");
        if (hp != null) { hp.enumValueIndex = (int)Handedness.Left; so.ApplyModifiedProperties(); }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TrophySetup] Created XRHandTrackingEvents_Left.");
    }

    [MenuItem("Trophy/2. Wire Detectors")]
    static void WireDetectors()
    {
        var trackingEvents = Object.FindFirstObjectByType<XRHandTrackingEvents>();
        if (trackingEvents == null)
        {
            Debug.LogError("[TrophySetup] No XRHandTrackingEvents in scene. Run Trophy/1 first.");
            return;
        }

        foreach (var d in Object.FindObjectsByType<TrophyPoseDetector>(FindObjectsInactive.Include))
            Object.DestroyImmediate(d.gameObject);

        var guids = AssetDatabase.FindAssets("t:XRHandShape", new[] { "Assets/PoseData" });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[TrophySetup] No XRHandShape assets in Assets/PoseData. Record poses with the XR Hands sample first.");
            return;
        }

        int created = 0;
        foreach (var guid in guids)
        {
            var path  = AssetDatabase.GUIDToAssetPath(guid);
            var shape = AssetDatabase.LoadAssetAtPath<XRHandShape>(path);
            if (shape == null) continue;

            string pname    = System.IO.Path.GetFileNameWithoutExtension(path);
            var go          = new GameObject($"PoseDetector_{pname}");
            var detector    = go.AddComponent<TrophyPoseDetector>();
            var so          = new SerializedObject(detector);
            so.FindProperty("handShape").objectReferenceValue           = shape;
            so.FindProperty("poseName").stringValue                     = pname;
            so.FindProperty("handTrackingEvents").objectReferenceValue  = trackingEvents;
            so.ApplyModifiedProperties();
            created++;
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[TrophySetup] Wired {created} detectors.");
    }
}
