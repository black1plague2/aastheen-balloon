using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StreakRewardSystem : MonoBehaviour
{
    public static StreakRewardSystem Instance;

    [Header("UI")]
    public TMP_Text streakText;
    public TMP_Text badgePopupText;

    public int CurrentStreak { get; private set; } = 0;
    public int MaxStreak     { get; private set; } = 0;

    private List<string> earnedBadgesList = new List<string>();
    private HashSet<string> earnedBadgesSet = new HashSet<string>();
    public List<string> EarnedBadges => earnedBadgesList;

    private bool  hadAnyIncorrect   = false;
    private bool  hadAnySmallMiss   = false;
    private float fastestReactionMs = float.MaxValue;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize()
    {
        CurrentStreak      = 0;
        MaxStreak          = 0;
        hadAnyIncorrect    = false;
        hadAnySmallMiss    = false;
        fastestReactionMs  = float.MaxValue;
        earnedBadgesList.Clear();
        earnedBadgesSet.Clear();
        if (streakText)     streakText.text     = "";
        if (badgePopupText) badgePopupText.text = "";
    }

    public int OnCorrectPop(float reactionMs, float balloonSize)
    {
        CurrentStreak++;
        MaxStreak = Mathf.Max(MaxStreak, CurrentStreak);
        if (reactionMs < fastestReactionMs) fastestReactionMs = reactionMs;

        int bonus = GetStreakBonus();
        UpdateStreakUI();
        CheckStreakMilestone();
        return bonus;
    }

    public void OnIncorrectPop(float balloonSize)
    {
        CurrentStreak   = 0;
        hadAnyIncorrect = true;
        if (balloonSize < 0.25f) hadAnySmallMiss = true;
        UpdateStreakUI();
    }

    public void OnMissedBalloon()
    {
        CurrentStreak   = 0;
        hadAnyIncorrect = true;
        UpdateStreakUI();
    }

    public void EvaluateSessionBadges(float overallAccuracy, float avgReactionMs,
        float sequenceAccuracy, float leftCoverage, bool usnMode)
    {
        if (!hadAnyIncorrect)                    AwardBadge("perfect_session");
        if (avgReactionMs < 800f)                AwardBadge("speed_demon");
        if (MaxStreak >= 7)                      AwardBadge("streak_master");
        if (!hadAnySmallMiss)                    AwardBadge("sniper");
        if (sequenceAccuracy >= 0.9f)            AwardBadge("memory_pro");
        if (usnMode && leftCoverage >= 0.35f)    AwardBadge("laterality_champ");
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private int GetStreakBonus()
    {
        if (CurrentStreak >= 10) return 300;
        if (CurrentStreak >= 7)  return 200;
        if (CurrentStreak >= 3)  return 50;
        return 0;
    }

    private void CheckStreakMilestone()
    {
        if (CurrentStreak == 3)
        {
            ShowPopup("On Fire! +50");
            PopEffectController.Instance?.PlayStreakMilestone();
        }
        else if (CurrentStreak == 7)
        {
            ShowPopup("Unstoppable! +200");
            PopEffectController.Instance?.PlayStreakMilestone();
        }
        else if (CurrentStreak == 10)
        {
            ShowPopup("LEGENDARY! +300");
            PopEffectController.Instance?.PlayStreakMilestone();
        }
    }

    private void AwardBadge(string id)
    {
        if (!earnedBadgesSet.Add(id)) return;
        earnedBadgesList.Add(id);
        ShowPopup(BadgeName(id));
        PopEffectController.Instance?.PlayBadgeEarned();
        Debug.Log($"[Rewards] Badge: {id}");
    }

    private void ShowPopup(string text)
    {
        if (badgePopupText == null) return;
        badgePopupText.text = text;
        CancelInvoke(nameof(ClearPopup));
        Invoke(nameof(ClearPopup), 2f);
    }

    private void ClearPopup()
    {
        if (badgePopupText) badgePopupText.text = "";
    }

    private void UpdateStreakUI()
    {
        if (streakText == null) return;
        streakText.text = CurrentStreak >= 3 ? $"Streak x{CurrentStreak}!" : "";
    }

    private string BadgeName(string id)
    {
        switch (id)
        {
            case "perfect_session":  return "PERFECT SESSION";
            case "speed_demon":      return "SPEED DEMON";
            case "streak_master":    return "STREAK MASTER";
            case "sniper":           return "SNIPER";
            case "memory_pro":       return "MEMORY PRO";
            case "laterality_champ": return "LATERALITY CHAMPION";
            default:                 return id.ToUpper();
        }
    }
}
