using UnityEngine;
using UnityEngine.Serialization;

public class Shop : MonoBehaviour
{
    public enum Catalog
    {
        Early,
        Late
    }

    public const int Venom = 0;
    public const int Climb = 1;
    public const int Key = 2;
    public const int ExtraLife = 3;
    public const int HealthBoost = 4;
    public const int StrongDart = 5;
    public const int Grenade = 6;

    [SerializeField] private GameObject _panel;
    [SerializeField] private Catalog _catalog = Catalog.Early;
    [SerializeField] private GameObject _earlyItemsRoot;
    [SerializeField] private GameObject _lateItemsRoot;
    private Player _player;
    [SerializeField] private UiManager _uiManager;
    [Header("Early prices")]
    [SerializeField] private int _venumPrice = 200;
    [SerializeField] private int _climbPrice = 400;
    [SerializeField] private int _keyPrice = 100;
    [SerializeField] private TMPro.TextMeshProUGUI _venumPriceText;
    [SerializeField] private TMPro.TextMeshProUGUI _climbPriceText;
    [SerializeField] private TMPro.TextMeshProUGUI _keyPriceText;
    [Header("Late prices")]
    [SerializeField] private int _extraLifePrice = 250;
    [SerializeField] private int _healthBoostPrice = 300;
    [FormerlySerializedAs("_damageBoostPrice")]
    [SerializeField] private int _strongDartPrice = 300;
    [SerializeField] private int _grenadePrice = 300;
    [SerializeField] private TMPro.TextMeshProUGUI _extraLifePriceText;
    [SerializeField] private TMPro.TextMeshProUGUI _healthBoostPriceText;
    [FormerlySerializedAs("_damageBoostPriceText")]
    [SerializeField] private TMPro.TextMeshProUGUI _strongDartPriceText;
    [SerializeField] private GameObject _ladder;
    [SerializeField] private GameObject[] _hudButtons;
    [SerializeField] private int _adRewardCoins = 200;

    private int _selectedItem = -1;
    private int _selectedPrice;

    void Awake()
    {
        CacheHudButtons();
        ApplyCatalogVisibility();
    }

    void OnEnable()
    {
        SetHudButtons(false);
    }

    void OnDisable()
    {
        SetHudButtons(true);
    }

    void Start()
    {
        _player = FindFirstObjectByType<Player>();
        ApplyOwnedItems();
        UpdatePriceLabels();
    }

    public void ApplyOwnedItems()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player>();
        if (_player == null || GameManager.Instance == null)
            return;

        if (_catalog == Catalog.Late)
            GameManager.Instance.GrantEarlyPurchases();

        _player.HasVenum = GameManager.Instance.HasSnakeVenom;
        _player.HasKey = GameManager.Instance.HasKeyKoala;
        _player.HasClimb = GameManager.Instance.HasClimb;

        if (_player.HasClimb)
        {
            if (_ladder != null)
                _ladder.SetActive(true);
            else
                GameManager.Instance.EnableLadders();
        }

