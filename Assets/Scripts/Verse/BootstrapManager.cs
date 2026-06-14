using UnityEngine;
using UnityEngine.SceneManagement;
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
        FixWorldSpaceCanvasCamera();
        AddControllerHelpers();
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

    private void Start()
    {
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
        if (statusText)
            statusText.text = $"Loading {response.prescription?.game_name ?? "game"}…";

        Invoke(nameof(LoadGameScene), 1.5f);
    }

    private void LoadGameScene()
    {
        string sceneName = PlaylistManager.GameIdToSceneName(PlaylistManager.Instance.GameId);
        Debug.Log($"[Bootstrap] Loading scene: {sceneName}");
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
