using UnityEngine;

public class Spike : MonoBehaviour, IDamager
{
    [SerializeField] private float _damage = 25f;

    public float Damage
    {
        get => _damage;
        set => _damage = value;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        player.TakeDamage(Damage);
    }
}
