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
        var body = new VerifyCodeRequest { code = code };
        StartCoroutine(Post<VerifyCodeResponse>(
            $"{BaseUrl}/prescriptions/verify",
            body, null, onSuccess, onError));
    }

    // ── /sessions/{id}/results ───────────────────────────────────────────────

    public void SubmitResults(int sessionId, string sessionToken,
        BalloonGameMetrics metrics,
        Action             onDone,
        Action<string>     onError)
    {
        var body = new SessionResultsPayload { game_metrics = metrics };
        StartCoroutine(Post<SessionResultsPayload>(        // response body ignored
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
