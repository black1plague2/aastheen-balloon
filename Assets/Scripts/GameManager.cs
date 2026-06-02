using System;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public TMP_Text scoreText;
    public TMP_Text repsText;
    public TMP_Text warningText;

    [Header("Scene References")]
    public BalloonSpawner balloonSpawner;

    // Prescription — defaults used until backend sends a prescription message
    [HideInInspector] public float targetRotation  = 60f;
    [HideInInspector] public float holdTimeMs      = 1000f;
    [HideInInspector] public int   repCount        = 10;
    [HideInInspector] public float balloonSize     = 0.3f;
    [HideInInspector] public float spawnInterval   = 3f;
    [HideInInspector] public float sessionDuration = 120f;

    // Live sensor values — read every frame by Balloon.cs and PinRotationDriver.cs
    [HideInInspector] public float latestRotation = 0f;
    [HideInInspector] public float latestSpeed    = 0f;

    // Session state
    private int   score          = 0;
    private int   repsCompleted  = 0;
    private bool  sessionActive  = false;
    private float sessionTimer   = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!sessionActive) return;
        sessionTimer -= Time.deltaTime;
        UpdateUI();
        if (sessionTimer <= 0f)
            EndSession();
    }

    // ── Called by WSClient ─────────────────────────────────────────────────────

    // Stores prescription params — does NOT auto-start the session.
    // The doctor must send a separate "start" command to begin.
    public void OnPrescriptionReceived(float tRot, float holdMs, int reps,
        float bSize, float spawnInt, float sessDur)
    {
        targetRotation  = tRot;
        holdTimeMs      = holdMs;
        repCount        = reps;
        balloonSize     = bSize;
        spawnInterval   = spawnInt;
        sessionDuration = sessDur;
        Debug.Log($"[GameManager] Prescription stored — reps:{reps}  dur:{sessDur}s  spawnEvery:{spawnInt}s");
    }

    // Handles "start" | "stop" | "pause" | "reset" — called by WSClient + editor test menu
    public void OnCommand(string command)
    {
        switch (command.ToLower())
        {
            case "start":
                if (!sessionActive) StartSession();
                break;
            case "stop":
                if (sessionActive) EndSession();
                break;
            case "pause":
                sessionActive = !sessionActive;
                Debug.Log($"[GameManager] Paused={!sessionActive}");
                break;
            case "reset":
                EndSession();
                score = 0; repsCompleted = 0;
                UpdateUI();
                break;
        }
    }

    // Called by WSClient every time a sensor frame arrives
    public void OnSensorData(float rotation, float speed, string warning)
    {
        latestRotation = rotation;
        latestSpeed    = speed;

        if (warningText == null) return;
        warningText.text = string.IsNullOrEmpty(warning) ? "" : warning;
    }

    // ── Session flow ───────────────────────────────────────────────────────────

    private void StartSession()
    {
        score         = 0;
        repsCompleted = 0;
        sessionTimer  = sessionDuration;
        sessionActive = true;
        UpdateUI();
        if (balloonSpawner != null) balloonSpawner.StartSpawning();
        Debug.Log($"[GameManager] Session started — reps:{repCount}  duration:{sessionDuration}s");
    }

    // Called by Balloon.cs on a successful pop
    public void OnRepCompleted(float rotationAchieved, float heldMs)
    {
        repsCompleted++;
        score += 100;
        UpdateUI();
        WSClient.Instance?.SendRepDone(score, rotationAchieved, heldMs);
        Debug.Log($"[GameManager] Rep {repsCompleted} complete.");
        if (repsCompleted >= repCount) EndSession();
    }

    private void EndSession()
    {
        sessionActive = false;
        if (balloonSpawner != null) balloonSpawner.StopSpawning();
        if (warningText   != null) warningText.text = "Session Complete!  Score: " + score;
        Debug.Log($"[GameManager] Session ended — score:{score}");
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (repsText  != null) repsText.text  = $"Reps: {repsCompleted} / {repCount}";
    }

    // ── Editor-only test menu (Test → ... in Unity menu bar) ─────────────────
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Test/1 - Send Prescription")]
    static void EditorSendPrescription()
    {
        if (Instance == null) { UnityEngine.Debug.LogError("[Test] GameManager not found — enter Play Mode first."); return; }
        Instance.OnPrescriptionReceived(60f, 1000f, 5, 0.3f, 2f, 60f);
        UnityEngine.Debug.Log("[Test] Prescription sent — 5 reps, 60s, spawn every 2s.");
    }

    [UnityEditor.MenuItem("Test/2 - Start Session")]
    static void EditorStartSession()
    {
        if (Instance == null) { UnityEngine.Debug.LogError("[Test] GameManager not found — enter Play Mode first."); return; }
        Instance.OnCommand("start");
        UnityEngine.Debug.Log("[Test] Start command sent.");
    }

    [UnityEditor.MenuItem("Test/3 - Stop Session")]
    static void EditorStopSession()
    {
        if (Instance == null) { UnityEngine.Debug.LogError("[Test] GameManager not found — enter Play Mode first."); return; }
        Instance.OnCommand("stop");
        UnityEngine.Debug.Log("[Test] Stop command sent.");
    }

    [UnityEditor.MenuItem("Test/4 - Simulate Rep Done")]
    static void EditorSimulateRep()
    {
        if (Instance == null) { UnityEngine.Debug.LogError("[Test] GameManager not found — enter Play Mode first."); return; }
        Instance.OnRepCompleted(62f, 1100f);
        UnityEngine.Debug.Log("[Test] Rep simulated.");
    }

    [UnityEditor.MenuItem("Test/5 - Reset")]
    static void EditorReset()
    {
        if (Instance == null) { UnityEngine.Debug.LogError("[Test] GameManager not found — enter Play Mode first."); return; }
        Instance.OnCommand("reset");
        UnityEngine.Debug.Log("[Test] Reset sent.");
    }
#endif
}
