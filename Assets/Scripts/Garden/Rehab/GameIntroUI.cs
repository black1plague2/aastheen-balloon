using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// World-space multi-slide intro panel shown before each garden session.
/// Walks the patient through how-to-play and rehab context,
/// then fires OnDone so GardenGameManager can start the session.
/// </summary>
public class GameIntroUI : MonoBehaviour
{
    public static GameIntroUI Instance { get; private set; }

    [Header("Root Panel")]
    public GameObject introPanel;

    [Header("Content")]
    public TextMeshProUGUI txt_Title;
    public TextMeshProUGUI txt_Body;
    public TextMeshProUGUI txt_Counter;

    [Header("Accent Bar (colour changes per slide)")]
    public Image img_AccentBar;

    [Header("Buttons")]
    public Button btn_Back;
    public Button btn_Next;
    public Button btn_Start;

    [Header("Progress Dots")]
    public Transform dotsRoot;
    public GameObject dotPrefab;

    [Header("Slide Accent Colours")]
    public Color[] accentColors = new Color[]
    {
        new Color(0.40f, 0.85f, 1.00f),   // sky blue
        new Color(0.50f, 1.00f, 0.60f),   // mint
        new Color(1.00f, 0.85f, 0.30f),   // gold
        new Color(0.55f, 0.85f, 1.00f),   // periwinkle
        new Color(1.00f, 0.60f, 0.30f),   // coral
        new Color(0.35f, 1.00f, 0.80f),   // teal
    };

    public System.Action OnDone;

    // ── Slide content ──────────────────────────────────────────────────────

    struct Slide { public string title, body; }

    static readonly Slide[] Slides =
    {
        new Slide
        {
            title = "Welcome to Garden Therapy",
            body  =
                "In this activity you will care for plants\n" +
                "using real gardening tools.\n\n" +
                "Each task gently exercises your arm,\n" +
                "wrist, and fingers in a fun way.\n\n" +
                "Take your time and enjoy the garden!"
        },
        new Slide
        {
            title = "Reading Your Task",
            body  =
                "A panel will appear telling you:\n\n" +
                "  Which PLANT needs care\n" +
                "  Which TOOL to pick up\n" +
                "  What MOVEMENT to make\n\n" +
                "Follow the glowing highlight\n" +
                "on the plant and tool to guide you."
        },
        new Slide
        {
            title = "Picking Up Tools",
            body  =
                "Look at a tool and SQUEEZE the\n" +
                "controller trigger to grab it.\n\n" +
                "Release the trigger to put it down.\n\n" +
                "The tool you need glows —\n" +
                "use the colour to find it quickly."
        },
        new Slide
        {
            title = "Your Four Tools",
            body  =
                "Watering Can\n" +
                "  Reach forward, rotate palm up\n" +
                "  Trains forearm & shoulder\n\n" +
                "Scissors\n" +
                "  Open and close your fingers\n" +
                "  Trains grip & pinch strength\n\n" +
                "Shovel\n" +
                "  Scoop and lift motion\n" +
                "  Trains wrist flexion & elbow\n\n" +
                "Fertilizer\n" +
                "  Rotate palm down to pour\n" +
                "  Trains forearm pronation"
        },
        new Slide
        {
            title = "How to Complete a Task",
            body  =
                "1  Pick up the correct tool\n\n" +
                "2  Bring it close to the\n" +
                "    glowing plant\n\n" +
                "3  Hold it steady —\n" +
                "    watch the bar fill up\n\n" +
                "4  A vibration means success!\n\n" +
                "If the bar resets, move closer\n" +
                "and hold steady — try again."
        },
        new Slide
        {
            title = "Ready to Begin!",
            body  =
                "Your therapist has set up\n" +
                "today's tasks for you.\n\n" +
                "Work at your own comfortable pace.\n" +
                "There is no rush.\n\n" +
                "Press  START  when you are ready."
        }
    };

    // ── Runtime ────────────────────────────────────────────────────────────

    int     _idx;
    Image[] _dots;

    // ── Unity ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (introPanel != null) introPanel.SetActive(false);
    }

    void Start()
    {
        BuildDots();
        if (btn_Back  != null) btn_Back.onClick.AddListener(GoPrev);
        if (btn_Next  != null) btn_Next.onClick.AddListener(GoNext);
        if (btn_Start != null) btn_Start.onClick.AddListener(Finish);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void Show(System.Action onDone = null)
    {
        OnDone = onDone;
        _idx   = 0;
        if (introPanel != null) introPanel.SetActive(true);
        Refresh();
    }

    // ── Navigation ─────────────────────────────────────────────────────────

    void GoNext() { if (_idx < Slides.Length - 1) { _idx++; Refresh(); } else Finish(); }
    void GoPrev() { if (_idx > 0)                 { _idx--; Refresh(); } }

    void Finish()
    {
        if (introPanel != null) introPanel.SetActive(false);
        OnDone?.Invoke();
    }

    // ── Slide refresh ──────────────────────────────────────────────────────

    void Refresh()
    {
        Slide s      = Slides[_idx];
        bool  isLast = _idx == Slides.Length - 1;

        if (txt_Title   != null) txt_Title.text   = s.title;
        if (txt_Body    != null) txt_Body.text    = s.body;
        if (txt_Counter != null) txt_Counter.text = $"{_idx + 1} / {Slides.Length}";

        if (btn_Back  != null) btn_Back.gameObject.SetActive(_idx > 0);
        if (btn_Next  != null) btn_Next.gameObject.SetActive(!isLast);
        if (btn_Start != null) btn_Start.gameObject.SetActive(isLast);

        if (img_AccentBar != null && accentColors != null && accentColors.Length > 0)
            img_AccentBar.color = accentColors[_idx % accentColors.Length];

        UpdateDots();
    }

    // ── Progress dots ──────────────────────────────────────────────────────

    void BuildDots()
    {
        if (dotsRoot == null || dotPrefab == null) return;
        _dots = new Image[Slides.Length];
        for (int i = 0; i < Slides.Length; i++)
        {
            var go = Instantiate(dotPrefab, dotsRoot);
            _dots[i] = go.GetComponent<Image>();
        }
        UpdateDots();
    }

    void UpdateDots()
    {
        if (_dots == null) return;
        for (int i = 0; i < _dots.Length; i++)
        {
            if (_dots[i] == null) continue;
            bool active = (i == _idx);
            _dots[i].color           = active
                ? new Color(1f, 0.85f, 0.2f, 1f)
                : new Color(1f, 1f,    1f,   0.28f);
            _dots[i].transform.localScale = Vector3.one * (active ? 1.35f : 1f);
        }
    }
}
