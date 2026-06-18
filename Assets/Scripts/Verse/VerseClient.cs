using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Singleton REST client for the Verse backend.
// Survives scene transitions so both Bootstrap and game scenes share it.
public class VerseClient : MonoBehaviour
{
    public static VerseClient Instance { get; private set; }

    [Header("Backend")]
    [Tooltip("Default URL baked into the build. Overridden at runtime by PlayerPrefs.")]
    public string BaseUrl = "https://aastheen.onrender.com";

    private const string PrefKey = "verse_backend_url";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Runtime override wins over the baked-in default.
        if (PlayerPrefs.HasKey(PrefKey))
            BaseUrl = PlayerPrefs.GetString(PrefKey);
    }

    /// <summary>Persists the URL so it survives app restarts.</summary>
    public void SetAndSaveBaseUrl(string url)
    {
        BaseUrl = url.TrimEnd('/');
        PlayerPrefs.SetString(PrefKey, BaseUrl);
        PlayerPrefs.Save();
        Debug.Log($"[VerseClient] BaseUrl saved: {BaseUrl}");
    }

    // ── /prescriptions/verify ────────────────────────────────────────────────

    public void VerifyCode(string code,
        Action<VerifyCodeResponse> onSuccess,
        Action<string>             onError)
    {
        StartCoroutine(VerifyCodeCoroutine(code, onSuccess, onError));
    }

    private IEnumerator VerifyCodeCoroutine(string code,
        Action<VerifyCodeResponse> onSuccess,
        Action<string>             onError)
    {
        string json  = JsonUtility.ToJson(new VerifyCodeRequest { code = code });
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        string url   = $"{BaseUrl}/prescriptions/verify";

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 40;

        Debug.Log($"[VerseClient] POST {url}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string err = string.IsNullOrEmpty(req.downloadHandler?.text) ? req.error : req.downloadHandler.text;
            Debug.LogError($"[VerseClient] VerifyCode failed: {err}");
            onError?.Invoke(err);
            yield break;
        }

        string raw = req.downloadHandler.text;
        Debug.Log($"[VerseClient] VerifyCode OK: {raw}");

        try
        {
            // Peek at game_id before full parse.
            var peek = JsonUtility.FromJson<VerifyCodeResponse>(raw);
            var gameId = peek?.prescription?.game_id;

            if (gameId == "garden")
            {
                // Two-pass: re-parse targets as GardenSettings.
                var gr = JsonUtility.FromJson<GardenVerifyCodeResponse>(raw);
                var response = new VerifyCodeResponse
                {
                    session_id    = gr.session_id,
                    session_token = gr.session_token,
                    status        = gr.status,
                    prescription  = new PrescriptionPublic
                    {
                        id            = gr.prescription.id,
                        game_id       = "garden",
                        game_name     = gr.prescription.game_name,
                        patient_name  = gr.prescription.patient_name,
                        gardenTargets = gr.prescription.targets,
                    }
                };
                onSuccess?.Invoke(response);
            }
            else if (gameId == "multi")
            {
                // Two-pass: re-parse with MultiGameTargets-aware struct.
                var multi = JsonUtility.FromJson<MultiVerifyCodeResponse>(raw);
                var games = multi?.prescription?.targets?.games;

                var response = new VerifyCodeResponse
                {
                    session_id    = multi.session_id,
                    session_token = multi.session_token,
                    status        = multi.status,
                    prescription  = new PrescriptionPublic
                    {
                        id           = multi.prescription.id,
                        game_id      = "multi",
                        game_name    = multi.prescription.game_name,
                        patient_name = multi.prescription.patient_name,
                        multiGames   = games,
                    }
                };
                onSuccess?.Invoke(response);
            }
            else
            {
                onSuccess?.Invoke(peek);
            }
        }
        catch (System.Exception e)
        {
            onError?.Invoke("Parse error: " + e.Message);
        }
    }

    // ── /sessions/{id}/results ───────────────────────────────────────────────

    public void SubmitResults(int sessionId, string sessionToken,
        BalloonGameMetrics metrics,
        Action             onDone,
        Action<string>     onError)
    {
        var body = new SessionResultsPayload { game_metrics = metrics };
        StartCoroutine(Post<SessionResultsPayload>(
            $"{BaseUrl}/sessions/{sessionId}/results",
            body, sessionToken,
            _ => onDone?.Invoke(), onError));
    }

    public void SubmitGardenResults(int sessionId, string sessionToken,
        GardenGameMetrics metrics,
        Action            onDone,
        Action<string>    onError)
    {
        var body = new GardenSessionResultsPayload { game_metrics = metrics };
        StartCoroutine(Post<GardenSessionResultsPayload>(
            $"{BaseUrl}/sessions/{sessionId}/results",
            body, sessionToken,
            _ => onDone?.Invoke(), onError));
    }

    // ── Generic POST ─────────────────────────────────────────────────────────

    private IEnumerator Post<T>(string url, object body, string bearerToken,
        Action<T>      onSuccess,
        Action<string> onError)
    {
        string json  = JsonUtility.ToJson(body);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(bearerToken))
            req.SetRequestHeader("Authorization", "Bearer " + bearerToken);

        req.timeout = 40;
        Debug.Log($"[VerseClient] POST {url}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[VerseClient] {url} OK: {req.downloadHandler.text}");
            try
            {
                var response = JsonUtility.FromJson<T>(req.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            catch (Exception e)
            {
                onError?.Invoke("Parse error: " + e.Message);
            }
        }
        else
        {
            string err = string.IsNullOrEmpty(req.downloadHandler?.text)
                ? req.error
                : req.downloadHandler.text;
            Debug.LogError($"[VerseClient] {url} failed: {err}");
            onError?.Invoke(err);
        }
    }
}
