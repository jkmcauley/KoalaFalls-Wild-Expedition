using UnityEngine;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Kill(other.GetComponentInParent<Player>());
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Kill(collision.collider.GetComponentInParent<Player>());
    }

    void Kill(Player player)
    {
        if (player == null || player.IsDead || GameManager.Instance == null)
            return;

        player.TakeDamage(player.Health > 0f ? player.Health : 100f);
    }
}
