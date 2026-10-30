using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] private float _speed = 8f;
    [SerializeField] private float _hop = 6f;
    [SerializeField] private float _grenadeLifeTime = 1.4f;
    [SerializeField] private float _blastRadius = 2.2f;

    Rigidbody2D _rb;
    float _aliveUntil;
    bool _exploded;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        _aliveUntil = Time.time + _grenadeLifeTime;
        if (_rb == null)
            return;

        Vector2 facing = transform.right;
        _rb.linearVelocity = facing * _speed + Vector2.up * _hop;
    }

    void Update()
    {
        if (Time.time >= _aliveUntil)
            Explode();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded || other == null)
            return;
        if (other.GetComponentInParent<Player>() != null)
            return;

        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null && enemy is not KillMachine)
            return;
        if (other.isTrigger && enemy == null)
            return;

        Explode();
    }

    void Explode()
    {
        if (_exploded)
            return;
        _exploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _blastRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.gameObject == gameObject)
                continue;

            if (hit.GetComponentInParent<EnemyAI>() is KillMachine machine)
                machine.Kill();
        }

        Destroy(gameObject);
    }
}
