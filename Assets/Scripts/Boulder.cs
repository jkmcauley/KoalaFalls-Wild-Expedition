using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Boulder : MonoBehaviour, IDamager
{
    [SerializeField] private float startingSpeed = 4f;
    [SerializeField] private float minRollSpeed = 3.5f;
    [SerializeField] private float lifetime = 15f;
    [SerializeField] private bool rollLeft = true;
    [SerializeField] private float hitShake = 1.1f;
    [SerializeField] private float rollShake = 0.22f;
    [SerializeField] private float rollShakeEvery = 0.12f;
    [SerializeField] private float playerKnockX = 8f;
    [SerializeField] private float playerKnockY = 6f;
    [SerializeField] private float ignoreSpawnSoundFor = 0.4f;
    [SerializeField] private float minAirTime = 0.08f;
    [SerializeField] private float landSoundCooldown = 0.7f;
    [SerializeField] private float _damage = 60f;
    [SerializeField] private float playerHitCooldown = 0.4f;

    Rigidbody2D _rb;
    CinemachineImpulseSource _impulse;
    float _dir = -1f;
    float _nextRollShake;
    float _ignoreSoundUntil;
    float _nextLandSound;
    float _groundedUntil;
    float _airTime;
    bool _wasGrounded;
    float _nextPlayerHit;

    public float Damage
    {
        get => _damage;
        set => _damage = value;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _impulse = GetComponent<CinemachineImpulseSource>();
        _dir = rollLeft ? -1f : 1f;
        _rb.mass = 100f;
    }

    void Start()
    {
        _rb.linearVelocity = new Vector2(_dir * startingSpeed, _rb.linearVelocity.y);
        transform.Rotate(0f, 0f, 90f);
        _ignoreSoundUntil = Time.time + ignoreSpawnSoundFor;
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        Vector2 v = _rb.linearVelocity;
        if (Mathf.Abs(v.x) < minRollSpeed)
            v.x = _dir * minRollSpeed;
        else
            v.x = _dir * Mathf.Abs(v.x);
        _rb.linearVelocity = v;

        if (Time.time >= _nextRollShake && Mathf.Abs(v.x) >= minRollSpeed * 0.5f)
        {
            _nextRollShake = Time.time + rollShakeEvery;
            Shake(rollShake);
        }

        bool grounded = Time.time < _groundedUntil;
        float airBefore = _airTime;
        if (grounded)
            _airTime = 0f;
        else
            _airTime += Time.fixedDeltaTime;

        if (Time.time >= _ignoreSoundUntil
            && Time.time >= _nextLandSound
            && grounded
            && !_wasGrounded
            && airBefore >= minAirTime)
        {
            _nextLandSound = Time.time + landSoundCooldown;
            SoundManager.Instance?.PlayBoulder();
        }

        _wasGrounded = grounded;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (HitPlayer(collision.collider))
            return;
        if (HitEnemy(collision.collider))
            return;

        MarkGround(collision);
    }

    static bool HitGround(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
                return true;
        }
        return false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.GetComponentInParent<Player>() != null)
            KeepRolling();
        MarkGround(collision);
    }

    void MarkGround(Collision2D collision)
    {
        if (HitGround(collision))
            _groundedUntil = Time.time + 0.05f;
    }

    void KeepRolling()
    {
        float speed = Mathf.Max(minRollSpeed, Mathf.Abs(_rb.linearVelocity.x), startingSpeed);
        _rb.linearVelocity = new Vector2(_dir * speed, _rb.linearVelocity.y);
    }

    void Shake(float force)
    {
        if (_impulse == null)
            return;
        _impulse.GenerateImpulseWithForce(force);
      
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (HitPlayer(other))
            return;
        HitEnemy(other);
    }

    bool HitPlayer(Component other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return false;

        KeepRolling();
        player.KnockBack(new Vector2(_dir * playerKnockX, playerKnockY), 0.2f);
        Shake(hitShake);
        if (Time.time >= _nextPlayerHit)
        {
            _nextPlayerHit = Time.time + playerHitCooldown;
            player.TakeDamage(Damage);
        }
        return true;
    }

    bool HitEnemy(Component other)
    {
        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy == null)
            return false;

        KeepRolling();
        enemy.Kill();
        Shake(hitShake);
        return true;
    }
}
