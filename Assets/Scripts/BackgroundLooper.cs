using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    [SerializeField] private Transform _camera;
    [Tooltip("0 = stays in the world, 1 = locked to the camera. Used when ParallaxLayer is on a parent.")]
    [SerializeField] [Range(0f, 1f)] private float _parallaxSpeed = 0.3f;
    [Tooltip("Pulls loop copies together a little so the wrap seam does not show a crease.")]
    [SerializeField] private float _wrapOverlap = 0f;

    private SpriteRenderer _spriteRenderer;
    private float _backgroundWidth;
    private bool _parentHandlesLoop;

    public float ParallaxSpeed => _parallaxSpeed;
    public float WrapOverlap => _wrapOverlap;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        var parentParallax = GetComponentInParent<ParallaxLayer>();
        _parentHandlesLoop = parentParallax != null && parentParallax.gameObject != gameObject;

        if (_parentHandlesLoop || _spriteRenderer == null || _spriteRenderer.sprite == null)
            return;

        _backgroundWidth = _spriteRenderer.sprite.rect.width
            / _spriteRenderer.sprite.pixelsPerUnit
            * Mathf.Abs(transform.lossyScale.x);
    }

    void Start()
    {
        if (_parentHandlesLoop)
            return;

        if (_camera == null)
        {
            var main = Camera.main;
            if (main != null)
                _camera = main.transform;
        }
    }

    void LateUpdate()
    {
        if (_parentHandlesLoop || _camera == null || _backgroundWidth < 0.01f)
            return;

        float offset = _camera.position.x - transform.position.x;
        int hops = Mathf.RoundToInt(offset / _backgroundWidth);
        if (hops != 0)
            transform.position += Vector3.right * (hops * _backgroundWidth);
    }
}
