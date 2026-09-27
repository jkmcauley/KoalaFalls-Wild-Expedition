using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Player : MonoBehaviour, IDamageable, IDamager
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _runSpeed = 9f;
    [SerializeField] private float _runAfter = 0.35f;
    [SerializeField] private float _runAcceleration = 8f;
    [SerializeField] private float _deceleration = 30f;
    [SerializeField] private float _jumpSpeed = 8f;
    [SerializeField] [Range(0f, 1f)] private float _airControl = 0.35f;
    [SerializeField] private LayerMask _groundLayer = ~0;
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] private float _wallCheckDistance = 0.15f;
    [SerializeField] private float _wallJumpX = 10f;
    [SerializeField] private float _wallJumpY = 16f;
    [SerializeField] private float _wallSlideSpeed = 3f;
    [SerializeField] private float _wallJumpLock = 0.12f;
    [SerializeField] private GameObject _dartPrefab;
    [SerializeField] private Transform _muzzlePoint;
    [SerializeField] private float _fireRate = .5f;
    [SerializeField] private float _meleeRange = 1.35f;
    [SerializeField] private float _stabTime = 0.9f;
    private float _nextFireTime;
    private float _stabUntil;
    private bool _fellOutOfLevel;

    private Rigidbody2D _rb;
    private Collider2D _col;
    public Animator _anim;
    private SpriteRenderer _sprite;
    private InputSystem_Actions _inputs;
    private InputAction _moveAction;
    private InputAction _attackAction;
    private InputAction _jumpAction;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _onWall;
    private float _wallDir;
    private float _lockMoveUntil;
    private int _jumps;
    private float _hold;
    private bool _running;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
    private readonly Collider2D[] _meleeHits = new Collider2D[8];
    private bool _isShooting;
    public bool _isStabbing;
    bool _isDead;
    public int _lives = 3;
    public int _maxLives;
    public bool IsDead => _isDead;
    public bool _isClimbing;
    [SerializeField]private ParticleSystem _dustParticles;
    [SerializeField] public int _coinValue;
    [SerializeField] public float _health;
    [SerializeField] private float _damage = 60f;
    public float Health
    {
        get => _health;
        set => _health = value;
    }

    public float _maxHealth;

    public float Damage
    {
        get => _damage;
        set => _damage = value;
    }
    
    public bool HasVenum;
    public bool HasClimb;
    public bool HasKey;
    public bool ReachedByClimb;
    Vector2 _platformDelta;


    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        Collider2D[] cols = GetComponents<Collider2D>();
        _col = null;
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && cols[i].enabled)
            {
                _col = cols[i];
                break;
            }
        }
        _anim = GetComponent<Animator>();
        Transform child = transform.Find("Sprite");
        _sprite = child != null ? child.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        _inputs = new InputSystem_Actions();
        _moveAction = _inputs.Player.Move;
        _jumpAction = _inputs.Player.Jump;
        _attackAction = _inputs.Player.Attack;
        _maxHealth = _health;
        _maxLives = _lives;
    }

    void Start()
    {
        if (GameManager.Instance == null)
            return;
        if (GameManager.Instance.HasClimb)
            HasClimb = true;
        if (GameManager.Instance.HasKeyKoala)
            HasKey = true;
        if (GameManager.Instance.HasSnakeVenom)
            HasVenum = true;
    }

    void OnEnable() => _inputs.Player.Enable();
    void OnDisable() => _inputs.Player.Disable();
    void OnDestroy() => _inputs?.Dispose();

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (!_fellOutOfLevel && transform.position.y < -33f)
        {
            _fellOutOfLevel = true;
            _health = 0;
            _isDead = true;
            LoseLife();
            return;
        }

        if (_isDead || _isClimbing)
            return;

        _moveInput = _moveAction.ReadValue<Vector2>();
        CheckGround();
        CheckWall();

        if (_isGrounded && !_wasGrounded)
        {
            _jumps = 0;
            PlayDust();
        }
        _wasGrounded = _isGrounded;

        if (_jumpAction.WasPressedThisFrame())
        {
            if (!_isGrounded && _onWall)
                WallJump();
            else if (_jumps < 2)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpSpeed);
                _jumps++;
                PlayDust();
            }
        }
     

        _isStabbing = Time.time < _stabUntil;
        bool close = EnemyInMelee();
        _isShooting = !_isStabbing && !close && _attackAction.IsPressed();
        if (_attackAction.WasPressedThisFrame() && !_isStabbing)
        {
            if (close)
                Stab();
            else
                Shoot();
        }

        if (Mathf.Abs(_moveInput.x) > 0.1f)
            _hold += Time.deltaTime;
        else
            _hold = 0f;
        _running = _hold >= _runAfter;

        if (_sprite != null && Time.time >= _lockMoveUntil && Mathf.Abs(_moveInput.x) > 0.01f)
            _sprite.flipX = _moveInput.x < 0;

        if (_anim == null)
            return;

        int move = 0;
        if (_running)
            move = 2;
        else if (_hold > 0f)
            move = 1;

        _anim.SetInteger("Move", move);
        _anim.SetBool("isJumping", !_isGrounded && _jumps > 0);
        _anim.SetBool("isShooting", _isShooting);
        _anim.SetBool("isStabbing", _isStabbing);
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0f)
            return;
        if (_isDead || _isClimbing)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        float vx = _rb.linearVelocity.x;
        float vy = _rb.linearVelocity.y;

        if (Time.time >= _lockMoveUntil)
        {
            float decel = _isGrounded ? _deceleration : _deceleration * _airControl;
            if (Mathf.Abs(_moveInput.x) > 0.1f)
            {
                float sign = Mathf.Sign(_moveInput.x);
                float target = (_isGrounded && _running ? _runSpeed : _moveSpeed) * sign;
                vx = target;
            }
            else
            {
                vx = Mathf.MoveTowards(vx, 0f, decel * Time.fixedDeltaTime);
            }
        }

        if (_isGrounded && _jumps == 0 && vy > 0f)
            vy = 0f;
        if (!_isGrounded && _onWall && vy < -_wallSlideSpeed)
            vy = -_wallSlideSpeed;

        _rb.linearVelocity = new Vector2(vx, vy);
        if (_platformDelta.sqrMagnitude > 0f)
        {
            _rb.position += _platformDelta;
            _platformDelta = Vector2.zero;
        }
    }

    public void AddPlatformDelta(Vector2 delta)
    {
        _platformDelta += delta;
    }

    void WallJump()
    {
        _rb.linearVelocity = new Vector2(-_wallDir * _wallJumpX, _wallJumpY);
        _jumps = 1;
        _lockMoveUntil = Time.time + _wallJumpLock;
        if (_sprite != null)
            _sprite.flipX = _wallDir > 0f;
        PlayDust();
    }

    void Stab()
    {
        _isStabbing = true;
        _stabUntil = Time.time + _stabTime;
        _isShooting = false;
        HitEnemiesInMelee();
    }

    void Shoot()
    {
        if (_dartPrefab == null || Time.time < _nextFireTime)
            return;

        _nextFireTime = Time.time + _fireRate;

        Transform spawn = _muzzlePoint != null ? _muzzlePoint : transform;
        bool faceLeft = _sprite != null && _sprite.flipX;
        Vector3 pos = spawn.position;
        if (faceLeft)
            pos.x = transform.position.x - (spawn.position.x - transform.position.x);
        Quaternion rot = faceLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        Instantiate(_dartPrefab, pos, rot);
        SoundManager.Instance?.PlayShoot();
    }

    bool EnemyInMelee()
    {
        return ForEachEnemyInMelee(null);
    }

    void HitEnemiesInMelee()
    {
        ForEachEnemyInMelee(enemy => enemy.TakeDamage(Damage));
    }

    bool ForEachEnemyInMelee(System.Action<EnemyAI> onHit)
    {
        if (_col == null)
            return false;

        float dir = _sprite != null && _sprite.flipX ? -1f : 1f;
        Bounds b = _col.bounds;
        Vector2 size = new Vector2(b.size.x + _meleeRange, b.size.y);
        Vector2 center = new Vector2(b.center.x + dir * (_meleeRange * 0.5f), b.center.y);

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = true;

        int count = Physics2D.OverlapBox(center, size, 0f, filter, _meleeHits);
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _meleeHits[i];
            if (hit == null || hit == _col)
                continue;
            EnemyAI enemy = hit.GetComponent<EnemyAI>() ?? hit.GetComponentInParent<EnemyAI>();
            if (enemy == null)
                continue;
            found = true;
            onHit?.Invoke(enemy);
        }
        return found;
    }


    void CheckGround()
    {
        _isGrounded = HasHit(Vector2.down, _groundCheckDistance, 0.5f, true);
    }

    void CheckWall()
    {
        _onWall = false;
        _wallDir = 0f;
        if (_isGrounded)
            return;
        if (HasHit(Vector2.right, _wallCheckDistance, 0.7f, false))
        {
            _onWall = true;
            _wallDir = 1f;
        }
        else if (HasHit(Vector2.left, _wallCheckDistance, 0.7f, false))
        {
            _onWall = true;
            _wallDir = -1f;
        }
    }

    bool HasHit(Vector2 dir, float distance, float normalMin, bool wantFloor)
    {
        if (_col == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(_groundLayer.value == 0 ? (LayerMask)~0 : _groundLayer);
        filter.useTriggers = false;

        int count = _col.Cast(dir, filter, _hits, distance);
        for (int i = 0; i < count; i++)
        {
            if (_hits[i].collider == null)
                continue;
            if (_hits[i].collider.GetComponent<NoWallClimb>() != null
                || _hits[i].collider.GetComponentInParent<NoWallClimb>() != null)
                continue;
            float n = wantFloor ? _hits[i].normal.y : Mathf.Abs(_hits[i].normal.x);
            if (n > normalMin)
                return true;
        }
        return false;
    }
    void PlayDust()
    {
        if (_dustParticles == null)
            return;

        _dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _dustParticles.Play();
    }

    public void KnockBack(Vector2 velocity, float lockTime = 0.2f)
    {
        if (_rb == null || _isDead)
            return;
        _rb.linearVelocity = velocity;
        _lockMoveUntil = Time.time + lockTime;
    }

    public void SetClimbing(bool climbing)
    {
        _isClimbing = climbing;
        if (!climbing)
        {
            _jumps = 0;
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
        }
        if (_anim != null)
            _anim.SetBool("isClimbing", climbing);
    }

    public void TakeDamage(float damage)
    {
        if (_isDead)
            return;

        _health -= damage;
        if (UiManager.Instance != null)
            UiManager.Instance.UpdateHealthUI(_health, _maxHealth);
        if (_health > 0)
            return;

        _health = 0;
        _isDead = true;
        _isStabbing = false;
        _isShooting = false;
        if (_rb != null)
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        if (_anim != null)
            _anim.SetBool("isDead", true);
        LoseLife();
    }

    void LoseLife()
    {
        if (_lives > 0)
            _lives--;

        if (GameManager.Instance != null)
            GameManager.Instance.SaveLives(_lives);

        if (UiManager.Instance != null)
        {
            UiManager.Instance.UpdateLives(_lives, () =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.Gameover();
            });
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Gameover();
    }
}
