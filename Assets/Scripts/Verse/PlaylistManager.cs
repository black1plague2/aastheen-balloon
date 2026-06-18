using System.Collections.Generic;
using UnityEngine;

// Persists across scenes (Bootstrap → Game scenes → Bootstrap).
// Bootstrap fills it after a successful /prescriptions/verify.
// GameManager reads it on Start to get settings and session auth.
public class PlaylistManager : MonoBehaviour
{
    public static PlaylistManager Instance { get; private set; }

    // ── Session auth ──────────────────────────────────────────────────────────
    public int    SessionId    { get; private set; }
    public string SessionToken { get; private set; }

    // ── Prescription info ─────────────────────────────────────────────────────
    public string         PatientName { get; private set; }
    public bool           HasSession  => SessionId > 0 && !string.IsNullOrEmpty(SessionToken);

    // ── Single-game (game_id == "balloon" | "garden") ─────────────────────────
    public string         GameId   { get; private set; }
    public BalloonSettings Settings { get; private set; }
    public GardenSettings  GardenSettings { get; private set; }

    // ── Multi-game playlist ───────────────────────────────────────────────────
    private readonly Queue<MultiGameEntry> _queue = new Queue<MultiGameEntry>();
    public bool HasNextGame => _queue.Count > 0;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Called by BootstrapManager on verify success ──────────────────────────

    public void Set(VerifyCodeResponse response)
    {
        SessionId    = response.session_id;
        SessionToken = response.session_token;
        PatientName  = response.prescription?.patient_name ?? "";
        GameId       = response.prescription?.game_id ?? "balloon";
        _queue.Clear();

        if (GameId == "multi" && response.prescription.multiGames != null)
        {
            foreach (var entry in response.prescription.multiGames)
                _queue.Enqueue(entry);

            AdvanceToNextGame();
        }
        else if (GameId == "garden")
        {
            GardenSettings = response.prescription?.gardenTargets ?? new GardenSettings();
            Settings       = null;
        }
        else
        {
            Settings       = response.prescription?.targets ?? new BalloonSettings();
            GardenSettings = null;
        }
    }

    // ── Called by GameManager/GardenGameManager at end of each game ───────────

    /// Pops the next game from the queue, updates GameId + Settings/GardenSettings.
    /// Returns the scene name to load, or null if playlist is empty.
    public string AdvanceToNextGame()
    {
        if (_queue.Count == 0) return null;

        var entry  = _queue.Dequeue();
        GameId = entry.id;

        if (entry.id == "balloon")
        {
            Settings       = entry.ToBalloonSettings();
            GardenSettings = null;
        }
        else if (entry.id == "garden")
        {
            GardenSettings = entry.ToGardenSettings();
            Settings       = null;
        }

        return GameIdToSceneName(entry.id);
    }

    public void Clear()
    {
        SessionId    = 0;
        SessionToken = null;
        GameId       = null;
        PatientName  = null;
        Settings     = null;
        GardenSettings = null;
        _queue.Clear();
    }

    // ── Scene name mapping ────────────────────────────────────────────────────

    public static string GameIdToSceneName(string gameId) => gameId?.ToLower() switch
    {
        "balloon"   => "ballooon",        // scene file is Assets/Scenes/ballooon.unity
        "garden"    => "Game_Garden",
        "bowarrow"  => "Game_BowArrow",
        "bow_arrow" => "Game_BowArrow",
        "armcurl"   => "Game_ArmCurl",
        "arm_curl"  => "Game_ArmCurl",
        _           => "ballooon",
    };
}
