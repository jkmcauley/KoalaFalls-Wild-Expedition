using UnityEngine;

public class Cougar : EnemyAI
{
    protected override string IdleTrigger => "CougarIdle";
    protected override string WalkTrigger => "CougarWalk";
    protected override bool ChaseWhileAttacking => true;

    protected override void PlayHitSound()
    {
        SoundManager.Instance?.PlayCougar();
    }

    protected override bool ReachedTarget()
    {
        return Mathf.Abs(transform.position.x - _target.position.x) <= 0.1f;
    }

    protected override void ClampToPatrol()
    {
        if (_patrolZone == null)
            return;

        float minX = Mathf.Min(_patrolZone.PointA.position.x, _patrolZone.PointB.position.x);
        float maxX = Mathf.Max(_patrolZone.PointA.position.x, _patrolZone.PointB.position.x);
        Vector3 pos = transform.position;
        float x = Mathf.Clamp(pos.x, minX, maxX);
        if (x == pos.x)
            return;

        pos.x = x;
        transform.position = pos;
        StopMoving();
    }
}
