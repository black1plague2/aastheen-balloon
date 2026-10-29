using System;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;

public class TrophyPoseDetector : MonoBehaviour
{
    [Header("Pose")]
    public XRHandShape handShape;
    public string poseName;
    public float holdDuration     = 2f;
    public float deadZoneDuration = 1f;

    [Header("Tracking")]
    public XRHandTrackingEvents handTrackingEvents;

    public event Action<string> OnPoseHeld;
    public event Action<string> OnPoseLost;

    float _holdTimer;
    float _deadZoneTimer;
    bool  _held;
    bool  _fired;
    bool  _detected;

    void OnEnable()
    {
        if (handTrackingEvents != null)
            handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
    }

    void OnDisable()
    {
        if (handTrackingEvents != null)
            handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
    }

    void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
    {
        if (handShape == null) return;
        _detected = handTrackingEvents.handIsTracked && handShape.CheckConditions(args);
    }

    void Update()
    {
        if (_deadZoneTimer > 0f)
        {
            _deadZoneTimer -= Time.deltaTime;
            return;
        }

        if (_detected)
        {
            _holdTimer += Time.deltaTime;
            if (!_fired && _holdTimer >= holdDuration)
            {
                _fired         = true;
                _held          = true;
                _deadZoneTimer = deadZoneDuration;
                Debug.Log($"[TrophyPoseDetector:{poseName}] HELD");
                OnPoseHeld?.Invoke(poseName);
            }
        }
        else
        {
            if (_held) OnPoseLost?.Invoke(poseName);
            _held      = false;
            _holdTimer = 0f;
            _fired     = false;
        }
    }

    public float HoldProgress => _deadZoneTimer <= 0f ? Mathf.Clamp01(_holdTimer / holdDuration) : 0f;
    public float LiveScore    => _detected ? 1f : 0f;
}
