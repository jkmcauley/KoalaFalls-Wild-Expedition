using System.Collections;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    [SerializeField] private Transform _topPoint;
    [SerializeField] private float _climbSpeed = 4f;
    [SerializeField] private Player _player;

    bool _climbing;
    bool _ignoreUntilExit;

    void OnTriggerEnter2D(Collider2D other)
    {
        TryStartClimb(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryStartClimb(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null)
            return;
        if (!_climbing)
            _ignoreUntilExit = false;
    }

    void TryStartClimb(Collider2D other)
    {
        if (_climbing || _ignoreUntilExit || _topPoint == null)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            player = _player;
        if (player == null)
            return;

        if (!player.HasClimb && GameManager.Instance != null && GameManager.Instance.HasClimb)
            player.HasClimb = true;
        if (!player.HasClimb)
            return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = other.attachedRigidbody;
        if (rb == null)
            return;
        if (rb.position.y >= _topPoint.position.y - 0.1f)
            return;

        StartCoroutine(Climb(player, rb));
    }

    IEnumerator Climb(Player player, Rigidbody2D playerRb)
    {
        _climbing = true;
        player.SetClimbing(true);

        float normalGravity = playerRb.gravityScale;
        playerRb.gravityScale = 0f;
        playerRb.linearVelocity = Vector2.zero;

        while (playerRb.position.y < _topPoint.position.y - 0.05f)
        {
            Vector2 next = playerRb.position;
            next.x = Mathf.MoveTowards(next.x, _topPoint.position.x, _climbSpeed * Time.fixedDeltaTime);
            next.y = Mathf.MoveTowards(next.y, _topPoint.position.y, _climbSpeed * Time.fixedDeltaTime);
            playerRb.MovePosition(next);
            yield return new WaitForFixedUpdate();
        }

        playerRb.position = _topPoint.position;
        playerRb.linearVelocity = Vector2.zero;
        playerRb.gravityScale = normalGravity;
        player.ReachedByClimb = true;
        player.SetClimbing(false);
        _climbing = false;
        _ignoreUntilExit = true;
    }
}
