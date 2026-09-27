using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLegacy : MonoBehaviour
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

    private Rigidbody2D _rb;
    private Collider2D _col;
    private Animator _anim;
    private SpriteRenderer _sprite;
    private InputSystem_Actions _inputs;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _wasGrounded;
    private int _jumps;
    private float _hold;
    private bool _running;
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _anim = GetComponent<Animator>();
        Transform child = transform.Find("Sprite");
        _sprite = child != null ? child.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        _inputs = new InputSystem_Actions();
        _moveAction = _inputs.Player.Move;
        _jumpAction = _inputs.Player.Jump;
    }

    void OnEnable() => _inputs.Player.Enable();
    void OnDisable() => _inputs.Player.Disable();
    void OnDestroy() => _inputs?.Dispose();

    void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();
        CheckGround();

        if (_isGrounded && !_wasGrounded)
            _jumps = 0;
        _wasGrounded = _isGrounded;

        if (_jumpAction.WasPressedThisFrame() && _jumps < 2)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpSpeed);
            _jumps++;
        }

        if (Mathf.Abs(_moveInput.x) > 0.1f)
            _hold += Time.deltaTime;
        else
            _hold = 0f;
        _running = _hold >= _runAfter;

        if (_sprite != null && Mathf.Abs(_moveInput.x) > 0.01f)
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
    }

    void FixedUpdate()
    {
        float vx = _rb.linearVelocity.x;
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

        float vy = _rb.linearVelocity.y;
        if (_isGrounded && _jumps == 0 && vy > 0f)
            vy = 0f;
        _rb.linearVelocity = new Vector2(vx, vy);
    }

    void CheckGround()
    {
        if (_col == null)
        {
            _isGrounded = false;
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(_groundLayer.value == 0 ? (LayerMask)~0 : _groundLayer);
        filter.useTriggers = false;

        int count = _col.Cast(Vector2.down, filter, _hits, _groundCheckDistance);
        _isGrounded = false;
        for (int i = 0; i < count; i++)
        {
            if (_hits[i].collider != null && _hits[i].normal.y > 0.5f)
            {
                _isGrounded = true;
                return;
            }
        }
    }
}
