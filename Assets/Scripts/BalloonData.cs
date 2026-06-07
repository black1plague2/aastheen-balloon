using UnityEngine;

public enum BalloonType { Red, Green, Blue, Yellow }
public enum GameMode    { Standard, Sequence, Reaction, USN }

public static class BalloonData
{
    public static Color GetColor(BalloonType t)
    {
        switch (t)
        {
            case BalloonType.Red:    return new Color(0.95f, 0.15f, 0.15f);
            case BalloonType.Green:  return new Color(0.15f, 0.85f, 0.25f);
            case BalloonType.Blue:   return new Color(0.20f, 0.45f, 0.95f);
            case BalloonType.Yellow: return new Color(0.95f, 0.85f, 0.10f);
            default:                 return Color.white;
        }
    }

    // Smaller balloons award more points
    public static int GetBaseScore(float size)
    {
        int sizeBonus = Mathf.RoundToInt((0.5f - size) * 200f);
        return 100 + Mathf.Max(0, sizeBonus);
    }
}
