using UnityEngine;

public class Gator : EnemyAI
{
    protected override string IdleTrigger => "GatorIdle";
    protected override string WalkTrigger => "GatorWalk";
    protected override bool ChaseWhileAttacking => true;

    protected override void PlayHitSound()
    {
        SoundManager.Instance?.PlayGator();
    }
}
