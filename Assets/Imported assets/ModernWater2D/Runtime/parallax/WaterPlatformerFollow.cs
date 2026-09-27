using UnityEngine;

namespace Water2D
{
    [ExecuteAlways]
    public class WaterPlatformerFollow : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] float x_offset;
        [Tooltip("0 = stays in the world, 1 = locked to the camera.")]
        [SerializeField] [Range(0f, 1f)] float followSpeed = 1f;

        float _originX;
        float _originCamX;
        bool _ready;

        void OnEnable()
        {
            _ready = false;
        }

        void LateUpdate() => Follow();

        void Follow()
        {
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            if (!_ready)
            {
                _originX = transform.position.x;
                _originCamX = cam.transform.position.x;
                _ready = true;
            }

            float x = _originX + (cam.transform.position.x - _originCamX) * followSpeed + x_offset;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }
    }
}
