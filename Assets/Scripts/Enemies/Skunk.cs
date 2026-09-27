using UnityEngine;

public class Skunk : EnemyAI
{
    protected override string IdleTrigger => "SkunkIdle";

    protected override void PlayHitSound()
    {
        SoundManager.Instance?.PlaySkunk();
    }
}
