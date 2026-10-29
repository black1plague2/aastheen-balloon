using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrophyPromptUI : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text gestureNameText;
    public TMP_Text progressLabel;
    public TMP_Text completionLabel;

    [Header("Hold Bar")]
    public Image holdFillBar;

    [Header("Icons (order: OpenHand, FingerSpread, PinchClose, FullFist, PointIndex)")]
    public Image   poseIconDisplay;
    public Sprite[] poseIcons;

    static readonly string[] POSE_NAMES   = { "OpenHand", "FingerSpread", "PinchClose", "FullFist", "PointIndex" };
    static readonly string[] POSE_DISPLAY = { "Open Hand", "Finger Spread", "Pinch Close", "Full Fist", "Point Index" };

    void Awake()
    {
        if (completionLabel != null) completionLabel.gameObject.SetActive(false);
    }

    public void SetPrompt(string poseName, int placed, int total)
    {
        int idx = System.Array.IndexOf(POSE_NAMES, poseName);

        if (gestureNameText != null)
            gestureNameText.text = idx >= 0 ? POSE_DISPLAY[idx] : poseName;

        if (progressLabel != null)
            progressLabel.text = $"{placed} / {total}";

        if (holdFillBar != null)
            holdFillBar.fillAmount = 0f;

        if (poseIconDisplay != null && poseIcons != null
            && idx >= 0 && idx < poseIcons.Length && poseIcons[idx] != null)
            poseIconDisplay.sprite = poseIcons[idx];
    }

    public void UpdateHoldProgress(float progress)
    {
        if (holdFillBar != null)
            holdFillBar.fillAmount = Mathf.Clamp01(progress);
    }

    public void ShowCompletion()
    {
        if (gestureNameText  != null) gestureNameText.gameObject.SetActive(false);
        if (progressLabel    != null) progressLabel.gameObject.SetActive(false);
        if (holdFillBar      != null) holdFillBar.transform.parent.gameObject.SetActive(false);
        if (completionLabel  != null) completionLabel.gameObject.SetActive(true);
    }
}
