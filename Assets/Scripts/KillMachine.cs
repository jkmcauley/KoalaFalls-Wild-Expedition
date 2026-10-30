using System.Collections;
using UnityEngine;

public class KillMachine : EnemyAI
{ 
    protected override string IdleTrigger => "KillIdle";
    protected override string WalkTrigger => "KillMove";
    protected override bool ChaseWhileAttacking => true;

    protected override void Awake()
    {
        _spriteFacesLeft = true;
        base.Awake();
    }

    protected override void UpdateAttackAnim()
    {
        if (_animator == null)
            return;

        _animator.SetBool("isAttacking", _isAttacking);
    }

    protected override void PlayHitSound()
    {
        SoundManager.Instance?.PlayKillMachine();
    }

    protected override void OnDeath()
    {
        SoundManager.Instance?.PlayExplosion();
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null)
                cols[i].enabled = false;
        }

        if (_rb != null)
            _rb.simulated = false;
        StartCoroutine(RemoveAfterExplosion());
    }

    protected override IEnumerator AwakeFromSleep()
    {
        yield break;
    }

    IEnumerator RemoveAfterExplosion()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
    
}
