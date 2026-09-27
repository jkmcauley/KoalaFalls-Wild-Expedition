using UnityEngine;

[DefaultExecutionOrder(32000)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform _camera;
    [Tooltip("0 = stays in the world, 1 = locked to the camera. Used for this object, or as fallback for children.")]
    [SerializeField] [Range(0f, 1f)] private float _parallaxSpeed = 0.2f;
    [Tooltip("Default seam overlap for child layers that do not set their own.")]
    [SerializeField] private float _wrapOverlap = 0.08f;

    private Camera _cam;
    private float _cameraStartX;
    private bool _started;
    private Layer[] _layers;

    private struct Layer
    {
        public Transform transform;
        public float startX;
        public float startY;
        public float speed;
        public float worldWidth;
    }

    void Start()
    {
        BindCamera();
        if (_camera == null)
            return;

        _cameraStartX = _camera.position.x;
        BuildLayers();
        _started = true;
    }

    void BindCamera()
    {
        if (_camera == null)
        {
            Camera main = Camera.main;
            if (main == null)
                return;
            _camera = main.transform;
            _cam = main;
            return;
        }

        _cam = _camera.GetComponent<Camera>();
        if (_cam == null)
            _cam = _camera.GetComponentInChildren<Camera>();
        if (_cam == null && Camera.main != null)
        {
            _cam = Camera.main;
            _camera = _cam.transform;
        }
    }

    void BuildLayers()
    {
        SpriteRenderer selfRenderer = GetComponent<SpriteRenderer>();
        if (selfRenderer != null)
        {
            _layers = new[] { BuildLayer(transform, selfRenderer, _parallaxSpeed, _wrapOverlap) };
            return;
        }

        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<SpriteRenderer>() != null && !IsLoopCopy(child))
                count++;
        }

        _layers = new Layer[count];
        int index = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null || IsLoopCopy(child))
                continue;

            BackgroundLooper looper = child.GetComponent<BackgroundLooper>();
            float speed = looper != null ? looper.ParallaxSpeed : _parallaxSpeed;
            float overlap = looper != null ? looper.WrapOverlap : _wrapOverlap;
            if (overlap <= 0f)
                overlap = _wrapOverlap;
            _layers[index] = BuildLayer(child, renderer, speed, overlap);
            index++;
        }
    }

    Layer BuildLayer(Transform layerTransform, SpriteRenderer renderer, float speed, float overlap)
    {
        float spriteWidth = LayerWorldWidth(layerTransform, renderer, overlap);
        float worldWidth = Mathf.Max(0.01f, spriteWidth);
        SpawnCopies(layerTransform, renderer, worldWidth);

        return new Layer
        {
            transform = layerTransform,
            startX = layerTransform.position.x,
            startY = layerTransform.position.y,
            speed = speed,
            worldWidth = worldWidth
        };
    }

    static bool IsLoopCopy(Transform t)
    {
        return t != null && t.name.Contains("_loop_");
    }

    static float LayerWorldWidth(Transform layer, SpriteRenderer renderer, float overlap)
    {
        Transform left = null;
        Transform right = null;
        for (int i = 0; i < layer.childCount; i++)
        {
            Transform child = layer.GetChild(i);
            if (child == null || IsLoopCopy(child))
                continue;
            string name = child.name;
            if (left == null && name.IndexOf("left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                left = child;
            else if (right == null && name.IndexOf("right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                right = child;
        }

        if (left != null && right != null)
        {
            float endDistance = Mathf.Abs(right.position.x - left.position.x);
            float pad = overlap > 0.08f ? overlap : 1.5f;
            return Mathf.Max(0.01f, endDistance - pad);
        }

        Bounds? bounds = null;
        SpriteRenderer[] renderers = layer.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer current = renderers[i];
            if (current == null || current.sprite == null || IsLoopCopy(current.transform))
                continue;
            if (current.transform != layer && current.transform.parent != layer)
                continue;
            if (bounds == null)
                bounds = current.bounds;
            else
            {
                Bounds grown = bounds.Value;
                grown.Encapsulate(current.bounds);
                bounds = grown;
            }
        }

        float width = bounds != null ? bounds.Value.size.x : SpriteWorldWidth(renderer);
        return Mathf.Max(0.01f, width - overlap);
    }

    static float SpriteWorldWidth(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
            return 0f;

        return renderer.sprite.rect.width / renderer.sprite.pixelsPerUnit * Mathf.Abs(renderer.transform.lossyScale.x);
    }

    void SpawnCopies(Transform layer, SpriteRenderer source, float worldWidth)
    {
        if (worldWidth < 0.01f || source == null || source.sprite == null)
            return;

        float viewWidth = 40f;
        if (_cam != null && _cam.orthographic)
            viewWidth = _cam.orthographicSize * 2f * _cam.aspect;

        int total = Mathf.Max(3, Mathf.CeilToInt((viewWidth + worldWidth * 2f) / worldWidth));
        if ((total & 1) == 0)
            total++;

        int sides = total / 2;
        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(layer.lossyScale.x));
        float localSpacing = worldWidth / scaleX;

        int childCount = layer.childCount;
        Transform[] decorations = new Transform[childCount];
        int decorationCount = 0;
        for (int c = 0; c < childCount; c++)
        {
            Transform child = layer.GetChild(c);
            if (IsLoopCopy(child))
                continue;
            decorations[decorationCount] = child;
            decorationCount++;
        }

        for (int i = -sides; i <= sides; i++)
        {
            if (i == 0)
                continue;

            GameObject copy = new GameObject(layer.name + "_loop_" + i);
            copy.transform.SetParent(layer, false);
            copy.transform.localPosition = new Vector3(localSpacing * i, 0f, 0f);
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;

            SpriteRenderer destination = copy.AddComponent<SpriteRenderer>();
            destination.sprite = source.sprite;
            destination.color = source.color;
            destination.flipX = source.flipX;
            destination.flipY = source.flipY;
            destination.drawMode = source.drawMode;
            destination.size = source.size;
            destination.sortingLayerID = source.sortingLayerID;
            destination.sortingOrder = source.sortingOrder;
            destination.sharedMaterial = source.sharedMaterial;
            destination.maskInteraction = source.maskInteraction;

            for (int d = 0; d < decorationCount; d++)
                Instantiate(decorations[d].gameObject, copy.transform, false);
        }
    }

    void LateUpdate()
    {
        if (!_started)
            Start();
        if (_camera == null || _layers == null)
            return;

        if (Camera.main != null)
            _camera = Camera.main.transform;

        float cameraX = _camera.position.x;
        float cameraDelta = cameraX - _cameraStartX;

        for (int i = 0; i < _layers.Length; i++)
        {
            Layer layer = _layers[i];
            if (layer.transform == null || layer.worldWidth < 0.01f)
                continue;

            float x = layer.startX + cameraDelta * layer.speed;
            int hops = Mathf.RoundToInt((cameraX - x) / layer.worldWidth);
            if (hops != 0)
            {
                layer.startX += hops * layer.worldWidth;
                x = layer.startX + cameraDelta * layer.speed;
                _layers[i] = layer;
            }

            layer.transform.position = new Vector3(x, layer.startY, layer.transform.position.z);
        }
    }
}
