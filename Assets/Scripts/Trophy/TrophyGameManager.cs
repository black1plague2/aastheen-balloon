using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class TrophyGameManager : MonoBehaviour
{
    [Header("References")]
    public TrophyAssembler assembler;
    public TrophyPromptUI promptUI;

    [Header("Session Settings")]
    public int    tileCount       = 9;
    public string promptMode      = "random";
    public float  holdDurationSec = 2f;
    public float  sessionDuration = 120f;

    TrophyPoseDetector[] _detectors;
    Dictionary<string, int> _poseCounts = new Dictionary<string, int>();
    float _sessionTimer;
    bool  _sessionActive;
    bool  _complete;
    string _currentPromptPose;

    string[] _poseOrder;

    void Start()
    {
        _detectors = FindObjectsByType<TrophyPoseDetector>(FindObjectsInactive.Exclude);

        var poseNames = new System.Collections.Generic.List<string>();
        foreach (var d in _detectors)
        {
            d.holdDuration = holdDurationSec;
            d.OnPoseHeld  += HandlePoseHeld;
            _poseCounts[d.poseName] = 0;
            poseNames.Add(d.poseName);
        }
        _poseOrder = poseNames.ToArray();

        assembler.OnTrophyComplete += OnAllTilesPlaced;
        assembler.Initialize(tileCount, promptMode);

        _currentPromptPose = GetNextPrompt();
        promptUI?.SetPrompt(_currentPromptPose, 0, tileCount);

        _sessionActive = true;
        Debug.Log($"[TrophyGame] Session started. Tiles={tileCount} Mode={promptMode} Detectors={_detectors.Length}");
    }

    void Update()
    {
        if (!_sessionActive || _complete) return;

        _sessionTimer += Time.deltaTime;
        if (_sessionTimer >= sessionDuration)
        {
            EndSession(forced: true);
            return;
        }

        // Feed hold progress to UI for the currently prompted pose
        foreach (var d in _detectors)
        {
            if (d.poseName == _currentPromptPose)
            {
                promptUI?.UpdateHoldProgress(d.HoldProgress);
                break;
            }
        }
    }

    void HandlePoseHeld(string poseName)
    {
        if (!_sessionActive || _complete) return;

        if (_poseCounts.ContainsKey(poseName)) _poseCounts[poseName]++;

        OVRInput.SetControllerVibration(0.4f, 0.4f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0.4f, 0.4f, OVRInput.Controller.RTouch);
        Invoke(nameof(StopHaptics), 0.5f);

        bool complete = assembler.TryPlaceNextTile();
        if (!complete)
        {
            _currentPromptPose = GetNextPrompt();
            promptUI?.SetPrompt(_currentPromptPose, assembler.TilesPlaced, assembler.TotalTiles);
        }
    }

    void StopHaptics()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }

    void OnAllTilesPlaced()
    {
        _complete = true;
        _sessionActive = false;
        promptUI?.ShowCompletion();
        StartCoroutine(FinishAfterDelay());
    }

    IEnumerator FinishAfterDelay()
    {
        yield return new WaitForSeconds(4.5f);
        EndSession(forced: false);
    }

    void EndSession(bool forced)
    {
        _sessionActive = false;
        Debug.Log($"[TrophyGame] Session ended. forced={forced} tiles={assembler.TilesPlaced}/{assembler.TotalTiles}");
        foreach (var kv in _poseCounts)
            Debug.Log($"  {kv.Key}: {kv.Value} reps");
        // TODO: submit to VerseClient when integrating with main app
    }

    string GetNextPrompt()
    {
        if (_detectors.Length == 0) return "None";
        if (promptMode == "sequential")
            return _poseOrder[assembler.TilesPlaced % _poseOrder.Length];
        return _detectors[Random.Range(0, _detectors.Length)].poseName;
    }
}
