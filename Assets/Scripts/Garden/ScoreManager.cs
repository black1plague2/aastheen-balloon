using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    // Static instance so any script can access it
    public static ScoreManager Instance;

    [Header("Game Settings")]
    public int totalObjects = 6;

    [Header("UI Text References")]
    public TextMeshProUGUI txt_Score;
    public TextMeshProUGUI txt_Correct;
    public TextMeshProUGUI txt_Wrong;
    public TextMeshProUGUI txt_Remaining;
    public TextMeshProUGUI txt_Complete;

    [Header("Colors")]
    public Color correctColor = new Color(0f, 1f, 0.4f);
    public Color wrongColor = new Color(1f, 0.3f, 0.3f);
    public Color remainingColor = new Color(1f, 1f, 0f);
    public Color completeColor = new Color(0f, 1f, 0.4f);

    private int correctCount = 0;
    private int wrongCount = 0;

    void Awake()
    {
        // Make this a singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Hide complete message at start
        if (txt_Complete != null)
            txt_Complete.gameObject.SetActive(false);

        // Show initial values
        RefreshUI();
    }

    public void AddCorrect()
    {
        correctCount++;
        RefreshUI();
        CheckCompletion();
    }

    public void AddWrong()
    {
        wrongCount++;
        RefreshUI();
    }

    void RefreshUI()
    {
        int remaining = totalObjects - correctCount;

        // Score text
        if (txt_Score != null)
        {
            txt_Score.text = "Score: "
                + correctCount + " / " + totalObjects;
        }

        // Correct count
        if (txt_Correct != null)
        {
            txt_Correct.text = "✓ Correct: " + correctCount;
            txt_Correct.color = correctColor;
        }

        // Wrong count
        if (txt_Wrong != null)
        {
            txt_Wrong.text = "✗ Mistakes: " + wrongCount;
            txt_Wrong.color = wrongCount > 0
                ? wrongColor
                : Color.white;
        }

        // Remaining count
        if (txt_Remaining != null)
        {
            txt_Remaining.text = "Remaining: " + remaining;
            txt_Remaining.color = remaining > 0
                ? remainingColor
                : correctColor;
        }
    }

    void CheckCompletion()
    {
        if (correctCount < totalObjects) return;

        // All sorted correctly!
        if (txt_Complete != null)
        {
            txt_Complete.gameObject.SetActive(true);

            if (wrongCount == 0)
            {
                txt_Complete.text =
                    "PERFECT!\nAll " + totalObjects
                    + " sorted correctly!\nNo mistakes!";
            }
            else
            {
                txt_Complete.text =
                    "WELL DONE!\n"
                    + correctCount + "/"
                    + totalObjects + " correct\n"
                    + wrongCount + " mistake(s)";
            }

            txt_Complete.color = completeColor;
        }

        // Celebration haptic pattern
        StartCoroutine(CelebrationHaptic());

        Debug.Log("GAME COMPLETE! Correct: "
            + correctCount + " Wrong: " + wrongCount);
    }

    System.Collections.IEnumerator CelebrationHaptic()
    {
        for (int i = 0; i < 3; i++)
        {
            OVRInput.SetControllerVibration(
                0.5f, 0.5f, OVRInput.Controller.RTouch
            );
            yield return new WaitForSeconds(0.2f);
            OVRInput.SetControllerVibration(
                0f, 0f, OVRInput.Controller.RTouch
            );
            yield return new WaitForSeconds(0.1f);
        }
    }

    public int GetCorrect() => correctCount;
    public int GetWrong() => wrongCount;
    public bool IsComplete() => correctCount >= totalObjects;
}