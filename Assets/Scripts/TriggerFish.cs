using UnityEngine;

public class TriggerFish : MonoBehaviour
{
    [SerializeField] private Fish _fish;

    void Awake()
    {
        if (_fish == null)
            _fish = GetComponentInParent<Fish>() ?? GetComponentInChildren<Fish>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_fish == null)
            return;
        if (other.GetComponent<Player>() == null && other.GetComponentInParent<Player>() == null)
            return;

        _fish.PlayFishJump();
    }
}
