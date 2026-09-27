using UnityEngine;

public class WindChildren : MonoBehaviour
{
    SpriteRenderer _renderer;
    Material _mat;
    Transform[] _kids;
    Vector3[] _restPos;
    Quaternion[] _restRot;
    float _smoothed;

    static readonly int WindTimeId = Shader.PropertyToID("WindTime");
    static readonly int WindMinId = Shader.PropertyToID("WindMinIntensity");
    static readonly int WindMaxId = Shader.PropertyToID("WindMaxIntensity");
    static readonly int WindScaleId = Shader.PropertyToID("WindNoiseScale");
    static readonly int RotFactorId = Shader.PropertyToID("_WindRotationWindFactor");
    static readonly int MaxRotId = Shader.PropertyToID("_WindMaxRotation");
    static readonly int ExtraRotId = Shader.PropertyToID("_WindRotation");

    void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _mat = _renderer.material;

        int count = transform.childCount;
        _kids = new Transform[count];
        _restPos = new Vector3[count];
        _restRot = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            _kids[i] = transform.GetChild(i);
            _restPos[i] = _kids[i].localPosition;
            _restRot[i] = _kids[i].localRotation;
        }
    }

    void LateUpdate()
    {
        if (_mat == null || _kids == null)
            return;

        _smoothed = Mathf.Lerp(_smoothed, -WindAngle(), Time.deltaTime * 6f);
        float angle = _smoothed;
        Vector3 pivot = SpriteBottom();
        float height = _renderer != null && _renderer.sprite != null
            ? Mathf.Max(0.01f, _renderer.sprite.bounds.size.y)
            : 1f;

        for (int i = 0; i < _kids.Length; i++)
        {
            float height01 = Mathf.Clamp01((_restPos[i].y - pivot.y) / height);
            Quaternion rot = Quaternion.Euler(0f, 0f, angle * height01 * Mathf.Rad2Deg);
            _kids[i].localPosition = pivot + rot * (_restPos[i] - pivot);
            _kids[i].localRotation = _restRot[i] * rot;
        }
    }

    Vector3 SpriteBottom()
    {
        if (_renderer == null || _renderer.sprite == null)
            return Vector3.zero;
        Bounds bounds = _renderer.sprite.bounds;
        return new Vector3(bounds.center.x, bounds.min.y, 0f);
    }

    float WindAngle()
    {
        float min = Shader.GetGlobalFloat(WindMinId);
        float max = Shader.GetGlobalFloat(WindMaxId);
        float scale = Shader.GetGlobalFloat(WindScaleId);
        float noise = FastNoise(transform.position.x * scale + Shader.GetGlobalFloat(WindTimeId)) + 0.5f;
        float intensity = Mathf.Lerp(min, max, noise);
        float factor = _mat.HasProperty(RotFactorId) ? _mat.GetFloat(RotFactorId) : 0.1f;
        float extra = _mat.HasProperty(ExtraRotId) ? _mat.GetFloat(ExtraRotId) : 0f;
        float maxRot = _mat.HasProperty(MaxRotId) ? _mat.GetFloat(MaxRotId) : 2f;
        return Mathf.Clamp(factor * intensity + extra, -maxRot, maxRot);
    }

    static float FastNoise(float x)
    {
        float i = Mathf.Floor(x);
        float f = x - i;
        float s = Mathf.Sign(x * 0.5f - Mathf.Floor(x * 0.5f) - 0.5f);
        float k = 0.5f + 0.5f * Mathf.Sin(i);
        return s * f * (f - 1f) * ((16f * k - 4f) * f * (f - 1f) - 1f);
    }
}
