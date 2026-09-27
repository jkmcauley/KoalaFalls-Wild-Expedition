using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int _coinValue = 5;
    [SerializeField] private ParticleSystem _goldDust;

    public int CoinValue { get => _coinValue; set => _coinValue = value; }
    public ParticleSystem GoldDust { get => _goldDust; set => _goldDust = value; }
    private Player _player;

    void Awake()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<Collider2D>() == null || child.GetComponent<Coin>() != null)
                continue;

            Coin coin = child.gameObject.AddComponent<Coin>();
            coin.CoinValue = _coinValue;
            coin.GoldDust = _goldDust;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GetComponent<Collider2D>() == null)
            return;
        if (other.GetComponent<Player>() == null && other.GetComponentInParent<Player>() == null)
            return;
        _player = other.GetComponent<Player>();

         _player._coinValue += _coinValue;
         if (GameManager.Instance != null)
             GameManager.Instance.SaveCoins(_player._coinValue);
         SoundManager.Instance?.PlayCoin();
         UiManager.Instance.ShowGemCount(_player._coinValue);

        if (_goldDust != null)
        {
            ParticleSystem dust = Instantiate(_goldDust, transform.position, _goldDust.transform.rotation);
            Destroy(dust.gameObject, 1f);
        }

        Destroy(gameObject);
    }
}
