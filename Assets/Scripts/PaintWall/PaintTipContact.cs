using UnityEngine;

/// <summary>
/// Attach to a small trigger collider at the working end of a paint tool
/// (brush bristle tip / roller cover). While overlapping a PaintableSurface
/// collider, continuously stamps paint at the contact point.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PaintTipContact : MonoBehaviour
{
    [Tooltip("Minimum seconds between paint stamps while in continuous contact.")]
    public float stampInterval = 0.03f;

    private float _lastStampTime;

    void OnTriggerStay(Collider other)
    {
        if (Time.unscaledTime - _lastStampTime < stampInterval) return;

        var surface = other.GetComponent<PaintableSurface>();
        if (surface == null) return;

        Vector3 tipPos = transform.position;
        Vector3 closest = other.ClosestPoint(tipPos);
        Vector3 dir = closest - tipPos;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        _lastStampTime = Time.unscaledTime;
        surface.PaintAtWorldPoint(tipPos, dir);
    }
}
