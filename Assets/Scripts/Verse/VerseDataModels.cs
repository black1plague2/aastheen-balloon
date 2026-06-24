using System;
using UnityEngine;

// ── Inbound: POST /prescriptions/verify ───────────────────────────────────────

[Serializable]
public class VerifyCodeRequest
{
    public string code;
}

// Balloon-specific prescription settings stored in the backend's `targets` dict.
// Doctor sets these fields when creating the prescription in the Flutter app.
[Serializable]
public class BalloonSettings
{
    public float  targetRotation     = 60f;
    public float  holdTimeMs         = 1000f;
    public int    repCount           = 10;
    public float  balloonSize        = 0.3f;
    public float  balloonSizeMin     = 0f;
    public float  balloonSizeMax     = 0f;
    public float  spawnInterval      = 3f;
    public float  sessionDuration    = 120f;
    public string gameMode           = "standard"; // standard | sequence | reaction | usn
    public int    distractorCount    = 1;
    public int    sequenceLength     = 3;
    public float  reactionTimeLimit  = -1f;
    public bool   adaptiveDifficulty = false;
    public bool   usnMode            = false;
    public int    cbsScore           = -1;
    public int    mptScore           = -1;
}

[Serializable]
public class PrescriptionPublic
{
    public int             id;
    public string          game_id;
    public string          game_name;
    public string          difficulty;
    public BalloonSettings targets;            // populated for game_id == "balloon"
    public bool            requires_nurse_approval;
    public string          patient_name;
    // Populated by VerseClient two-pass parse when game_id == "garden"
    [System.NonSerialized] public GardenSettings gardenTargets;
    // Populated by VerseClient two-pass parse when game_id == "pose"
    [System.NonSerialized] public PoseSettings poseTargets;
    // Populated by VerseClient two-pass parse when game_id == "multi"
    [System.NonSerialized] public MultiGameEntry[] multiGames;
}

[Serializable]
public class VerifyCodeResponse
{
    public int              session_id;
    public string           session_token;
    public string           status;        // "active" | "pending_approval"
    public PrescriptionPublic prescription;
}

// ── Outbound: POST /sessions/{id}/results ─────────────────────────────────────

[Serializable]
public class BalloonGameMetrics
{
    public int    total_score;
    public float  accuracy;
    public float  avg_reaction_time_ms;
    public int    max_streak;
    public int    correct_pops;
    public int    incorrect_pops;
    public int    missed_balloons;
    public string game_mode;
    public float  session_duration;
    public int    cbs_score;
    public int    mpt_score;
    public int    difficulty_level;
    public float  sequence_accuracy;
    public float  spatial_left;
    public float  spatial_center;
    public float  spatial_right;
}

// emg_samples / imu_samples are omitted intentionally — backend accepts null fine.
[Serializable]
public class SessionResultsPayload
{
    public BalloonGameMetrics game_metrics;
    public string             network_mode = "home";
}

// ── Garden settings (targets dict for game_id = "garden") ─────────────────────

[Serializable]
public class GardenSettings
{
    public int    tasksPerSession        = 2;
    public string gameMode               = "sequenced"; // "sequenced" | "freeorder"
    public bool   memoryMode             = false;
    public bool   showOverviewFirst      = false;
    public float  instructionDisplayTime = 4f;
    public float  sessionDuration        = 180f;
}

// ── Garden session metrics (outbound) ─────────────────────────────────────────

[Serializable]
public class GardenGameMetrics
{
    public int    correct_tasks;
    public int    mistakes;
    public int    total_tasks;
    public float  session_duration;
    public int    difficulty_level;
    public string game_mode;
}

[Serializable]
public class GardenSessionResultsPayload
{
    public GardenGameMetrics game_metrics;
    public string            network_mode = "home";
}

// ── Multi-game prescription (targets dict for game_id = "multi") ──────────────
// All per-game settings are FLAT inside each entry (no nested "settings" object)
// so JsonUtility can deserialise without polymorphism.

[Serializable]
public class MultiGameEntry
{
    public string id;     // "balloon" | "garden"
    public string name;

    // Balloon fields
    public float  targetRotation  = 60f;
    public float  holdTimeMs      = 1000f;
    public int    repCount        = 10;
    public float  sessionDuration = 120f;
    public float  spawnInterval   = 3f;
    public float  balloonSize     = 0.3f;
    public string gameMode        = "standard";

    // Garden fields (distinct names to avoid field clash with balloon)
    public int    tasksPerSession        = 2;
    public string gardenMode             = "sequenced"; // "sequenced" | "freeorder"
    public bool   memoryMode             = false;
    public bool   showOverviewFirst      = false;
    public float  instructionDisplayTime = 4f;
    public float  gardenDuration         = 180f;

    public BalloonSettings ToBalloonSettings() => new BalloonSettings
    {
        targetRotation  = targetRotation,
        holdTimeMs      = holdTimeMs,
        repCount        = repCount,
        sessionDuration = sessionDuration,
        spawnInterval   = spawnInterval,
        balloonSize     = balloonSize,
        gameMode        = gameMode,
    };

    public GardenSettings ToGardenSettings() => new GardenSettings
    {
        tasksPerSession        = tasksPerSession,
        gameMode               = gardenMode,
        memoryMode             = memoryMode,
        showOverviewFirst      = showOverviewFirst,
        instructionDisplayTime = instructionDisplayTime,
        sessionDuration        = gardenDuration,
    };
}

[Serializable]
public class MultiGameTargets
{
    public MultiGameEntry[] games;
}

// Two-pass types: used by VerseClient when game_id == "garden"
[Serializable]
public class GardenPrescriptionPublic
{
    public int            id;
    public string         game_id;
    public string         game_name;
    public string         patient_name;
    public GardenSettings targets;
}

[Serializable]
public class GardenVerifyCodeResponse
{
    public int                    session_id;
    public string                 session_token;
    public string                 status;
    public GardenPrescriptionPublic prescription;
}

// ── Pose game settings (targets dict for game_id = "pose") ───────────────────

[Serializable]
public class PoseSettings
{
    public float sessionDuration  = 120f;  // seconds
    public float holdDurationSec  = 3f;    // seconds pose must be held to count
    public int   repTarget        = 10;    // target reps for the session
}

// ── Pose session metrics (outbound) ──────────────────────────────────────────

[Serializable]
public class PoseGameMetrics
{
    public int   total_reps;
    public float session_duration;
    public int   thumbsup_count;
    public int   yo_count;
    public int   nice_count;
    public int   four_fingers_count;
    public int   bunny_count;
    public float hold_duration_sec;
}

[Serializable]
public class PoseSessionResultsPayload
{
    public PoseGameMetrics game_metrics;
    public string          network_mode = "home";
}

// Two-pass types: used by VerseClient when game_id == "multi"
[Serializable]
public class MultiPrescriptionPublic
{
    public int             id;
    public string          game_id;
    public string          game_name;
    public string          patient_name;
    public MultiGameTargets targets;
}

[Serializable]
public class MultiVerifyCodeResponse
{
    public int                    session_id;
    public string                 session_token;
    public string                 status;
    public MultiPrescriptionPublic prescription;
}
