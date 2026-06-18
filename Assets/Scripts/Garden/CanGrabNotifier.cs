using UnityEngine;
using Oculus.Interaction;

public class CanGrabNotifier : MonoBehaviour
{
    [HideInInspector] public bool isGrabbed = false;

    private Grabbable grabbable;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();

        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised +=
                HandlePointerEvent;
        }
        else
        {
            Debug.LogError("No Grabbable found on " + gameObject.name);
        }
    }

    void HandlePointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                isGrabbed = true;
                Debug.Log("Can grabbed!");
                break;

            case PointerEventType.Unselect:
                isGrabbed = false;
                Debug.Log("Can released!");
                break;
        }
    }

    void OnDestroy()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -=
                HandlePointerEvent;
        }
    }

    public bool IsGrabbed() => isGrabbed;
}