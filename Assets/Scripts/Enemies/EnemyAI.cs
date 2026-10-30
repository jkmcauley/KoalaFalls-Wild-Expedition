using System;
using System.Collections;
using UnityEngine;

public abstract class EnemyAI : MonoBehaviour, IDamageable, IDamager
{
    [Header("Stats")]
    [SerializeField] protected float _health = 60;
    [SerializeField] protected int _coins = 10;
    [SerializeField] protected float _moveSpeed = 2f;
    [SerializeField] protected float _damage = 10f;

    [Header("Patrol")]
    [SerializeField] protected float _idleTime = 2f;
    [SerializeField] protected PatrolZone _assignedZone;
    [SerializeField] protected bool _spriteFacesLeft;

    [Header("Attack")]
    [SerializeField] protected float _attackRange = 2f;

    [Header("Components")]
    [SerializeField] protected SpriteRenderer _spriteRenderer;
    [SerializeField] protected Animator _animator;

    protected Rigidbody2D _rb;
    protected PatrolZone _patrolZone;
    protected Transform _target;
    protected bool _isIdle;
    protected Player _player;
    protected bool _isAttacking;
    protected bool _isDead;
    protected float _maxHealth;
    bool _stayAsleepThisLevel;
    bool _playerInZone;
    float _nextSoundTime;

    public float Health
    {
        get => _health;
        set => _health = value;
    }
    public bool IsDead => _isDead;
    public int Coins => _coins;
    public float MoveSpeed => _moveSpeed;
    public float Damage
    {
        get => _damage;
        set => _damage = value;
    } 


