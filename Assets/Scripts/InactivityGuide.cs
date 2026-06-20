using UnityEngine;

// Attach alongside GameManager.
// After inactivityTimeout seconds with an active target but no correct pop,
// shows the GhostHandGuide pointing at the current balloon.
public class InactivityGuide : MonoBehaviour
{
    public static InactivityGuide Instance { get; private set; }

    [Header("Settings")]
    public float inactivityTimeout  = 15f;   // seconds — overridden by prescription
    public bool  guidedModeEnabled  = true;  // false = fully disabled

    [Header("Ghost Visual")]
    public GhostHandGuide ghostHand;          // scene instance, assigned in Inspector

    private bool      _sessionActive;
    private float     _timer;
    private bool      _guideVisible;
    private Transform _currentTarget;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ── Session lifecycle ──────────────────────────────────────────────────────

    public void OnSessionStart()
    {
        _sessionActive = true;
        _timer         = 0f;
        _guideVisible  = false;
    }

    public void OnSessionEnd()
    {
        _sessionActive = false;
        HideGuide();
    }

    // ── Target events (called by BalloonSpawner / Balloon) ────────────────────

    public void OnNewTarget(Transform balloonTransform)
    {
        _currentTarget = balloonTransform;
        _timer         = 0f;
        HideGuide();
        ghostHand?.SetTarget(balloonTransform);
    }

    public void OnTargetGone()
    {
        _currentTarget = null;
        _timer         = 0f;
        HideGuide();
    }

    // ── Correct action (called by GameManager.OnTargetPopped) ─────────────────

    public void NotifyCorrectPop()
    {
        _timer         = 0f;
        _currentTarget = null;
        HideGuide();
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!_sessionActive || !guidedModeEnabled || _currentTarget == null) return;

        _timer += Time.deltaTime;
        if (!_guideVisible && _timer >= inactivityTimeout)
            ShowGuide();
    }

    // ── Internal ───────────────────────────────────────────────────────────────

    void ShowGuide()
    {
        if (_guideVisible || ghostHand == null) return;
        _guideVisible = true;
        ghostHand.Show();
    }

    void HideGuide()
    {
        if (!_guideVisible) return;
        _guideVisible = false;
        ghostHand?.Hide();
    }
}
