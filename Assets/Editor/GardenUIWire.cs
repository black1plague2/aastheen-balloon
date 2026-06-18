// Editor-only helper — run via Garden menu or script-execute
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public static class GardenUIWire
{
    // ── helpers ─────────────────────────────────────────────────────────────

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float fs, TextAlignmentOptions align, Color color, bool italic = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fs;
        tmp.alignment = align;
        tmp.color     = color;
        if (italic) tmp.fontStyle = FontStyles.Italic;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static void RT(Component c, Vector2 pos, Vector2 size)
    {
        var rt = c.GetComponent<RectTransform>();
        if (rt == null) rt = c.gameObject.AddComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    // ── Phase 1 — patch RehabCanvas task panel ───────────────────────────────

    [MenuItem("Garden/1 Wire TaskPanel")]
    public static void WireTaskPanel()
    {
        var rc = GameObject.Find("RehabCanvas");
        if (rc == null) { Debug.LogError("RehabCanvas not found"); return; }

        // Fix world-space canvas scale (was 0,0,0 → invisible)
        if (rc.transform.localScale == Vector3.zero)
            rc.transform.localScale = Vector3.one * 0.001f;

        var tiui = rc.GetComponentInChildren<TaskInstructionUI>(true);
        if (tiui == null) { Debug.LogError("TaskInstructionUI not found"); return; }

        var tp   = tiui.taskPanel;
        var hold = tiui.holdProgressRoot;

        // Darken panel background
        var bg = tp.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);

        // txt_TaskBadge — repurpose TXT_TaskNumber
        var txNum = tp.transform.Find("TXT_TaskNumber");
        if (txNum != null)
        {
            var tmp = txNum.GetComponent<TextMeshProUGUI>();
            tmp.fontSize  = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = new Color(0.75f, 0.92f, 1f);
            tiui.txt_TaskBadge = tmp;
        }

        // img_TaskBadgeBG — translucent strip behind badge
        if (tiui.img_TaskBadgeBG == null)
        {
            var img = MakeImage(tp.transform, "IMG_BadgeBG", new Color(0.3f, 0.75f, 1f, 0.15f));
            img.transform.SetSiblingIndex(0);
            RT(img, new Vector2(0, 160), new Vector2(580, 54));
            tiui.img_TaskBadgeBG = img;
        }

        // TXT_PlantName — bigger, bold white
        var txPlant = tp.transform.Find("TXT_PlantName");
        if (txPlant != null)
        {
            var tmp = txPlant.GetComponent<TextMeshProUGUI>();
            tmp.fontSize  = 38;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.white;
        }

        // TXT_Action — expand height for 2-line text
        var txAct = tp.transform.Find("TXT_Action");
        if (txAct != null)
        {
            RT(txAct, new Vector2(0, 15), new Vector2(540, 80));
            var tmp = txAct.GetComponent<TextMeshProUGUI>();
            tmp.fontSize           = 22;
            tmp.enableWordWrapping = true;
            tmp.color              = new Color(0.95f, 0.95f, 0.95f);
        }

        // TXT_ToolName — move down slightly, warm yellow
        var txTool = tp.transform.Find("TXT_ToolName");
        if (txTool != null)
        {
            RT(txTool, new Vector2(18, -60), new Vector2(520, 40));
            var tmp = txTool.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 22;
            tmp.color    = new Color(1f, 0.88f, 0.4f);
        }

        // img_ToolColor — coloured square swatch left of tool name
        if (tiui.img_ToolColor == null)
        {
            var img = MakeImage(tp.transform, "IMG_ToolColor", new Color(0.2f, 0.6f, 1f));
            RT(img, new Vector2(-270, -60), new Vector2(22, 22));
            tiui.img_ToolColor = img;
        }

        // TXT_RehabTip — italic accent text below tool
        if (tiui.txt_RehabTip == null)
        {
            var tmp = MakeTMP(tp.transform, "TXT_RehabTip",
                "Exercises: forearm supination & shoulder reach",
                15f, TextAlignmentOptions.Center, new Color(0.4f, 0.85f, 1f, 0.88f), italic: true);
            RT(tmp, new Vector2(0, -100), new Vector2(540, 32));
            tiui.txt_RehabTip = tmp;
        }

        // TXT_MemoryHint — push down
        var txMem = tp.transform.Find("TXT_MemoryHint");
        if (txMem != null) RT(txMem, new Vector2(0, -125), new Vector2(540, 36));

        // TXT_WrongTool — push down
        var txWrong = tp.transform.Find("TXT_WrongTool");
        if (txWrong != null)
        {
            RT(txWrong, new Vector2(0, -165), new Vector2(540, 62));
            var tmp = txWrong.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 19;
        }

        // HoldProgressRoot — push down, add TXT_HoldPrompt above bar
        if (hold != null)
        {
            RT(hold.transform, new Vector2(0, -148), new Vector2(500, 30));

            if (tiui.txt_HoldPrompt == null)
            {
                var tmp = MakeTMP(hold.transform, "TXT_HoldPrompt",
                    "Hold steady...", 17f, TextAlignmentOptions.Center, Color.white);
                RT(tmp, new Vector2(0, 26), new Vector2(500, 28));
                tiui.txt_HoldPrompt = tmp;
            }
        }

        tiui.wrongFlashTime = 2.2f;

        EditorUtility.SetDirty(tiui);
        EditorUtility.SetDirty(rc);
        Debug.Log("[GardenUIWire] TaskPanel wiring complete.");
    }

    // ── Phase 2 — patch CompletePanel ────────────────────────────────────────

    [MenuItem("Garden/2 Wire CompletePanel")]
    public static void WireCompletePanel()
    {
        var rc = GameObject.Find("RehabCanvas");
        if (rc == null) { Debug.LogError("RehabCanvas not found"); return; }

        var tiui = rc.GetComponentInChildren<TaskInstructionUI>(true);
        if (tiui == null) { Debug.LogError("TaskInstructionUI not found"); return; }

        var cp = tiui.completePanel;

        // Darken complete panel bg
        var bg = cp.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.04f, 0.10f, 0.08f, 0.92f);

        // Repurpose TXT_Complete as the message text (wire txt_CompleteMessage)
        var txComplete = cp.transform.Find("TXT_Complete");
        if (txComplete != null)
        {
            RT(txComplete, new Vector2(0, -20), new Vector2(540, 130));
            var tmp = txComplete.GetComponent<TextMeshProUGUI>();
            tmp.fontSize  = 22;
            tmp.color     = new Color(0.92f, 0.92f, 0.92f);
            tmp.alignment = TextAlignmentOptions.Center;
            tiui.txt_CompleteMessage = tmp;
        }

        // Add TXT_CompleteHeading above message
        if (tiui.txt_CompleteHeading == null)
        {
            var tmp = MakeTMP(cp.transform, "TXT_CompleteHeading",
                "Session Complete!", 38f, TextAlignmentOptions.Center,
                new Color(0.2f, 1f, 0.5f));
            tmp.fontStyle = FontStyles.Bold;
            RT(tmp, new Vector2(0, 130), new Vector2(540, 80));
            tiui.txt_CompleteHeading = tmp;
        }

        // Add TXT_NextSession below message
        if (tiui.txt_NextSession == null)
        {
            var tmp = MakeTMP(cp.transform, "TXT_NextSession",
                "Next session: 3 tasks", 19f, TextAlignmentOptions.Center,
                new Color(0.4f, 0.85f, 1f));
            RT(tmp, new Vector2(0, -130), new Vector2(540, 40));
            tiui.txt_NextSession = tmp;
        }

        // Add a subtle accent bar at top of complete panel
        var existAccent = cp.transform.Find("IMG_Accent");
        if (existAccent == null)
        {
            var img = MakeImage(cp.transform, "IMG_Accent", new Color(0.2f, 1f, 0.5f, 0.4f));
            img.transform.SetSiblingIndex(0);
            RT(img, new Vector2(0, 185), new Vector2(600, 8));
        }

        EditorUtility.SetDirty(tiui);
        Debug.Log("[GardenUIWire] CompletePanel wiring complete.");
    }

    // ── Phase 3 — create IntroCanvas ─────────────────────────────────────────

    [MenuItem("Garden/3 Create IntroCanvas")]
    public static void CreateIntroCanvas()
    {
        // Don't duplicate
        if (GameObject.Find("IntroCanvas") != null)
        {
            Debug.LogWarning("[GardenUIWire] IntroCanvas already exists.");
            return;
        }

        var rc = GameObject.Find("RehabCanvas");
        Vector3 refPos   = rc != null ? rc.transform.position   : new Vector3(0, 1.6f, -3.2f);
        float   refScale = rc != null ? rc.transform.localScale.x : 0.001f;

        // ── Root Canvas ──────────────────────────────────────────────────────
        var canvasGO = new GameObject("IntroCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode         = RenderMode.WorldSpace;
        canvas.sortingOrder       = 1;  // in front of RehabCanvas
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.transform.position   = refPos;
        canvasGO.transform.rotation   = rc != null ? rc.transform.rotation : Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * refScale;

        var canvasRT      = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(620, 440);

        // Add GameIntroUI component
        var introUI = canvasGO.AddComponent<GameIntroUI>();

        // ── IntroPanel (dark bg) ─────────────────────────────────────────────
        var panelGO = new GameObject("IntroPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
        var panelRT    = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        introUI.introPanel = panelGO;

        // ── Accent bar (top coloured strip) ──────────────────────────────────
        var accentImg = MakeImage(panelGO.transform, "IMG_AccentBar", new Color(0.4f, 0.85f, 1f));
        RT(accentImg, new Vector2(0, 205), new Vector2(620, 8));
        introUI.img_AccentBar = accentImg;

        // ── TXT_Counter (top right) ───────────────────────────────────────────
        var txCounter = MakeTMP(panelGO.transform, "TXT_Counter",
            "1 / 6", 16f, TextAlignmentOptions.Right, new Color(0.6f, 0.8f, 1f, 0.7f));
        RT(txCounter, new Vector2(270, 185), new Vector2(120, 28));
        introUI.txt_Counter = txCounter;

        // ── TXT_Title ─────────────────────────────────────────────────────────
        var txTitle = MakeTMP(panelGO.transform, "TXT_Title",
            "Welcome to Garden Therapy", 32f, TextAlignmentOptions.Center, Color.white);
        txTitle.fontStyle = FontStyles.Bold;
        RT(txTitle, new Vector2(0, 140), new Vector2(560, 70));
        introUI.txt_Title = txTitle;

        // ── Divider line ──────────────────────────────────────────────────────
        var divider = MakeImage(panelGO.transform, "IMG_Divider", new Color(1f, 1f, 1f, 0.12f));
        RT(divider, new Vector2(0, 105), new Vector2(540, 2));

        // ── TXT_Body ─────────────────────────────────────────────────────────
        var txBody = MakeTMP(panelGO.transform, "TXT_Body",
            "In this session you will care for plants using gardening tools.",
            20f, TextAlignmentOptions.Center, new Color(0.88f, 0.92f, 1f));
        txBody.enableWordWrapping = true;
        RT(txBody, new Vector2(0, -20), new Vector2(560, 230));
        introUI.txt_Body = txBody;

        // ── Dots root (horizontal row) ────────────────────────────────────────
        var dotsGO = new GameObject("DotsRoot");
        dotsGO.transform.SetParent(panelGO.transform, false);
        var dotsRT = dotsGO.AddComponent<RectTransform>();
        dotsRT.anchorMin = dotsRT.anchorMax = new Vector2(0.5f, 0.5f);
        dotsRT.anchoredPosition = new Vector2(0, -150);
        dotsRT.sizeDelta        = new Vector2(200, 20);
        var hLayout = dotsGO.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing         = 10;
        hLayout.childAlignment  = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth  = false;
        hLayout.childForceExpandHeight = false;
        introUI.dotsRoot = dotsGO.transform;

        // Dot prefab — small circle Image (we'll create one dot as prefab reference)
        var dotGO = new GameObject("Dot");
        dotGO.transform.SetParent(dotsGO.transform, false);
        var dotImg = dotGO.AddComponent<Image>();
        dotImg.color = new Color(1f, 0.85f, 0.2f);
        var dotRT   = dotGO.GetComponent<RectTransform>();
        dotRT.sizeDelta = new Vector2(14, 14);
        introUI.dotPrefab = dotGO;

        // ── Navigation buttons row ────────────────────────────────────────────
        var btnRow = new GameObject("NavButtons");
        btnRow.transform.SetParent(panelGO.transform, false);
        var btnRowRT = btnRow.AddComponent<RectTransform>();
        btnRowRT.anchorMin = btnRowRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRowRT.anchoredPosition = new Vector2(0, -188);
        btnRowRT.sizeDelta        = new Vector2(560, 50);
        var hBtns = btnRow.AddComponent<HorizontalLayoutGroup>();
        hBtns.spacing = 16;
        hBtns.childAlignment = TextAnchor.MiddleCenter;
        hBtns.childForceExpandWidth  = false;
        hBtns.childForceExpandHeight = false;

        introUI.btn_Back  = MakeButton(btnRow.transform, "BTN_Back",  "< Back",
            new Color(0.2f, 0.3f, 0.45f), new Vector2(140, 46));
        introUI.btn_Next  = MakeButton(btnRow.transform, "BTN_Next",  "Next >",
            new Color(0.2f, 0.5f, 0.85f), new Vector2(140, 46));
        introUI.btn_Start = MakeButton(btnRow.transform, "BTN_Start", "START",
            new Color(0.1f, 0.7f, 0.35f), new Vector2(180, 46));

        introUI.btn_Back.gameObject.SetActive(false);
        introUI.btn_Start.gameObject.SetActive(false);

        // Wire accent colors
        introUI.accentColors = new Color[]
        {
            new Color(0.40f, 0.85f, 1.00f),
            new Color(0.50f, 1.00f, 0.60f),
            new Color(1.00f, 0.85f, 0.30f),
            new Color(0.55f, 0.85f, 1.00f),
            new Color(1.00f, 0.60f, 0.30f),
            new Color(0.35f, 1.00f, 0.80f),
        };

        EditorUtility.SetDirty(canvasGO);
        Debug.Log("[GardenUIWire] IntroCanvas created and wired.");
    }

    static Button MakeButton(Transform parent, string name, string label, Color bgColor, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        // Size via layout element
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = size.x;
        le.preferredHeight = size.y;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        var lRT = labelGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;

        return btn;
    }

    // ── Phase 4 — run all three in sequence ──────────────────────────────────

    [MenuItem("Garden/Wire All UI (run this)")]
    public static void WireAll()
    {
        WireTaskPanel();
        WireCompletePanel();
        CreateIntroCanvas();

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[GardenUIWire] ALL wiring complete — save the scene (Ctrl+S).");
    }
}
#endif