    protected abstract string IdleTrigger { get; }
    protected virtual string WalkTrigger => null;
    protected virtual bool ChaseWhileAttacking => false;
    protected virtual float ContactRange => 0.15f;



    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_animator == null)
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        _maxHealth = _health;
    }

    protected virtual void Start()
    {
        _player = FindFirstObjectByType<Player>();
        if (_assignedZone != null)
            SetPatrolZone(_assignedZone);
    }

    public virtual void SetPatrolZone(PatrolZone zone)
    {
        _patrolZone = zone;
        if (!PointsMoveWithThis())
        {
            Vector3 spawn = transform.position;
            spawn.x = _patrolZone.PointA.position.x;
            transform.position = spawn;
        }
        _target = _patrolZone.PointB;
        FaceTarget();
        StopAllCoroutines();
        StartCoroutine(StartPatrol());
    }

    protected virtual void Update()
    {
        if (_isDead)
            return;
        UpdateZoneSound();
        UpdateAttack();
    }

    protected virtual void UpdateAttack()
    {
        if (_player == null || _player.IsDead)
        {
            if (_isAttacking)
                EndAttack();
            return;
        }

        bool inRange = IsPlayerInAttackRange();
        if (inRange)
        {
            if (!_isAttacking)
                BeginAttack();
            FacePlayer();
        }
        else if (_isAttacking)
        {
            EndAttack();
        }

        UpdateAttackAnim();
    }

    protected virtual bool IsPlayerInAttackRange()
    {
        return DistanceToPlayer() <= _attackRange;
    }

    protected float DistanceToPlayer()
    {
        if (_player == null)
            return float.MaxValue;

        Vector2 from = transform.position;
        Vector2 to = _player.transform.position;

        Collider2D enemyCol = GetComponent<Collider2D>();
        Collider2D playerCol = _player.GetComponent<Collider2D>();
        if (enemyCol != null)
            from = enemyCol.bounds.ClosestPoint(to);
        if (playerCol != null)
            to = playerCol.bounds.ClosestPoint(from);

        return Vector2.Distance(from, to);
    }

    protected virtual void BeginAttack()
    {
        _isAttacking = true;
        if (!ChaseWhileAttacking)
            StopMoving();
    }

    protected virtual void UpdateAttackAnim()
    {
        if (_animator == null)
            return;

        bool showAttack = _isAttacking;
        if (ChaseWhileAttacking)
            showAttack = _isAttacking && DistanceToPlayer() <= ContactRange;

        _animator.SetBool("isAttacking", showAttack);

        if (ChaseWhileAttacking && _isAttacking && !showAttack && !IsWalking() && !string.IsNullOrEmpty(WalkTrigger))
            _animator.Play(WalkTrigger, 0, 0f);
    }

    protected virtual void EndAttack()
    {
        _isAttacking = false;
        StopAttack();
        if (_patrolZone != null)
            FaceTarget();
    }

    protected virtual void FixedUpdate()
    {
        if (_isDead)
        {
            StopMoving();
            return;
        }
        if (_rb == null)
            return;
        if (_isAttacking)
        {
            if (ChaseWhileAttacking)
                MoveToPlayer();
            else
                StopMoving();
            ClampToPatrol();
            return;
        }
        if (_patrolZone == null)
            return;
        if (_isIdle)
        {
            StopMoving();
            ClampToPatrol();
            return;
        }
        Move();
        ClampToPatrol();
    }

    protected virtual void Move()
    {
        float direction = Mathf.Sign(_target.position.x - transform.position.x);
        _rb.linearVelocity = new Vector2(direction * _moveSpeed, _rb.linearVelocity.y);

        if (ReachedTarget())
            StartCoroutine(IdleAndSwitch());
    }

    protected virtual void MoveToPlayer()
    {
        if (_player == null || DistanceToPlayer() <= ContactRange)
        {
            StopMoving();
            return;
        }

        float origin = _spriteRenderer != null ? _spriteRenderer.bounds.center.x : transform.position.x;
        float direction = Mathf.Sign(_player.transform.position.x - origin);
        _rb.linearVelocity = new Vector2(direction * _moveSpeed, _rb.linearVelocity.y);
    }

    protected virtual bool ReachedTarget()
    {
        if (_spriteRenderer == null)
            return Mathf.Abs(transform.position.x - _target.position.x) <= 0.1f;

        Bounds b = _spriteRenderer.bounds;
        if (transform.position.x < _target.position.x)
            return b.max.x >= _target.position.x;
        return b.min.x <= _target.position.x;
    }

    protected virtual void ClampToPatrol()
    {
        if (_patrolZone == null || _spriteRenderer == null)
            return;

        float minX = Mathf.Min(_patrolZone.PointA.position.x, _patrolZone.PointB.position.x);
        float maxX = Mathf.Max(_patrolZone.PointA.position.x, _patrolZone.PointB.position.x);
        Bounds b = _spriteRenderer.bounds;
        float dx = 0f;
        if (b.min.x < minX)
            dx = minX - b.min.x;
        else if (b.max.x > maxX)
            dx = maxX - b.max.x;
        if (dx != 0f)
        {
            transform.position += new Vector3(dx, 0f, 0f);
            StopMoving();
        }
    }

    bool PointsMoveWithThis()
    {
        if (_patrolZone == null)
            return false;
        Transform a = _patrolZone.PointA;
        Transform b = _patrolZone.PointB;
        return (a != null && a.IsChildOf(transform)) || (b != null && b.IsChildOf(transform));
    }

    protected virtual void SwitchTarget()
    {
        _target = _target == _patrolZone.PointA ? _patrolZone.PointB : _patrolZone.PointA;
    }

    protected virtual void FaceTarget()
    {
        FaceX(_target != null ? _target.position.x : transform.position.x);
    }

    protected virtual void FacePlayer()
    {
        if (_player == null)
            return;
        FaceX(_player.transform.position.x);
    }

    protected virtual void FaceX(float worldX)
    {
        if (_spriteRenderer == null)
            return;

        float origin = _spriteRenderer.bounds.center.x;
        float direction = worldX - origin;
        bool flip;
        if (direction > 0.05f)
            flip = _spriteFacesLeft;
        else if (direction < -0.05f)
            flip = !_spriteFacesLeft;
        else
            return;
        if (_spriteRenderer.flipX == flip)
            return;

        float centerOffset = origin - transform.position.x;
        _spriteRenderer.flipX = flip;
        transform.position += new Vector3(centerOffset * 2f, 0f, 0f);
    }

    protected virtual IEnumerator StartPatrol()
    {
        _isIdle = true;
        StopMoving();
        if (_animator != null && !string.IsNullOrEmpty(IdleTrigger))
            _animator.ResetTrigger(IdleTrigger);
        yield return WaitForWalk();
        _isIdle = false;
    }

    protected virtual IEnumerator IdleAndSwitch()
    {
        _isIdle = true;
        StopMoving();
        ClampToPatrol();
        SwitchTarget();
        FaceTarget();
        PlayIdle();
        yield return WaitForWalk();
        _isIdle = false;
    }

    protected virtual IEnumerator WaitForWalk()
    {
        while (IsWalking())
            yield return null;
        while (!IsWalking())
            yield return null;
    }

    protected virtual bool IsWalking()
    {
        return _animator != null
            && !string.IsNullOrEmpty(WalkTrigger)
            && _animator.GetCurrentAnimatorStateInfo(0).IsName(WalkTrigger);
    }

    protected virtual void StopMoving()
    {
        if (_rb != null)
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    protected virtual void PlayIdle()
    {
        if (_isAttacking)
            return;
        if (_animator != null && !string.IsNullOrEmpty(IdleTrigger))
            _animator.SetTrigger(IdleTrigger);
    }

    protected virtual void PlayWalk()
    {
        if (_animator != null && !string.IsNullOrEmpty(WalkTrigger))
            _animator.SetTrigger(WalkTrigger);
    }

    protected virtual void PlayAttack()
    {
        if (_animator != null)
            _animator.SetBool("isAttacking", true);
    }

    protected virtual void StopAttack()
    {
        if (_animator != null)
            _animator.SetBool("isAttacking", false);
    }

    public void Kill()
    {
        TakeDamage(Mathf.Max(_health, 1f), true);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, false);
    }

    public void TakeDamage(float amount, bool strongDart)
    {
        if (strongDart)
            _stayAsleepThisLevel = true;

        if (_isDead)
        {
            if (_stayAsleepThisLevel)
                StopAllCoroutines();
            return;
        }

        _health -= amount;
        if (_health > 0)
            return;

        _health = 0;
        _isDead = true;
        _isAttacking = false;
        _isIdle = true;
        StopAllCoroutines();
        StopAttack();
        StopMoving();
        if (_animator != null)
            _animator.SetBool("isDead", true);
        OnDeath();
        if (!_stayAsleepThisLevel)
            StartCoroutine(AwakeFromSleep());
    }

    protected virtual void OnDeath()
    {
    }

    protected virtual IEnumerator AwakeFromSleep()
    {
        yield return new WaitForSeconds(3f);
        _health = _maxHealth;
        _isDead = false;
        if (_animator != null)
        {
            _animator.SetBool("isDead", false);
            PlayIdle();
        }
        if (_patrolZone != null)
            StartCoroutine(StartPatrol());
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (_isDead || player == null || player.IsDead)
            return;

        if (player._isStabbing)
            TakeDamage(player.Damage);
        else
        {
            player.TakeDamage(Damage);
            TryPlaySound();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null)
            return;
        _nextSoundTime = 0f;
    }

    void UpdateZoneSound()
    {
        if (_player == null || _player.IsDead || _patrolZone == null)
            return;

        bool inZone = PlayerInPatrolZone();
        if (inZone && !_playerInZone)
            TryPlaySound();
        _playerInZone = inZone;
    }

    bool PlayerInPatrolZone()
    {
        Transform a = _patrolZone.PointA;
        Transform b = _patrolZone.PointB;
        if (a == null || b == null)
            return false;

        Vector3 p = _player.transform.position;
        float minX = Mathf.Min(a.position.x, b.position.x);
        float maxX = Mathf.Max(a.position.x, b.position.x);
        float midY = (a.position.y + b.position.y) * 0.5f;
        return p.x >= minX && p.x <= maxX && Mathf.Abs(p.y - midY) < 8f;
    }

    protected virtual void TryPlaySound()
    {
        if (_health <= 0)
        {
            return;
        }

        if (Time.time < _nextSoundTime)
            return;
        _nextSoundTime = Time.time + 2f;
        PlayHitSound();
    }

    protected virtual void PlayHitSound()
    {
    }
}
