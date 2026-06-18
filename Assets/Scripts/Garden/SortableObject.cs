using UnityEngine;

public class SortableObject : MonoBehaviour
{
    [Header("Object Settings")]
    public string objectColor;
    public float glowIntensity = 1.5f;
    public bool useGlow = true;

    private Rigidbody rb;
    private bool isPlaced = false;
    private MeshRenderer meshRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Tomatoes may have renderer on child mesh
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (useGlow) ApplyGlow();
    }

    void ApplyGlow()
    {
        if (meshRenderer == null) return;

        Material[] mats = meshRenderer.materials;
        foreach (Material mat in mats)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(
                "_EmissionColor",
                GetGlowColor() * glowIntensity
            );
        }
        meshRenderer.materials = mats;
    }

    Color GetGlowColor()
    {
        switch (objectColor.ToLower())
        {
            case "red":    return new Color(1f, 0.2f, 0.2f);
            case "green":  return new Color(0.2f, 1f, 0.2f);
            case "yellow": return new Color(1f, 1f, 0.2f);
            case "orange": return new Color(1f, 0.5f, 0f);
            case "purple": return new Color(0.7f, 0f, 1f);
            default:       return Color.white;
        }
    }

    public void PlaceInZone(Transform zoneTransform)
    {
        if (isPlaced) return;

        isPlaced = true;

        // Stop all physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Snap to zone center slightly above surface
        transform.position = new Vector3(
            zoneTransform.position.x,
            zoneTransform.position.y + 0.08f,
            zoneTransform.position.z
        );

        transform.rotation = Quaternion.identity;

        // Increase glow intensity on placement
        if (useGlow && meshRenderer != null)
        {
            Material[] mats = meshRenderer.materials;
            foreach (Material mat in mats)
            {
                mat.SetColor(
                    "_EmissionColor",
                    GetGlowColor() * (glowIntensity * 3f)
                );
            }
            meshRenderer.materials = mats;
        }

        Debug.Log(objectColor + " tomato placed correctly!");
    }

    public bool IsPlaced() => isPlaced;
    public string GetColor() => objectColor;
}