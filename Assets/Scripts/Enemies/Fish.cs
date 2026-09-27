using UnityEngine;

public class Fish : MonoBehaviour, IDamager
{
    [SerializeField] private float _damage = 25f;

    Animator _animator;
    bool _ready = true;

    public float Damage
    {
        get => _damage;
        set => _damage = value;
    }

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayFishJump()
    {
        _ready = true;
        if (_animator != null)
            _animator.Play("FishJumping", 0, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (!_ready)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        _ready = false;
        player.TakeDamage(Damage);
    }
}
