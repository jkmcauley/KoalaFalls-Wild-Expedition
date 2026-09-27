using UnityEngine;

public class Bear : EnemyAI
{
    protected override string IdleTrigger => "BearIdle";
    protected override string WalkTrigger => "BearWalk";
    protected override bool ChaseWhileAttacking => true;

    protected override void PlayHitSound()
    {
        SoundManager.Instance?.PlayBear();
    }
}
