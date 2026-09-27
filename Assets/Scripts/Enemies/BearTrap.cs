using UnityEngine;

public class BearTrap : MonoBehaviour, IDamager
{
    [SerializeField] private float _damage = 50f;

    public float Damage
    {
        get => _damage;
        set => _damage = value;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null || player.IsDead)
            return;

        player.TakeDamage(Damage);
        SoundManager.Instance?.PlayBearTrap();
    }
}
