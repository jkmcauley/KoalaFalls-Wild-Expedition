using UnityEngine;

public class Spring : MonoBehaviour
{
    [SerializeField] private float _bounceSpeed = 18f;
    Animator _anim;
    Collider2D _col;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        _col = GetComponent<Collider2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Player player = collision.collider.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return;
        if (!LandedOnTop(collision.collider))
            return;

        player.Bounce(_bounceSpeed);
        if (_anim != null)
            _anim.SetTrigger("SpringFire");
    }

    bool LandedOnTop(Collider2D other)
    {
        float top = _col != null ? _col.bounds.max.y : transform.position.y;
        return other.bounds.center.y >= top;
    }
}
