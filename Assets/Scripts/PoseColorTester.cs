using UnityEngine;
using Oculus.Interaction;

public class PoseColorTester : MonoBehaviour
{
    public GameObject poseDetector;

    private IActiveState _activeState;
    private Renderer _renderer;
    private bool _lastState;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (poseDetector != null)
            _activeState = poseDetector.GetComponent<ActiveStateGroup>();
        SetColor(false);
    }

    void Update()
    {
        if (_activeState == null) return;
        bool active = _activeState.Active;
        if (active != _lastState)
        {
            _lastState = active;
            SetColor(active);
        }
    }

    void SetColor(bool detected)
    {
        if (_renderer != null)
            _renderer.material.color = detected ? Color.green : Color.red;
    }
}
