using System;
using System.Collections;
using UnityEngine;

public class TrophyTile : MonoBehaviour
{
    public int tileIndex;
    public Vector3 brokenOffset = new Vector3(0.4f, 0.1f, 0.4f);
    public AudioClip chimeNote;
    public ParticleSystem sparklePS;
    public float animDuration = 0.65f;
    public AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public event Action<int> OnActivated;

    static readonly Color INACTIVE = new Color(0.4f, 0.4f, 0.42f);
    static readonly Color ACTIVE   = new Color(1.0f, 0.84f, 0.0f);
    const float EMISSION_MULT = 2.5f;

    Vector3 _targetPos;
    Material _mat;
    bool _activated;

    void Awake()
    {
        _targetPos = transform.localPosition;
        transform.localPosition = _targetPos + brokenOffset;
        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            _mat = rend.material;
            _mat.color = INACTIVE;
        }
    }

    public void Activate()
    {
        if (_activated) return;
        _activated = true;
        StartCoroutine(AnimateFlyIn());
    }

    IEnumerator AnimateFlyIn()
    {
        Vector3 startPos = transform.localPosition;
        float t = 0f;

        if (_mat != null) _mat.EnableKeyword("_EMISSION");

        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float e = animCurve.Evaluate(Mathf.Clamp01(t));
            transform.localPosition = Vector3.Lerp(startPos, _targetPos, e);
            if (_mat != null)
            {
                _mat.color = Color.Lerp(INACTIVE, ACTIVE, e);
                _mat.SetColor("_EmissionColor", Color.Lerp(Color.black, ACTIVE * EMISSION_MULT, e));
                _mat.SetFloat("_Metallic", Mathf.Lerp(0.15f, 0.85f, e));
            }
            yield return null;
        }

        transform.localPosition = _targetPos;
        if (_mat != null)
        {
            _mat.color = ACTIVE;
            _mat.SetColor("_EmissionColor", ACTIVE * EMISSION_MULT);
            _mat.SetFloat("_Metallic", 0.85f);
        }

        if (sparklePS != null) sparklePS.Play();
        if (chimeNote != null) AudioSource.PlayClipAtPoint(chimeNote, transform.position, 0.8f);

        OnActivated?.Invoke(tileIndex);
    }
}
