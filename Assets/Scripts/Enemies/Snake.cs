using UnityEngine;

public class Snake : EnemyAI
{
    protected override string IdleTrigger => "SnakeIdle";
    protected override string WalkTrigger => "SnakeWalk";

    protected override void Awake()
    {
        _spriteFacesLeft = true;
        base.Awake();
    }

    protected override void PlayHitSound()
    {
        SoundManager.Instance?.PlaySnake();
    }
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (_isDead || player == null || player.IsDead)
            return;
        if (player.HasVenum || (GameManager.Instance != null && GameManager.Instance.HasSnakeVenom))
            return;

        if (player._isStabbing)
            TakeDamage(player.Damage);
        else
        {
            player.TakeDamage(Damage);
            this.TryPlaySound();
        }
    }
}
