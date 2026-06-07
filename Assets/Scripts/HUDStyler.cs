using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to UI_Canvas. Runs after HUDCanvasSetup (2-frame delay), styles all HUD text
// and adds a dark glass background + stat card panels.
public class HUDStyler : MonoBehaviour
{
    // ── Colour palette ─────────────────────────────────────────────────────────
    static readonly Color cBg     = new Color(0.05f, 0.06f, 0.13f, 0.88f);
    static readonly Color cCard   = new Color(0.10f, 0.12f, 0.22f, 0.94f);
    static readonly Color cAccent = new Color(0.15f, 0.20f, 0.35f, 0.94f);
    static readonly Color cScore  = new Color(1.00f, 0.85f, 0.20f);  // gold
    static readonly Color cTimer  = new Color(0.35f, 0.90f, 1.00f);  // cyan
    static readonly Color cReps   = new Color(0.70f, 0.88f, 1.00f);  // sky-blue
    static readonly Color cMode   = new Color(0.72f, 0.55f, 1.00f);  // violet
    static readonly Color cStreak = new Color(1.00f, 0.60f, 0.10f);  // orange
    static readonly Color cBadge  = new Color(1.00f, 0.95f, 0.35f);  // bright gold

    void Start() => StartCoroutine(StyleAfterSetup());

    IEnumerator StyleAfterSetup()
    {
        // Wait for HUDCanvasSetup to position the canvas (it waits 1 frame)
        yield return null;
        yield return null;

        var gm  = GameManager.Instance;
        var srs = StreakRewardSystem.Instance;

        // ── Full-canvas dark glass background ──────────────────────────────────
        AddFullBg(cBg);

        // ── Stat texts ─────────────────────────────────────────────────────────
        if (gm != null)
        {
            StyleStat(gm.scoreText,    cScore, 62, FontStyles.Bold);
            StyleStat(gm.timerText,    cTimer, 32, FontStyles.Bold);
            StyleStat(gm.repsText,     cReps,  30, FontStyles.Normal);
            StyleStat(gm.gameModeText, cMode,  22, FontStyles.Normal);
            StyleStat(gm.warningText,  Color.white, 44, FontStyles.Bold);

            // Dark card panels behind the three key stats
            AddCard(gm.scoreText?.rectTransform, cCard,   new Vector2(290, 108));
            AddCard(gm.timerText?.rectTransform, cAccent, new Vector2(230, 66));
            AddCard(gm.repsText?.rectTransform,  cAccent, new Vector2(230, 66));
        }

        if (srs != null)
        {
            StyleStat(srs.streakText,     cStreak, 34, FontStyles.Bold);
            StyleStat(srs.badgePopupText, cBadge,  40, FontStyles.Bold);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static void StyleStat(TMP_Text t, Color color, float size, FontStyles style)
    {
        if (t == null) return;
        t.color     = color;
        t.fontSize  = size;
        t.fontStyle = style;
    }

    void AddFullBg(Color color)
    {
        var go = new GameObject("_HUD_Bg");
        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(0);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void AddCard(RectTransform anchor, Color color, Vector2 size)
    {
        if (anchor == null) return;
        var go = new GameObject("_stat_card");
        go.transform.SetParent(anchor.parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchor.anchoredPosition;
        rt.sizeDelta        = size;
        // render just behind the text by inserting before it
        go.transform.SetSiblingIndex(anchor.GetSiblingIndex());
    }
}
