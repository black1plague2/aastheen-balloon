using UnityEngine;

public class Balloon : MonoBehaviour
{
    [HideInInspector] public BalloonType balloonType       = BalloonType.Red;
    [HideInInspector] public bool        isTarget          = true;
    [HideInInspector] public float       moveSpeed         = 0.3f;
    [HideInInspector] public float       reactionTimeLimit = -1f;

    private bool     alreadyPopped   = false;
    private float    spawnTime;
    private Renderer balloonRenderer;

    private static OVRSkeleton[] _skeletons;
    private const  float         kProximityRadius = 0.22f;

    private void Start()
    {
        spawnTime       = Time.time;
        balloonRenderer = GetComponentInChildren<Renderer>();

        if (_skeletons == null || _skeletons.Length == 0)
            _skeletons = FindObjectsOfType<OVRSkeleton>();

        if (balloonRenderer != null)
        {
            var mat = new Material(balloonRenderer.sharedMaterial);
            mat.color = BalloonData.GetColor(balloonType);
            balloonRenderer.material = mat;
        }

        if (isTarget && balloonRenderer != null)
        {
            balloonRenderer.material.EnableKeyword("_EMISSION");
            balloonRenderer.material.SetColor("_EmissionColor",
                BalloonData.GetColor(balloonType) * 0.4f);
        }
    }

    private void Update()
    {
        Vector3 target = new Vector3(transform.position.x, transform.position.y, -1f);
        transform.position = Vector3.MoveTowards(transform.position, target,
            moveSpeed * Time.deltaTime);

        if (transform.position.z < 0f)
        {
            if (isTarget) GameManager.Instance?.OnBalloonMissed();
            Destroy(gameObject);
            return;
        }

        if (reactionTimeLimit > 0f && Time.time - spawnTime > reactionTimeLimit)
        {
            if (isTarget) GameManager.Instance?.OnBalloonMissed();
            Destroy(gameObject);
            return;
        }

        CheckHandProximity();
    }

    private void CheckHandProximity()
    {
        if (alreadyPopped || _skeletons == null) return;
        float radius = transform.localScale.x * 0.5f + kProximityRadius;
        foreach (var sk in _skeletons)
        {
            if (sk == null || !sk.IsInitialized) continue;
            foreach (var bone in sk.Bones)
            {
                if (bone.Transform == null) continue;
                if (Vector3.Distance(bone.Transform.position, transform.position) <= radius)
                { Pop(); return; }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "PinTip") Pop();
    }

    private void Pop()
    {
        if (alreadyPopped) return;
        alreadyPopped = true;
        float reactionMs = (Time.time - spawnTime) * 1000f;
        if (isTarget)
            GameManager.Instance?.OnTargetPopped(balloonType, reactionMs,
                transform.localScale.x, transform.position);
        else
            GameManager.Instance?.OnDistractorPopped(balloonType, transform.position);
        Destroy(gameObject);
    }
}