        UpdatePriceLabels();
    }

    void ApplyCatalogVisibility()
    {
        GameObject panel = _panel != null ? _panel : gameObject;
        if (_earlyItemsRoot != null && _earlyItemsRoot != panel)
            _earlyItemsRoot.SetActive(_catalog == Catalog.Early);
        if (_lateItemsRoot != null && _lateItemsRoot != panel)
            _lateItemsRoot.SetActive(_catalog == Catalog.Late);
    }

    void CacheHudButtons()
    {
        if (_hudButtons != null && _hudButtons.Length > 0)
            return;

        Transform canvas = transform.parent;
        if (canvas == null)
            return;

        GameObject a = null;
        GameObject b = null;
        for (int i = 0; i < canvas.childCount; i++)
        {
            Transform child = canvas.GetChild(i);
            if (child.name == "Abutton")
                a = child.gameObject;
            else if (child.name == "Bbutton")
                b = child.gameObject;
        }

        if (a == null && b == null)
            return;

        _hudButtons = a != null && b != null
            ? new[] { a, b }
            : a != null
                ? new[] { a }
                : new[] { b };
    }

    void SetHudButtons(bool on)
    {
        if (_hudButtons == null)
            return;

        for (int i = 0; i < _hudButtons.Length; i++)
        {
            if (_hudButtons[i] != null)
                _hudButtons[i].SetActive(on);
        }
    }

    void OnValidate()
    {
        ApplyCatalogVisibility();
        UpdatePriceLabels();
    }

    void UpdatePriceLabels()
    {
        SetPriceLabel(_venumPriceText, _venumPrice, IsOwned(Venom));
        SetPriceLabel(_climbPriceText, _climbPrice, IsOwned(Climb));
        SetPriceLabel(_keyPriceText, _keyPrice, IsOwned(Key));
        SetPriceLabel(_extraLifePriceText, _extraLifePrice, IsOwned(ExtraLife));
        SetPriceLabel(_healthBoostPriceText, _healthBoostPrice, IsOwned(HealthBoost));
        if (_catalog == Catalog.Late)
            SetPriceLabel(_strongDartPriceText, _grenadePrice, IsOwned(Grenade));
        else
            SetPriceLabel(_strongDartPriceText, _strongDartPrice, IsOwned(StrongDart));
    }

    static void SetPriceLabel(TMPro.TextMeshProUGUI label, int price, bool owned)
    {
        if (label == null)
            return;
        label.text = owned ? "OWNED" : $"{price}G";
    }

    public void SelectItem(int item)
    {
        if (_catalog == Catalog.Late && item >= Venom && item <= Key)
            item = item == Key ? Grenade : item + ExtraLife;
        _selectedItem = item;
        _selectedPrice = PriceFor(item);
        if (_selectedPrice < 0)
        {
            _selectedItem = -1;
            _selectedPrice = 0;
            return;
        }

        RefreshGemCount();
    }

    public void BuyItem()
    {
        if (_player == null)
            return;

        if (_selectedItem < 0)
        {
            Debug.Log("Select an item first.");
            return;
        }

        if (IsOwned(_selectedItem))
        {
            Debug.Log("You already own this item.");
            RefreshGemCount();
            return;
        }

        if (_player._coinValue < _selectedPrice)
        {
            Debug.Log("Not enough diamonds.");
            RefreshGemCount();
            return;
        }

        _player._coinValue -= _selectedPrice;
        if (GameManager.Instance != null)
            GameManager.Instance.SaveCoins(_player._coinValue);
        GrantItem(_selectedItem);
        UpdatePriceLabels();
        RefreshGemCount();
    }

    public void WatchAdForCoins()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player>();
        if (_player == null)
            return;

        if (AdsManager.Instance == null)
        {
            Debug.Log("Rewarded ad is not ready.");
            return;
        }

        AdsManager.Instance.ShowRewarded(GrantAdCoins);
    }

    void GrantAdCoins()
    {
        if (_player == null)
            return;

        _player._coinValue += _adRewardCoins;
        if (GameManager.Instance != null)
            GameManager.Instance.SaveCoins(_player._coinValue);
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayCoin();
        RefreshGemCount();
    }

    int PriceFor(int item)
    {
        switch (item)
        {
            case Venom: return _venumPrice;
            case Climb: return _climbPrice;
            case Key: return _keyPrice;
            case ExtraLife: return _extraLifePrice;
            case HealthBoost: return _healthBoostPrice;
            case StrongDart: return _strongDartPrice;
            case Grenade: return _grenadePrice;
            default: return -1;
        }
    }

    bool IsOwned(int item)
    {
        if (_player == null && GameManager.Instance == null)
            return false;

        switch (item)
        {
            case Venom:
                return (_player != null && _player.HasVenum)
                    || (GameManager.Instance != null && GameManager.Instance.HasSnakeVenom);
            case Climb:
                return (_player != null && _player.HasClimb)
                    || (GameManager.Instance != null && GameManager.Instance.HasClimb);
            case Key:
                return (_player != null && _player.HasKey)
                    || (GameManager.Instance != null && GameManager.Instance.HasKeyKoala);
            case ExtraLife:
                return GameManager.Instance != null && GameManager.Instance.HasExtraLife;
            case HealthBoost:
                return GameManager.Instance != null && GameManager.Instance.HasHealthBoost;
            case StrongDart:
                return GameManager.Instance != null && GameManager.Instance.HasStrongDart;
            case Grenade:
                return GameManager.Instance != null && GameManager.Instance.HasGrenade;
            default:
                return false;
        }
    }

    void GrantItem(int item)
    {
        if (GameManager.Instance == null)
            return;

        switch (item)
        {
            case Venom:
                _player.HasVenum = true;
                GameManager.Instance.HasSnakeVenom = true;
                break;
            case Climb:
                _player.HasClimb = true;
                GameManager.Instance.HasClimb = true;
                if (_ladder != null)
                    _ladder.SetActive(true);
                break;
            case Key:
                _player.HasKey = true;
                GameManager.Instance.HasKeyKoala = true;
                break;
            case ExtraLife:
                GameManager.Instance.HasExtraLife = true;
                GameManager.Instance.SaveLives(GameManager.Instance.Lives + 1);
                _player._lives = GameManager.Instance.Lives;
                if (UiManager.Instance != null)
                    UiManager.Instance.RefreshLives(_player._lives);
                break;
            case HealthBoost:
                GameManager.Instance.HasHealthBoost = true;
                _player._maxHealth += GameManager.Instance.HealthBoostAmount;
                _player._health = _player._maxHealth;
                if (UiManager.Instance != null)
                    UiManager.Instance.UpdateHealthUI(_player._health, _player._maxHealth);
                break;
            case StrongDart:
                GameManager.Instance.HasStrongDart = true;
                break;
            case Grenade:
                GameManager.Instance.HasGrenade = true;
                break;
        }
    }

    void RefreshGemCount()
    {
        if (_player == null)
            return;

        if (UiManager.Instance != null)
            UiManager.Instance.ShowGemCount(_player._coinValue);
        else if (_uiManager != null)
            _uiManager.ShowGemCount(_player._coinValue);
    }
}
