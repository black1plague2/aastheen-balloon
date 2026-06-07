using UnityEngine;

public class PopEffectController : MonoBehaviour
{
    public static PopEffectController Instance;

    [Header("Pop Sounds")]
    public AudioClip correctPopSound;
    public AudioClip incorrectPopSound;
    public AudioClip wrongSequenceSound;

    [Header("Reward Sounds")]
    public AudioClip streakBonusSound;
    public AudioClip badgeEarnedSound;
    public AudioClip sessionCompleteSound;

    private AudioSource _audio;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 0f;
        _audio.playOnAwake  = false;
    }

    // ── Pop effects ────────────────────────────────────────────────────────────

    public void PlayCorrectPop(Vector3 position, BalloonType type)
    {
        SpawnParticles(position, BalloonData.GetColor(type), 30, 4f, 0.06f);
        Play(correctPopSound);
    }

    public void PlayIncorrectPop(Vector3 position)
    {
        SpawnParticles(position, new Color(0.5f, 0.5f, 0.5f), 12, 1.5f, 0.04f);
        Play(incorrectPopSound);
    }

    // Wrong colour in sequence mode — red flash burst
    public void PlayWrongSequencePop(Vector3 position)
    {
        SpawnParticles(position, new Color(1f, 0.15f, 0.15f), 20, 2.5f, 0.05f);
        Play(wrongSequenceSound != null ? wrongSequenceSound : incorrectPopSound);
    }

    // ── Reward effects ─────────────────────────────────────────────────────────

    public void PlayStreakMilestone()
    {
        Play(streakBonusSound);
    }

    public void PlayBadgeEarned()
    {
        Play(badgeEarnedSound);
    }

    public void PlaySessionComplete()
    {
        Play(sessionCompleteSound);
    }

    // ── Internals ──────────────────────────────────────────────────────────────

    private void Play(AudioClip clip)
    {
        if (clip != null && _audio != null)
            _audio.PlayOneShot(clip);
    }

    private void SpawnParticles(Vector3 position, Color color, int count, float speed, float size)
    {
        var go = new GameObject("PopFX");
        go.transform.position = position;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main           = ps.main;
        main.duration      = 0.4f;
        main.loop          = false;
        main.startLifetime = 0.5f;
        main.startSpeed    = speed;
        main.startSize     = size;
        main.startColor    = new ParticleSystem.MinMaxGradient(color, color * 0.55f);
        main.stopAction    = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var shape       = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.08f;

        ps.Play();
    }
}
