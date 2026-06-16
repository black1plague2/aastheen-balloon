using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Creates the Bootstrap scene (PIN keypad + Verse auth + server config) from scratch.
/// Run via: Verse → Create Bootstrap Scene
/// </summary>
public class BootstrapSceneSetup : Editor
{
    [MenuItem("Verse/Create Bootstrap Scene")]
    public static void CreateBootstrapScene()
    {
        const string scenePath = "Assets/Scenes/Bootstrap.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        // ── Persistent singletons ─────────────────────────────────────────
        var verseClientGO = new GameObject("_VerseClient");
        verseClientGO.AddComponent<VerseClient>();

        var playlistGO = new GameObject("_PlaylistManager");
        playlistGO.AddComponent<PlaylistManager>();

        var bootstrapGO = new GameObject("_Bootstrap");
        var bm = bootstrapGO.AddComponent<BootstrapManager>();

        // ── OVR Interaction Rig ───────────────────────────────────────────
        // OVRInteractionRig includes OVRCameraRig + controller ray interactors
        // + hand ray interactors + hand poke interactors — all pre-wired.
        bool usedInteractionRig = false;
        var interactionRigPrefab = FindPrefab(
            "Packages/com.meta.xr.sdk.interaction.ovr/Runtime/Prefabs/OVRComprehensiveInteractionRig.prefab",
            "OVRComprehensiveInteractionRig");
        if (interactionRigPrefab != null)
        {
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(interactionRigPrefab);
            rig.name = "OVRComprehensiveInteractionRig";
            rig.transform.position = Vector3.zero;
            usedInteractionRig = true;
        }
        else
        {
            // Fallback: bare OVRCameraRig (no Interaction SDK hands/rays)
            var ovrRigPrefab = FindPrefab(
                "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab",
                "OVRCameraRig");
            if (ovrRigPrefab != null)
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(ovrRigPrefab);
                rig.name = "OVRCameraRig";
                rig.transform.position = Vector3.zero;
                if (rig.GetComponent<OVRManager>() == null)
                {
                    var mgr = rig.AddComponent<OVRManager>();
                    mgr.trackingOriginType = OVRManager.TrackingOrigin.EyeLevel;
                }
            }
            else
            {
                Debug.LogWarning("[BootstrapSceneSetup] Neither OVRInteractionRig nor OVRCameraRig found. " +
                    "Drag the rig prefab into the Bootstrap scene manually.");
                var camGO = new GameObject("Main Camera [REPLACE WITH OVRInteractionRig]");
                camGO.tag = "MainCamera";
                var cam = camGO.AddComponent<Camera>();
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.08f, 0.12f);
                cam.transform.position = new Vector3(0f, 1.6f, 0f);
                camGO.AddComponent<AudioListener>();
            }
        }

        // ── EventSystem ───────────────────────────────────────────────────
        // With Interaction SDK: PointableCanvasModule handles both controller
        // rays and hand rays. Without it: fall back to OVRInputModule.
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        var pointableCanvasModuleType = System.Type.GetType(
            "Oculus.Interaction.PointableCanvasModule, Oculus.Interaction");
        if (usedInteractionRig && pointableCanvasModuleType != null)
            esGO.AddComponent(pointableCanvasModuleType);
        else
            esGO.AddComponent<OVRInputModule>();

        // ── World-space Canvas ────────────────────────────────────────────
        var canvasGO = new GameObject("BootstrapCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        // PointableCanvas bridges Interaction SDK rays/poke to Unity UI buttons.
        var pointableCanvasType = System.Type.GetType(
            "Oculus.Interaction.PointableCanvas, Oculus.Interaction");
        if (usedInteractionRig && pointableCanvasType != null)
            canvasGO.AddComponent(pointableCanvasType);
        else
            canvasGO.AddComponent<OVRRaycaster>();
        canvasGO.transform.position   = new Vector3(0f, 1.55f, 2.5f);
        canvasGO.transform.localScale = Vector3.one * 0.002f;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 600f);

        // ── Background panel ──────────────────────────────────────────────
        var bg = MakePanel(canvasGO.transform, "Background",
            new Vector2(700, 600), new Color(0.05f, 0.08f, 0.15f, 0.97f));

        // Title row
        MakeTMP(bg.transform, "TXT_Title", "Verse Rehab",
            42, new Vector2(0, 260), new Vector2(580, 60),
            new Color(0.4f, 0.8f, 1f), FontStyles.Bold);

        MakeTMP(bg.transform, "TXT_Subtitle", "Enter your prescription code",
            26, new Vector2(0, 210), new Vector2(580, 40),
            new Color(0.7f, 0.75f, 0.85f), FontStyles.Normal);

        // Settings gear button (top-right)
        var settingsToggle = MakeButton(bg.transform, "Btn_Settings", "⚙",
            new Vector2(310, 260), new Vector2(52, 52),
            new Color(0.2f, 0.25f, 0.38f));
        settingsToggle.GetComponentInChildren<TextMeshProUGUI>().fontSize = 24f;

        // PIN display
        var pinBg = MakePanel(bg.transform, "PinBG",
            new Vector2(360, 64), new Color(0.1f, 0.14f, 0.22f, 1f));
        pinBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 145);

        var pinTMP = MakeTMP(pinBg.transform, "TXT_Pin", "○ ○ ○ ○ ○",
            40, Vector2.zero, new Vector2(340, 60),
            new Color(0.9f, 0.95f, 1f), FontStyles.Normal);
        pinTMP.alignment = TextAlignmentOptions.Center;
        pinTMP.characterSpacing = 8f;

        var errorTMP = MakeTMP(bg.transform, "TXT_Error", "",
            22, new Vector2(0, 90), new Vector2(620, 36),
            new Color(1f, 0.35f, 0.35f), FontStyles.Normal);

        var statusTMP = MakeTMP(bg.transform, "TXT_Status", "Use the keypad below",
            22, new Vector2(0, 58), new Vector2(620, 36),
            new Color(0.6f, 0.7f, 0.85f), FontStyles.Italic);

        var patientTMP = MakeTMP(bg.transform, "TXT_PatientLabel", "Hi, Patient!",
            30, new Vector2(0, 100), new Vector2(620, 48),
            new Color(0.3f, 1f, 0.5f), FontStyles.Bold);
        patientTMP.gameObject.SetActive(false);

        // ── Keypad Panel ──────────────────────────────────────────────────
        var keypadPanel = new GameObject("Panel_Keypad");
        keypadPanel.transform.SetParent(bg.transform, false);
        var kpRT = keypadPanel.AddComponent<RectTransform>();
        kpRT.sizeDelta        = new Vector2(420, 280);
        kpRT.anchoredPosition = new Vector2(0, -120);

        string[][] keyLayout = new[]
        {
            new[] { "1", "2", "3" },
            new[] { "4", "5", "6" },
            new[] { "7", "8", "9" },
            new[] { "⌫", "0", "✓" },
        };

        float btnW = 110f, btnH = 58f, padX = 15f, padY = 10f;
        float totalW = 3 * btnW + 2 * padX;
        float totalH = 4 * btnH + 3 * padY;
        float startX = -totalW / 2f + btnW / 2f;
        float startY =  totalH / 2f - btnH / 2f;

        Button submitBtnRef = null;

        for (int row = 0; row < keyLayout.Length; row++)
        {
            for (int col = 0; col < keyLayout[row].Length; col++)
            {
                string label = keyLayout[row][col];
                float x = startX + col * (btnW + padX);
                float y = startY - row  * (btnH + padY);

                Color btnColor = label == "✓"
                    ? new Color(0.15f, 0.6f, 0.3f)
                    : label == "⌫"
                        ? new Color(0.5f, 0.2f, 0.15f)
                        : new Color(0.15f, 0.2f, 0.35f);

                var btn = MakeButton(keypadPanel.transform, $"Btn_{label}",
                    label, new Vector2(x, y), new Vector2(btnW, btnH), btnColor);

                var bmRef   = bm;
                var capture = label;

                if (label == "⌫")
                    btn.onClick.AddListener(() => bmRef.OnBackspace());
                else if (label == "✓")
                {
                    btn.onClick.AddListener(() => bmRef.OnSubmit());
                    submitBtnRef = btn;
                }
                else
                    btn.onClick.AddListener(() => bmRef.OnDigitPressed(capture));
            }
        }

        // ── Loading Panel ─────────────────────────────────────────────────
        var loadingPanel = MakePanel(bg.transform, "Panel_Loading",
            new Vector2(420, 280), new Color(0.05f, 0.08f, 0.15f, 0.95f));
        loadingPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
        MakeTMP(loadingPanel.transform, "TXT_Loading", "Verifying…",
            32, Vector2.zero, new Vector2(380, 60),
            new Color(0.4f, 0.8f, 1f), FontStyles.Italic);
        loadingPanel.SetActive(false);

        // ── Settings Panel ────────────────────────────────────────────────
        var settingsPanel = MakePanel(bg.transform, "Panel_Settings",
            new Vector2(600, 320), new Color(0.07f, 0.1f, 0.2f, 0.98f));
        settingsPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        MakeTMP(settingsPanel.transform, "TXT_SettingsTitle", "Server Configuration",
            30, new Vector2(0, 120), new Vector2(560, 48),
            new Color(0.4f, 0.8f, 1f), FontStyles.Bold);

        MakeTMP(settingsPanel.transform, "TXT_UrlLabel", "Backend URL",
            20, new Vector2(0, 78), new Vector2(560, 32),
            new Color(0.6f, 0.7f, 0.85f), FontStyles.Normal);

        var currentUrlLabel = MakeTMP(settingsPanel.transform, "TXT_CurrentUrl",
            "https://aastheen.onrender.com",
            18, new Vector2(0, 50), new Vector2(560, 28),
            new Color(0.5f, 0.55f, 0.7f), FontStyles.Italic);

        // TMP_InputField for URL entry (Quest virtual keyboard opens automatically)
        var urlInputField = MakeInputField(settingsPanel.transform, "Input_ServerUrl",
            "https://aastheen.onrender.com",
            new Vector2(0, 10), new Vector2(540, 52));

        // Save button
        var saveBtn = MakeButton(settingsPanel.transform, "Btn_SaveUrl", "Save",
            new Vector2(-80, -52), new Vector2(180, 52),
            new Color(0.15f, 0.55f, 0.25f));
        saveBtn.onClick.AddListener(() => bm.OnSaveServerUrl());

        // Cancel button
        var cancelBtn = MakeButton(settingsPanel.transform, "Btn_CancelSettings", "Cancel",
            new Vector2(100, -52), new Vector2(160, 52),
            new Color(0.4f, 0.18f, 0.15f));
        cancelBtn.onClick.AddListener(() => bm.OnToggleSettings());

        settingsPanel.SetActive(false);

        // Wire gear button
        settingsToggle.onClick.AddListener(() => bm.OnToggleSettings());

        // ── Wire BootstrapManager SerializeFields ─────────────────────────
        var bmSO = new SerializedObject(bm);
        bmSO.FindProperty("pinDisplay").objectReferenceValue    = pinTMP;
        bmSO.FindProperty("statusText").objectReferenceValue    = statusTMP;
        bmSO.FindProperty("errorText").objectReferenceValue     = errorTMP;
        bmSO.FindProperty("patientLabel").objectReferenceValue  = patientTMP;
        bmSO.FindProperty("submitButton").objectReferenceValue  = submitBtnRef;
        bmSO.FindProperty("keypadPanel").objectReferenceValue   = keypadPanel;
        bmSO.FindProperty("loadingPanel").objectReferenceValue  = loadingPanel;
        bmSO.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        bmSO.FindProperty("serverUrlInput").objectReferenceValue = urlInputField;
        bmSO.FindProperty("currentUrlLabel").objectReferenceValue = currentUrlLabel;
        bmSO.ApplyModifiedPropertiesWithoutUndo();

        // ── Save scene ────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorSceneManager.CloseScene(scene, false);

        // ── Add to Build Settings ─────────────────────────────────────────
        var scenes = EditorBuildSettings.scenes;
        bool bootstrapInBuild = false, balloonInBuild = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)         bootstrapInBuild = true;
            if (s.path.Contains("ballooon")) balloonInBuild   = true;
        }
        var newScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
        if (!bootstrapInBuild)
            newScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        if (!balloonInBuild)
            newScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/ballooon.unity", true));
        EditorBuildSettings.scenes = newScenes.ToArray();

        AssetDatabase.Refresh();

        string rigStatus = usedInteractionRig
            ? "✓ OVRInteractionRig (controllers + hands + poke)"
            : "⚠ Fell back to OVRCameraRig (no Interaction SDK hands)";
        string inputStatus = usedInteractionRig && pointableCanvasModuleType != null
            ? "✓ PointableCanvasModule (controller ray + hand ray + poke)"
            : "⚠ OVRInputModule (controller ray only)";
        EditorUtility.DisplayDialog("Bootstrap Scene Created",
            $"Saved to: {scenePath}\n\n" +
            $"Rig:   {rigStatus}\n" +
            $"Input: {inputStatus}\n\n" +
            "Build Settings updated:\n" +
            "  [0] Bootstrap\n" +
            "  [1] ballooon\n\n" +
            "Server URL: runtime-configurable via ⚙ button.",
            "OK");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
    }

    // ── Prefab finder: try known path first, then project-wide search ─────────

    static GameObject FindPrefab(string knownPath, string prefabName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(knownPath);
        if (prefab != null) return prefab;

        foreach (var guid in AssetDatabase.FindAssets($"{prefabName} t:Prefab"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith($"/{prefabName}.prefab"))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    // ── UI factory helpers ─────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, string name, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float fontSize, Vector2 pos, Vector2 size, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static TMP_InputField MakeInputField(Transform parent, string name,
        string placeholder, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.16f, 0.26f, 1f);

        var field = go.AddComponent<TMP_InputField>();

        // Viewport
        var viewportGO = new GameObject("Text Area");
        viewportGO.transform.SetParent(go.transform, false);
        viewportGO.AddComponent<RectMask2D>();
        var vrt = viewportGO.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(8, 4);
        vrt.offsetMax = new Vector2(-8, -4);

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(viewportGO.transform, false);
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text      = "";
        textTMP.fontSize  = 22f;
        textTMP.color     = Color.white;
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(viewportGO.transform, false);
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text      = placeholder;
        phTMP.fontSize  = 22f;
        phTMP.color     = new Color(0.5f, 0.55f, 0.65f, 0.6f);
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var phrt = phGO.GetComponent<RectTransform>();
        phrt.anchorMin = Vector2.zero;
        phrt.anchorMax = Vector2.one;
        phrt.offsetMin = phrt.offsetMax = Vector2.zero;

        field.textViewport   = vrt;
        field.textComponent  = textTMP;
        field.placeholder    = phTMP;
        field.contentType    = TMP_InputField.ContentType.Standard;
        field.lineType        = TMP_InputField.LineType.SingleLine;

        return field;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        btn.colors = new ColorBlock
        {
            normalColor      = bgColor,
            highlightedColor = bgColor * 1.3f,
            pressedColor     = bgColor * 0.7f,
            selectedColor    = bgColor,
            disabledColor    = bgColor * 0.5f,
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.sizeDelta = size;
        txtRT.anchoredPosition = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = label.Length > 1 ? 22f : 28f;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
