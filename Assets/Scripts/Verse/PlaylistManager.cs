using UnityEngine;

// Persists across scenes (Bootstrap → Game_Balloon → Bootstrap).
// Bootstrap fills it after a successful /prescriptions/verify.
// GameManager reads it on Start to get settings and session auth.
public class PlaylistManager : MonoBehaviour
{
    public static PlaylistManager Instance { get; private set; }

    // Session auth
    public int    SessionId    { get; private set; }
    public string SessionToken { get; private set; }

    // Prescription info
    public string        GameId      { get; private set; }
    public string        PatientName { get; private set; }
    public BalloonSettings Settings  { get; private set; }

    public bool HasSession => SessionId > 0 && !string.IsNullOrEmpty(SessionToken);

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Set(VerifyCodeResponse response)
    {
        SessionId    = response.session_id;
        SessionToken = response.session_token;
        GameId       = response.prescription?.game_id ?? "balloon";
        PatientName  = response.prescription?.patient_name ?? "";
        Settings     = response.prescription?.targets ?? new BalloonSettings();
    }

    public void Clear()
    {
        SessionId    = 0;
        SessionToken = null;
        GameId       = null;
        PatientName  = null;
        Settings     = null;
    }

    // Maps game_id from backend to the Unity scene name.
    // Add entries here as new games are integrated.
    public static string GameIdToSceneName(string gameId) => gameId?.ToLower() switch
    {
        "balloon"  => "ballooon",   // scene file is Assets/Scenes/ballooon.unity (triple-o)
        "bowarrow" => "Game_BowArrow",
        "bow_arrow"=> "Game_BowArrow",
        "armcurl"  => "Game_ArmCurl",
        "arm_curl" => "Game_ArmCurl",
        "garden"   => "Game_Garden",
        _          => "ballooon",
    };
}
