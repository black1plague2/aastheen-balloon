using UnityEngine;

public class SeedSortingZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public string acceptedColor;
    public string zoneName;

    [Header("Materials")]
    public Material normalMaterial;
    public Material correctFlashMaterial;
    public Material incorrectFlashMaterial;

    [Header("Particles")]
    public ParticleSystem successParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip incorrectSound;

    [Header("Haptics")]
    public float correctHapticStrength = 0.3f;
    public float correctHapticDuration = 0.5f;
    public float incorrectHapticStrength = 1f;
    public float incorrectHapticDuration = 0.2f;

    private MeshRenderer zoneRenderer;
    private int correctPlaced = 0;

    void Start()
    {
        zoneRenderer = GetComponent<MeshRenderer>();

        // Store normal material
        if (zoneRenderer != null && normalMaterial == null)
            normalMaterial = zoneRenderer.material;

        // Make sure collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check the object itself first
        SortableObject sortable =
            other.GetComponent<SortableObject>();

        // Then check parent if not found
        if (sortable == null)
            sortable =
                other.GetComponentInParent<SortableObject>();

        // Ignore if no sortable found
        if (sortable == null) return;

        // Ignore already placed objects
        if (sortable.IsPlaced()) return;

        // Compare colors
        if (sortable.GetColor().ToLower()
            == acceptedColor.ToLower())
        {
            HandleCorrect(sortable);
        }
        else
        {
            HandleIncorrect(sortable);
        }
    }

    void HandleCorrect(SortableObject sortable)
    {
        correctPlaced++;

        // Report to score manager
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddCorrect();

        // Lock tomato into zone
        sortable.PlaceInZone(transform);

        // Flash correct material
        if (zoneRenderer != null
            && correctFlashMaterial != null)
        {
            zoneRenderer.material = correctFlashMaterial;
            Invoke("ResetMaterial", 1.2f);
        }

        // Play success particles
        if (successParticles != null)
            successParticles.Play();

        // Play correct sound
        if (audioSource != null && correctSound != null)
            audioSource.PlayOneShot(correctSound);

        // Gentle positive haptic
        OVRInput.SetControllerVibration(
            correctHapticStrength,
            correctHapticStrength,
            OVRInput.Controller.RTouch
        );
        Invoke("StopHaptic", correctHapticDuration);

        Debug.Log("Correct! " + sortable.GetColor()
            + " in " + zoneName);
    }

    void HandleIncorrect(SortableObject sortable)
    {
        // Report to score manager
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddWrong();

        // Flash incorrect material briefly
        if (zoneRenderer != null
            && incorrectFlashMaterial != null)
        {
            zoneRenderer.material = incorrectFlashMaterial;
            Invoke("ResetMaterial", 0.4f);
        }

        // Play incorrect sound
        if (audioSource != null && incorrectSound != null)
            audioSource.PlayOneShot(incorrectSound);

        // Sharp negative haptic
        OVRInput.SetControllerVibration(
            incorrectHapticStrength,
            incorrectHapticStrength,
            OVRInput.Controller.RTouch
        );
        Invoke("StopHaptic", incorrectHapticDuration);

        Debug.Log("Wrong! " + sortable.GetColor()
            + " does not go in " + zoneName);
    }

    void ResetMaterial()
    {
        if (zoneRenderer != null && normalMaterial != null)
            zoneRenderer.material = normalMaterial;
    }

    void StopHaptic()
    {
        OVRInput.SetControllerVibration(
            0f, 0f, OVRInput.Controller.RTouch
        );
    }

    public int GetCorrectPlaced() => correctPlaced;
}