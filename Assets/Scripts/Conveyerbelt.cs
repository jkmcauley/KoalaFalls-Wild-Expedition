using UnityEngine;

public class ConveyerBelt : MonoBehaviour
{
    [SerializeField] private Transform _pointA;
    [SerializeField] private Transform _pointB;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _arrive = 0.05f;

    Transform _target;
    Rigidbody2D _rb;
    Collider2D[] _cols;
    readonly Collider2D[] _hits = new Collider2D[8];

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();
        _cols = GetComponentsInChildren<Collider2D>();
        _target = _pointB;
    }

    void FixedUpdate()
    {
        if (_pointA == null || _pointB == null || _target == null)
            return;

        Vector2 from = _rb != null ? _rb.position : (Vector2)transform.position;
        Vector2 next = Vector2.MoveTowards(from, _target.position, _speed * Time.fixedDeltaTime);
        Vector2 delta = next - from;

        if (_rb != null)
            _rb.MovePosition(next);
        else
            transform.position = next;

        Player rider = FindRider();
        if (rider != null)
        {
            if (rider.transform.parent == transform)
                rider.transform.SetParent(null);
            rider.AddPlatformDelta(delta);
        }

        if (Vector2.Distance(next, _target.position) <= _arrive)
            _target = _target == _pointA ? _pointB : _pointA;
    }

    Player FindRider()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false;

        for (int c = 0; c < _cols.Length; c++)
        {
            Collider2D col = _cols[c];
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            int count = col.Overlap(filter, _hits);
            for (int i = 0; i < count; i++)
            {
                Player player = _hits[i].GetComponentInParent<Player>();
                if (player == null || player.IsDead)
                    continue;
                if (_hits[i].bounds.min.y < col.bounds.max.y - 0.05f &&
                    player.transform.position.y < col.bounds.center.y)
                    continue;
                return player;
            }
        }

        return null;
    }
}
