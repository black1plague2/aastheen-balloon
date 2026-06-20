using System.Collections;
using UnityEngine;

public class GhostHandGuide : MonoBehaviour
{
    [Header("Appearance")]
    public Renderer[] ghostRenderers;
    [Range(0f, 1f)] public float maxAlpha       = 0.55f;
    [Range(0f, 1f)] public float minAlpha        = 0.15f;
    public float fadeInDuration                  = 0.6f;
    public float fadeOutDuration                 = 0.4f;

    [Header("Animation")]
    public float pulseSpeed                      = 2f;
    public float floatAmplitude                  = 0.04f;
    public float floatSpeed                      = 1.2f;
    public float scalePulseAmount                = 0.08f;

    private Transform _target;
    private Vector3   _baseScale;
    private bool      _visible;
    private Coroutine _fadeRoutine;

    void Awake()
    {
        _baseScale = transform.localScale;
        gameObject.SetActive(false);
        SetAlpha(0f);
    }

    public void SetTarget(Transform t) { _target = t; }

    public void Show()
    {
        if (_visible) return;
        _visible = true;
        gameObject.SetActive(true);
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeTo(maxAlpha, fadeInDuration));
    }

    public void Hide()
    {
        if (!_visible) return;
        _visible = false;
        _target  = null;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutAndDeactivate());
    }

    void Update()
    {
        if (!_visible) return;

        if (_target != null)
        {
            Vector3 p = _target.position;
            p.y += Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = p;
        }

        float alpha = Mathf.Lerp(minAlpha, maxAlpha,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        SetAlpha(alpha);

        float s = 1f + Mathf.Sin(Time.time * pulseSpeed * 0.7f) * scalePulseAmount;
        transform.localScale = _baseScale * s;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start = CurrentAlpha();
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetAlpha(Mathf.Lerp(start, target, t / duration));
            yield return null;
        }
        SetAlpha(target);
    }

    IEnumerator FadeOutAndDeactivate()
    {
        yield return StartCoroutine(FadeTo(0f, fadeOutDuration));
        gameObject.SetActive(false);
    }

    void SetAlpha(float a)
    {
        foreach (var r in ghostRenderers)
        {
            if (r == null) continue;
            Color c = r.material.color;
            c.a = a;
            r.material.color = c;
        }
    }

    float CurrentAlpha() =>
        ghostRenderers != null && ghostRenderers.Length > 0 && ghostRenderers[0] != null
            ? ghostRenderers[0].material.color.a
            : 0f;
}
