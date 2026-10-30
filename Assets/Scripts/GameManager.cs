using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    private static GameManager _instance;

    public static GameManager Instance

    {

        get

        {

            if (_instance == null)

            {

                _instance = FindFirstObjectByType<GameManager>();

            }

            return _instance;

        }

    }

    public bool HasWon => _won;
    bool _won;
    public float HealthBoostAmount = 50f;

    static bool _hasKeyKoala;
    static bool _hasSnakeVenom;
    static bool _hasClimb;
    static bool _hasExtraLife;
    static bool _hasHealthBoost;
    static bool _hasStrongDart;
    static bool _hasGrenade;
    static int _coins;
    static int _lives = 3;
    static int _koalasRescued;
    static readonly HashSet<string> _rescuedKoalaIds = new HashSet<string>();
    static bool _coinsSaved;
    static bool _livesSaved;

    public bool HasKeyKoala { get => _hasKeyKoala; set => _hasKeyKoala = value; }
    public bool HasSnakeVenom { get => _hasSnakeVenom; set => _hasSnakeVenom = value; }
    public bool HasClimb { get => _hasClimb; set => _hasClimb = value; }
    public bool HasExtraLife { get => _hasExtraLife; set => _hasExtraLife = value; }
    public bool HasHealthBoost { get => _hasHealthBoost; set => _hasHealthBoost = value; }
    public bool HasStrongDart { get => _hasStrongDart; set => _hasStrongDart = value; }
    public bool HasGrenade { get => _hasGrenade; set => _hasGrenade = value; }
    public int Coins { get => _coins; set => _coins = value; }
    public int Lives { get => _lives; set => _lives = value; }
    public int KoalasRescued { get => _koalasRescued; private set => _koalasRescued = value; }
    Fader _fader;
    bool _loading;
    RectTransform _startButton;
    [SerializeField] float _fadeTime = 0.5f;



    private void Awake()

    {

        if (_instance != null && _instance != this)
        {
            SoundManager incoming = GetComponent<SoundManager>();
            if (incoming != null)
                SoundManager.Instance?.Adopt(incoming);
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = MobilePerf.TargetFps;
        QualitySettings.vSyncCount = 0;
        EnsureTransitionFader();

    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        LimitWaterfallAudio();
        ApplyPurchasesToScene();
        HideRescuedKoalas();
        BindStartButton();
        if (_fader != null)
            _fader.FadeOut(_fadeTime);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _won = false;
        Time.timeScale = 1f;
        LimitWaterfallAudio();
        ApplyPurchasesToScene();
        HideRescuedKoalas();
        if (UiManager.Instance != null)
            UiManager.Instance.UpdateKoalaScoreUi(KoalasRescued);
        BindStartButton();
    }

    void BindStartButton()
    {
        _startButton = null;
        if (SceneManager.GetActiveScene().buildIndex != 0)
            return;
        GameObject start = GameObject.Find("Start");
        if (start != null)
            _startButton = start.transform as RectTransform;
    }

    void Update()
    {
        if (_loading || _startButton == null)
            return;
        if (!PrimaryPressedThisFrame())
            return;
        Vector2 screen = PrimaryScreenPoint();
        if (RectTransformUtility.RectangleContainsScreenPoint(_startButton, screen, null))
            StartGame();
    }

    static bool PrimaryPressedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    static Vector2 PrimaryScreenPoint()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
        return Vector2.zero;
    }

    public void SaveCoins(int coins)
    {
        Coins = coins;
        _coinsSaved = true;
    }

    public void SaveLives(int lives)
    {
        Lives = Mathf.Max(0, lives);
        _livesSaved = true;
    }

    public void ApplyPurchasesToScene()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.HasClimb = HasClimb;
            player.HasKey = HasKeyKoala;
            player.HasVenum = HasSnakeVenom;

            if (_coinsSaved)
                player._coinValue = Coins;
            else
                SaveCoins(player._coinValue);

            if (_livesSaved)
                player._lives = Lives;
            else
                SaveLives(player._lives);

            ApplyLateUpgrades(player);

            if (UiManager.Instance != null)
                UiManager.Instance.ShowGemCount(player._coinValue);
        }

        Shop shop = FindFirstObjectByType<Shop>(FindObjectsInactive.Include);
        if (shop != null)
            shop.ApplyOwnedItems();
        else if (HasClimb)
            EnableLadders();

        if (UiManager.Instance != null)
            UiManager.Instance.RefreshLives(Lives);
    }

    public void GrantEarlyPurchases()
    {
        HasClimb = true;
        HasKeyKoala = true;
        HasStrongDart = true;
    }

    public void ApplyLateUpgrades(Player player)
    {
        if (player == null)
            return;

        if (HasHealthBoost)
        {
            player._maxHealth += HealthBoostAmount;
            player._health = player._maxHealth;
        }

        if (UiManager.Instance != null)
            UiManager.Instance.UpdateHealthUI(player._health, player._maxHealth);
    }

    public void EnableLadders()
    {
        GameObject ladder = GameObject.Find("Ladder");
        if (ladder != null)
            ladder.SetActive(true);
        GameObject ladder2 = GameObject.Find("Ladder (2)");
        if (ladder2 != null)
            ladder2.SetActive(true);
    }

    void LimitWaterfallAudio()
    {
        AudioSource keep = null;
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || !IsWaterfallSpeaker(source))
                continue;

            if (keep == null)
            {
                keep = source;
                continue;
            }

            source.mute = true;
            source.playOnAwake = false;
            source.Stop();
        }

        if (keep == null)
            return;

        keep.mute = false;
        keep.playOnAwake = true;
        if (!keep.isPlaying)
            keep.Play();
    }

    static bool IsWaterfallSpeaker(AudioSource source)
    {
        Transform t = source.transform;
        while (t != null)
        {
            if (t.name.IndexOf("waterfall", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }
        return false;
    }

    public void RescueKoala(Koala koala)
    {
        if (_won || koala == null)
            return;
        if (!_rescuedKoalaIds.Add(KoalaId(koala)))
            return;

        KoalasRescued = _rescuedKoalaIds.Count;
        if (UiManager.Instance != null)
            UiManager.Instance.UpdateKoalaScoreUi(KoalasRescued);

        Win();
    }

    public bool WasKoalaRescued(Koala koala)
    {
        return koala != null && _rescuedKoalaIds.Contains(KoalaId(koala));
    }

    static string KoalaId(Koala koala)
    {
        return SceneManager.GetActiveScene().name + ":" + koala.name;
    }

    void HideRescuedKoalas()
    {
        Koala[] koalas = FindObjectsByType<Koala>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < koalas.Length; i++)
        {
            if (koalas[i] != null && WasKoalaRescued(koalas[i]))
                koalas[i].gameObject.SetActive(false);
        }
    }

    void ResetRun()
    {
        HasKeyKoala = false;
        HasSnakeVenom = false;
        HasClimb = false;
        HasExtraLife = false;
        HasHealthBoost = false;
        HasStrongDart = false;
        HasGrenade = false;
        Coins = 0;
        _coinsSaved = false;
        SaveLives(3);
        KoalasRescued = 0;
        _rescuedKoalaIds.Clear();
        _won = false;
    }

    void EnsureTransitionFader()
    {
        if (_fader != null)
            return;

        GameObject go = new GameObject("FadeCanvas");
        DontDestroyOnLoad(go);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        go.AddComponent<GraphicRaycaster>();
        _fader = go.AddComponent<Fader>();
        _fader.UseForTransitions();
    }

    IEnumerator FadeAndLoad(int sceneIndex)
    {
        if (_loading)
            yield break;

        _loading = true;
        Time.timeScale = 1f;
        if (_fader != null)
        {
            _fader.FadeIn(_fadeTime);
            yield return new WaitForSecondsRealtime(_fadeTime);
        }

        if (ShouldShowInterstitial(sceneIndex))
            yield return ShowInterstitialThenContinue();

        SceneManager.LoadScene(sceneIndex);
        yield return null;
        if (_fader != null)
            _fader.FadeOut(_fadeTime);
        _loading = false;
    }

    static bool ShouldShowInterstitial(int sceneIndex)
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        return current > 0 && sceneIndex > 0 && sceneIndex != current;
    }

    static IEnumerator ShowInterstitialThenContinue()
    {
        bool adDone = AdsManager.Instance == null;
        if (!adDone)
            AdsManager.Instance.ShowInterstitial(() => adDone = true);

        float waited = 0f;
        while (!adDone && waited < 90f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void StartGame()
    {
        ResetRun();
        StartCoroutine(FadeAndLoad(1));
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void RestartGame()
    {
        StartCoroutine(FadeAndLoad(SceneManager.GetActiveScene().buildIndex));
    }

    public void LoadMenu()
    {
        ResetRun();
        StartCoroutine(FadeAndLoad(0));
    }

    public void Win()
    {
        if (_won || !HasKeyKoala)
            return;

        Koala[] koalas = FindObjectsByType<Koala>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < koalas.Length; i++)
        {
            if (koalas[i] != null && !WasKoalaRescued(koalas[i]))
                return;
        }

        _won = true;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            StartCoroutine(FadeAndLoad(next));
        else
            Time.timeScale = 0f;
    }

    public void Gameover()
    {
        if (Lives <= 0)
            LoadMenu();
        else
            RestartGame();
    }
    
}

