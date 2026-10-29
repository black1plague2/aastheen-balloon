using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrophyAssembler : MonoBehaviour
{
    [Header("Tiles")]
    public TrophyTile[] allTiles;

    [Header("Victory FX")]
    public MeshRenderer trophyShellRenderer;
    public ParticleSystem confettiSystem;
    public Light trophyGlowLight;
    public AudioSource fanfareSource;
    public AudioClip fanfareClip;

    public event Action OnTrophyComplete;

    TrophyTile[] _activeTiles;
    string _promptMode;
    int _tilesPlaced;
    int _nextSequentialIndex;
    List<int> _remainingRandom;

    public int TilesPlaced => _tilesPlaced;
    public int TotalTiles  => _activeTiles?.Length ?? 0;

    public void Initialize(int tileCount, string promptMode)
    {
        _promptMode = promptMode;
        _tilesPlaced = 0;
        _nextSequentialIndex = 0;

        int count = Mathf.Min(tileCount, allTiles.Length);
        _activeTiles = new TrophyTile[count];

        for (int i = 0; i < allTiles.Length; i++)
        {
            if (allTiles[i] == null) continue;
            bool active = i < count;
            allTiles[i].gameObject.SetActive(active);
            if (active) _activeTiles[i] = allTiles[i];
        }

        _remainingRandom = new List<int>();
        for (int i = 0; i < count; i++) _remainingRandom.Add(i);
    }

    // Returns true when the last tile is placed.
    public bool TryPlaceNextTile()
    {
        if (_activeTiles == null || _tilesPlaced >= _activeTiles.Length) return true;

        int idx;
        if (_promptMode == "sequential")
        {
            idx = _nextSequentialIndex % _activeTiles.Length;
            _nextSequentialIndex++;
        }
        else
        {
            if (_remainingRandom.Count == 0) return true;
            int r = UnityEngine.Random.Range(0, _remainingRandom.Count);
            idx = _remainingRandom[r];
            _remainingRandom.RemoveAt(r);
        }

        _activeTiles[idx].Activate();
        _tilesPlaced++;

        if (_tilesPlaced >= _activeTiles.Length)
        {
            StartCoroutine(PlayVictorySequence());
            return true;
        }
        return false;
    }

    IEnumerator PlayVictorySequence()
    {
        yield return new WaitForSeconds(0.8f);

        if (trophyShellRenderer != null) trophyShellRenderer.enabled = false;
        if (confettiSystem != null) confettiSystem.Play();
        if (fanfareSource != null && fanfareClip != null)
            fanfareSource.PlayOneShot(fanfareClip);

        // Glow pulse in
        if (trophyGlowLight != null)
        {
            float t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; trophyGlowLight.intensity = Mathf.Lerp(0f, 3f, t / 0.5f); yield return null; }
            t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; trophyGlowLight.intensity = Mathf.Lerp(3f, 1f, t / 0.5f); yield return null; }
        }

        // Trophy spin
        float elapsed = 0f;
        while (elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            transform.Rotate(0f, 30f * Time.deltaTime, 0f);
            yield return null;
        }

        OnTrophyComplete?.Invoke();
    }
}
