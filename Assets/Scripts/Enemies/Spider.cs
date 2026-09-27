using UnityEngine;

public class Spider : EnemyAI
{
    protected override string IdleTrigger => "SpiderIdle";
    protected override string WalkTrigger => "SpiderWalk";
    protected override bool ChaseWhileAttacking => true;

    protected override bool IsPlayerInAttackRange()
    {
        if (_player == null)
            return false;
        return Vector2.Distance(transform.position, _player.transform.position) <= _attackRange;
    }

    protected override void UpdateAttackAnim()
    {
        if (_animator == null)
            return;

        _animator.SetBool("isAttacking", _isAttacking);
        if (!_isAttacking)
            return;
        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("SpiderAttack"))
            _animator.Play("SpiderAttack", 0, 0f);
    }

    protected override void PlayHitSound()
    {
        SoundManager.Instance?.PlaySpider();
    }
 
}
