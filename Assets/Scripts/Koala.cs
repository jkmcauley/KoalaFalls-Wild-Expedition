using UnityEngine;

public class Koala : MonoBehaviour
{
    [SerializeField] private float _reach = 3.5f;
    [SerializeField] private ParticleSystem _confetti;
    [SerializeField] private bool _requiresClimb = true;
    [SerializeField] private int _uiIndex;

    Player _player;
    Vector3 _center;
    bool _rescued;

    void Awake()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;

        if (sprite != null)
        {
            _center = sprite.bounds.center;
            Vector3 localSize = transform.InverseTransformVector(sprite.bounds.size);
            box.size = new Vector2(Mathf.Abs(localSize.x) + _reach, Mathf.Abs(localSize.y) + _reach);
            box.offset = transform.InverseTransformPoint(_center);
        }
        else
        {
            _center = transform.position;
        }
    }

    void Start()
    {
        _player = FindFirstObjectByType<Player>();
    }

    void Update()
    {
        TryRescue();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null)
            TryRescue();
    }

    void TryRescue()
    {
        if (_rescued || _player == null || GameManager.Instance == null || GameManager.Instance.HasWon)
            return;
        if (_requiresClimb && (!_player.HasClimb || !_player.ReachedByClimb))
            return;
        if (!_player.HasKey && !GameManager.Instance.HasKeyKoala)
            return;
        if (Vector2.Distance(_center, _player.transform.position) > _reach)
            return;

        _rescued = true;
        PlayConfetti();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayWin();
        GameManager.Instance.RescueKoala(this);
    }

    void PlayConfetti()
    {
        if (_confetti == null)
            return;

        ParticleSystem burst = Instantiate(_confetti, _center, _confetti.transform.rotation);
        ParticleSystem.MainModule main = burst.main;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        if (!burst.isPlaying)
            burst.Play();
        Destroy(burst.gameObject, 4f);
    }
}
