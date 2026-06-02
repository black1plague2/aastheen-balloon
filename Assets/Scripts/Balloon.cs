using UnityEngine;

public class Balloon : MonoBehaviour
{
    private bool alreadyPopped = false;
    private const float MoveSpeed = 0.3f;

    private void Start()
    {
        float s = GameManager.Instance != null ? GameManager.Instance.balloonSize : 0.3f;
        transform.localScale = new Vector3(s, s, s);
    }

    private void Update()
    {
        Vector3 target = new Vector3(transform.position.x, transform.position.y, -1f);
        transform.position = Vector3.MoveTowards(transform.position, target, MoveSpeed * Time.deltaTime);

        if (transform.position.z < 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Balloon trigger entered by: " + other.gameObject.name);

        if (other.gameObject.name == "PinTip")
            Pop();
    }

    private void Pop()
    {
        if (alreadyPopped) return;
        alreadyPopped = true;

        GameManager.Instance.OnRepCompleted(
            GameManager.Instance.latestRotation,
            0f);

        SpawnPopParticle();
        Destroy(gameObject);
    }

    private void SpawnPopParticle()
    {
        var go = new GameObject("PopParticle");
        go.transform.position = transform.position;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main           = ps.main;
        main.duration      = 0.3f;
        main.loop          = false;
        main.startLifetime = 0.3f;
        main.startSpeed    = 2f;
        main.startSize     = 0.05f;
        main.startColor    = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
        main.stopAction    = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape       = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.1f;

        ps.Play();
    }
}
