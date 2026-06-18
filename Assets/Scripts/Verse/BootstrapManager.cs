using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class BootstrapManager : MonoBehaviour
{
    [Header("PIN UI")]
    [SerializeField] private TMP_Text   pinDisplay;
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private TMP_Text   errorText;
    [SerializeField] private TMP_Text   patientLabel;
    [SerializeField] private Button     submitButton;
    [SerializeField] private GameObject keypadPanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Settings UI")]
    [SerializeField] private GameObject    settingsPanel;   // hidden by default
    [SerializeField] private TMP_InputField serverUrlInput;  // Quest virtual keyboard
    [SerializeField] private TMP_Text       currentUrlLabel; // shows active URL

    private string _pin  = "";
    private bool   _busy = false;

    private void Awake()
    {
        FixConflictingInteractionComponents();
        FixWorldSpaceCanvasCamera();
        AddControllerHelpers();
        FixInputModuleRayTransform();
    }

    // OVRComprehensiveInteractionRig provides the ray/poke interactors that drive
    // PointableCanvasModule. Basic OVRCameraRig does NOT include these.
    private static bool HasInteractionSdkRig() =>
        GameObject.Find("OVRComprehensiveInteractionRig") != null ||
        GameObject.Find("OVRInteractionRig") != null;

    // Resolves input module + canvas raycaster mismatches at startup.
    // Based on Meta's FlatUnityCanvas reference prefab (sdk.interaction v203):
    //   - PointableCanvas + GraphicRaycaster are required together on each canvas
    //   - Rigidbody/SphereCollider are NOT used for ray interaction (only poke needs PlaneSurface+PokeInteractable)
    //   - PointableCanvasModule on EventSystem drives all PointableCanvases
    //   - Fallback: if no Interaction SDK rig, use OVRInputModule + OVRRaycaster
    private void FixConflictingInteractionComponents()
    {
        bool hasInteractionRig = HasInteractionSdkRig();
        var pointableCanvasType = System.Type.GetType("Oculus.Interaction.PointableCanvas, Oculus.Interaction");
        bool hasPointableCanvasModule = FindFirstObjectByType<UnityEngine.EventSystems.BaseInputModule>()
            ?.GetType().FullName == "Oculus.Interaction.PointableCanvasModule";

        if (!hasInteractionRig && hasPointableCanvasModule)
        {
            // OVRCameraRig-only scene: PointableCanvasModule has no interactors to drive it.
            // Fall back to OVRInputModule which works with basic controller ray from OVRCameraRig.
            var pcm  = FindFirstObjectByType<UnityEngine.EventSystems.BaseInputModule>();
            var esGO = pcm.gameObject;
            Destroy(pcm);
            if (esGO.GetComponent<OVRInputModule>() == null)
                esGO.AddComponent<OVRInputModule>();
            hasPointableCanvasModule = false;
            Debug.Log("[Bootstrap] Swapped PointableCanvasModule → OVRInputModule (no Interaction SDK rig)");
        }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            var ovrRaycaster    = canvas.GetComponent<OVRRaycaster>();
            var pointableCanvas = pointableCanvasType != null
                ? canvas.GetComponent(pointableCanvasType) : null;

            if (pointableCanvas != null)
            {
                // Rigidbody+SphereCollider are incorrect for PointableCanvas interaction.
                // Ray interaction needs no physics; poke needs PokeInteractable+PlaneSurface instead.
                var rb = canvas.GetComponent<Rigidbody>();     if (rb  != null) Destroy(rb);
                var sc = canvas.GetComponent<SphereCollider>(); if (sc != null) Destroy(sc);

                if (!hasInteractionRig)
                {
                    // No Interaction SDK rig: swap PointableCanvas → OVRRaycaster
                    Destroy(pointableCanvas);
                    if (canvas.GetComponent<OVRRaycaster>() == null)
                        canvas.gameObject.AddComponent<OVRRaycaster>();
                    Debug.Log($"[Bootstrap] Swapped PointableCanvas → OVRRaycaster on '{canvas.name}'");
                }
                else
                {
                    // Has Interaction SDK rig: ensure GraphicRaycaster is present
                    // (PointableCanvasModule requires it to resolve ray hit → UI element).
                    // OVRRaycaster conflicts with PointableCanvas → replace with plain GraphicRaycaster.
                    if (ovrRaycaster != null)
                    {
                        if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null
                            || canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() is OVRRaycaster)
                            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                        Destroy(ovrRaycaster);
                        Debug.Log($"[Bootstrap] Swapped OVRRaycaster → GraphicRaycaster on '{canvas.name}'");
                    }
                    else if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    {
                        canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                        Debug.Log($"[Bootstrap] Added missing GraphicRaycaster to '{canvas.name}'");
                    }
                }
            }
        }

        // Strip OVRInputModule only if PointableCanvasModule is legitimately driving
        var ovrInput = FindFirstObjectByType<OVRInputModule>();
        if (ovrInput != null && hasPointableCanvasModule)
        {
            Debug.Log("[Bootstrap] Removing OVRInputModule (PointableCanvasModule is active)");
            Destroy(ovrInput);
        }
    }

    // World-space canvas needs a camera; OVRCameraRig's center eye isn't tagged MainCamera.
    private void FixWorldSpaceCanvasCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var anchor = GameObject.Find("CenterEyeAnchor");
            if (anchor != null) cam = anchor.GetComponent<Camera>();
        }
        if (cam == null) return;
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                canvas.worldCamera = cam;
        }
    }

    // Add controller 3D model rendering to left/right anchors.
    private void AddControllerHelpers()
    {
        AddHelper("LeftControllerAnchor",  OVRInput.Controller.LTouch);
        AddHelper("RightControllerAnchor", OVRInput.Controller.RTouch);
    }

    private void AddHelper(string anchorName, OVRInput.Controller controller)
    {
        var go = GameObject.Find(anchorName);
        if (go == null || go.GetComponent<OVRControllerHelper>() != null) return;
        var helper = go.AddComponent<OVRControllerHelper>();
        helper.m_controller = controller;
    }

    private void FixInputModuleRayTransform()
    {
        // Only runs if OVRInputModule survived (i.e. no PointableCanvasModule present).
        var inputModule = FindFirstObjectByType<OVRInputModule>();
        if (inputModule == null) return;

        inputModule.joyPadClickButton = OVRInput.Button.PrimaryIndexTrigger;

        if (inputModule.rayTransform != null) return;
        foreach (var name in new[] { "RightControllerAnchor", "RightHandAnchor" })
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                inputModule.rayTransform = go.transform;
                Debug.Log($"[Bootstrap] OVRInputModule.rayTransform → {name}, click → IndexTrigger");
                return;
            }
        }
        Debug.LogWarning("[Bootstrap] No anchor found for OVRInputModule.rayTransform");
    }


    private void Start()
    {
        WireButtons();
        SetLoading(false);
        SetError("");
        if (patientLabel) patientLabel.gameObject.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        UpdatePinDisplay();
        RefreshUrlLabel();

        if (VerseClient.Instance == null)
            Debug.LogError("[Bootstrap] VerseClient not found — add it to the Bootstrap scene.");
        if (PlaylistManager.Instance == null)
            Debug.LogError("[Bootstrap] PlaylistManager not found — add it to the Bootstrap scene.");
    }

    // Buttons were created without onClick assignments — wire them all at runtime.
    private void WireButtons()
    {
        for (int i = 0; i <= 9; i++)
        {
            string digit = i.ToString();
            var go = GameObject.Find($"Btn_{digit}");
            if (go == null) { Debug.LogWarning($"[Bootstrap] Btn_{digit} not found"); continue; }
            var btn = go.GetComponent<UnityEngine.UI.Button>();
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnDigitPressed(digit));
        }

        WireBtn("Btn_⌫", OnBackspace);     // ⌫ backspace
        WireBtn("Btn_✓", OnSubmit);         // ✓ submit

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmit);
        }

        WireBtn("Btn_Settings",        OnToggleSettings);
        WireBtn("Btn_SaveUrl",         OnSaveServerUrl);
        WireBtn("Btn_CancelSettings",  OnToggleSettings);

        Debug.Log("[Bootstrap] All keypad buttons wired.");
    }

    private void WireBtn(string goName, UnityEngine.Events.UnityAction action)
    {
        var go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[Bootstrap] {goName} not found"); return; }
        var btn = go.GetComponent<UnityEngine.UI.Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    // ── Called by digit buttons (Button.onClick → OnDigitPressed("3")) ────────

    public void OnDigitPressed(string digit)
    {
        if (_busy || _pin.Length >= 5) return;
        _pin += digit;
        UpdatePinDisplay();
        SetError("");
    }

    public void OnBackspace()
    {
        if (_busy || _pin.Length == 0) return;
        _pin = _pin[..^1];
        UpdatePinDisplay();
        SetError("");
    }

    public void OnSubmit()
    {
        if (_busy) return;
        if (_pin.Length != 5) { SetError("Enter a 5-digit code."); return; }

        if (VerseClient.Instance == null)
        {
            SetError("VerseClient missing — rebuild required.");
            Debug.LogError("[Bootstrap] VerseClient.Instance is null on submit.");
            return;
        }

        _busy = true;
        SetLoading(true);
        SetError("");

        VerseClient.Instance.VerifyCode(_pin,
            onSuccess: response =>
            {
                PlaylistManager.Instance.Set(response);

                // Nurse approval deferred — proceed regardless of status for now.
                // When clinic mode is implemented, poll GET /sessions/{id}/status here.
                if (response.status == "pending_approval")
                    Debug.Log("[Bootstrap] Nurse approval required — skipped for now, proceeding.");

                ShowPatientAndLoad(response);
            },
            onError: err =>
            {
                _busy = false;
                SetLoading(false);
                SetError("Invalid code. Try again.");
                Debug.LogWarning("[Bootstrap] Verify failed: " + err);
                _pin = "";
                UpdatePinDisplay();
            });
    }

    private void ShowPatientAndLoad(VerifyCodeResponse response)
    {
        SetLoading(false);
        if (patientLabel)
        {
            patientLabel.gameObject.SetActive(true);
            patientLabel.text = $"Hi, {response.prescription?.patient_name ?? "Patient"}";
        }

        string gameLabel = response.prescription?.game_name ?? "game";
        if (statusText) statusText.text = $"Loading {gameLabel}…";

        Invoke(nameof(LoadGameScene), 1.5f);
    }

    private void LoadGameScene()
    {
        // For single-game prescriptions GameId is already set by PlaylistManager.Set().
        // For multi-game, AdvanceToNextGame() was already called inside Set(), so
        // GameId reflects the first game in the playlist.
        string sceneName = PlaylistManager.GameIdToSceneName(PlaylistManager.Instance.GameId);
        Debug.Log($"[Bootstrap] Loading scene: {sceneName} (game_id: {PlaylistManager.Instance.GameId})");
        SceneManager.LoadScene(sceneName);
    }

    // ── Settings panel ───────────────────────────────────────────────────────

    public void OnToggleSettings()
    {
        if (!settingsPanel) return;
        bool show = !settingsPanel.activeSelf;
        settingsPanel.SetActive(show);
        if (show && serverUrlInput && VerseClient.Instance != null)
            serverUrlInput.text = VerseClient.Instance.BaseUrl;
    }

    public void OnSaveServerUrl()
    {
        if (serverUrlInput == null || VerseClient.Instance == null) return;
        string url = serverUrlInput.text.Trim();
        if (string.IsNullOrEmpty(url)) return;
        VerseClient.Instance.SetAndSaveBaseUrl(url);
        RefreshUrlLabel();
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void RefreshUrlLabel()
    {
        if (currentUrlLabel == null || VerseClient.Instance == null) return;
        currentUrlLabel.text = VerseClient.Instance.BaseUrl;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void UpdatePinDisplay()
    {
        if (!pinDisplay) return;
        string dots = new string('●', _pin.Length).PadRight(5, '○');
        // Format: ● ● ○ ○ ○
        pinDisplay.text = string.Join(" ", dots.ToCharArray());
    }

    private void SetLoading(bool loading)
    {
        if (loadingPanel) loadingPanel.SetActive(loading);
        if (keypadPanel)  keypadPanel.SetActive(!loading);
        if (submitButton) submitButton.interactable = !loading;
    }

    private void SetError(string msg)
    {
        if (errorText) errorText.text = msg;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Verse/Bootstrap - Simulate Verify (Editor)")]
    static void EditorSimulateVerify()
    {
        // Lets you test GameManager in-editor without a real network call.
        // Creates a fake session so GameManager auto-starts.
        var fake = new VerifyCodeResponse
        {
            session_id    = 999,
            session_token = "editor_test_token",
            status        = "active",
            prescription  = new PrescriptionPublic
            {
                game_id      = "balloon",
                game_name    = "Balloon Pop",
                patient_name = "Test Patient",
                targets      = new BalloonSettings
                {
                    repCount = 10, sessionDuration = 60f,
                    spawnInterval = 2f, balloonSize = 0.3f,
                    gameMode = "standard"
                }
            }
        };
        if (PlaylistManager.Instance != null)
            PlaylistManager.Instance.Set(fake);
        else
            Debug.LogWarning("[Verse] PlaylistManager not found — enter Play Mode first.");
    }
#endif
}
