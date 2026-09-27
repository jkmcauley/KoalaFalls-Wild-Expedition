using UnityEngine;

public class PatrolZone : MonoBehaviour
{
    [SerializeField] private Transform _pointA;
    [SerializeField] private Transform _pointB;

    public Transform PointA => Leaf(_pointA);
    public Transform PointB => Leaf(_pointB);

    static Transform Leaf(Transform point)
    {
        return point != null && point.childCount == 1 ? point.GetChild(0) : point;
    }
}
