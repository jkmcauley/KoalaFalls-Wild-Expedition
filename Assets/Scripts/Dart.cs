using UnityEngine;

public class Dart : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _dartLifeTime = 2f;
    [SerializeField] private float _damage = 30f;
    [SerializeField] private float _stickTime = 3f;

    bool _stuck;
    float _aliveUntil;

    void Start()
    {
        _aliveUntil = Time.time + _dartLifeTime;
    }

    void Update()
    {
        if (_stuck)
            return;

        transform.Translate(Vector3.right * _speed * Time.deltaTime);
        if (Time.time >= _aliveUntil)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_stuck)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable is Player)
            return;

        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        bool strongDart = GameManager.Instance != null && GameManager.Instance.HasStrongDart;
        if (enemy != null)
            enemy.TakeDamage(_damage, strongDart);
        else
            damageable.TakeDamage(_damage);
        StickTo(other);
    }

    void StickTo(Collider2D other)
    {
        _stuck = true;

        Collider2D dartCol = GetComponent<Collider2D>();
        if (dartCol != null)
            dartCol.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Transform host = other.transform;
        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null)
            host = enemy.transform;

        transform.SetParent(host, true);
        Vector3 pos = other.bounds.center;
        if (enemy is Snake || enemy is Skunk)
            pos.y -= other.bounds.extents.y * 0.5f;
        transform.position = pos;
        Destroy(gameObject, _stickTime);
    }
}
