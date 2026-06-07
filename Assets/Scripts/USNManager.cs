using UnityEngine;
using TMPro;

public class USNManager : MonoBehaviour
{
    public static USNManager Instance;

    [Header("UI")]
    public TMP_Text neglectWarningText; // "Look LEFT!"

    public bool UsnModeActive { get; private set; }

    // Spatial hit counters
    private int leftPops   = 0;
    private int centerPops = 0;
    private int rightPops  = 0;

    private float lastLeftPopTime;
    private const float NeglectAlertSeconds = 10f;

    public int   TotalPops      => leftPops + centerPops + rightPops;
    public float LeftFraction   => TotalPops > 0 ? (float)leftPops   / TotalPops : 0f;
    public float CenterFraction => TotalPops > 0 ? (float)centerPops / TotalPops : 0f;
    public float RightFraction  => TotalPops > 0 ? (float)rightPops  / TotalPops : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(bool usnMode)
    {
        UsnModeActive  = usnMode;
        leftPops       = centerPops = rightPops = 0;
        lastLeftPopTime = Time.time;
        if (neglectWarningText) neglectWarningText.text = "";
    }

    private void Update()
    {
        if (!UsnModeActive) return;
        if (Time.time - lastLeftPopTime > NeglectAlertSeconds)
        {
            if (neglectWarningText) neglectWarningText.text = "Look LEFT!";
        }
    }

    // Record a pop at worldX and track its zone
    public void RecordPop(float worldX)
    {
        if (worldX < -0.5f)
        {
            leftPops++;
            lastLeftPopTime = Time.time;
            if (neglectWarningText) neglectWarningText.text = "";
        }
        else if (worldX > 0.5f) rightPops++;
        else                    centerPops++;
    }

    // Returns (xMin, xMax) biased toward left in USN mode
    public void GetSpawnXRange(out float xMin, out float xMax)
    {
        if (!UsnModeActive) { xMin = -1.8f; xMax = 1.8f; return; }
        // 60% left, 40% right
        if (Random.value < 0.6f) { xMin = -2.5f; xMax = -0.5f; }
        else                     { xMin =  0.5f; xMax =  2.0f; }
    }

    public void Reset()
    {
        leftPops = centerPops = rightPops = 0;
        if (neglectWarningText) neglectWarningText.text = "";
    }
}
