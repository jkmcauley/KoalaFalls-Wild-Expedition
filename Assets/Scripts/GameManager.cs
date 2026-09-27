using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    public int Coins { get => _coins; set => _coins = value; }
    public int Lives { get => _lives; set => _lives = value; }
    public int KoalasRescued { get => _koalasRescued; private set => _koalasRescued = value; }
    Fader _fader;
    bool _loading;
    [SerializeField] float _fadeTime = 0.5f;



    private void Awake()

    {

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
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
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null)
                continue;
            if (t.name == "Ladder" || t.name == "Ladder (2)")
                t.gameObject.SetActive(true);
        }
    }

    void LimitWaterfallAudio()
    {
        AudioSource keep = null;
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        SceneManager.LoadScene(sceneIndex);
        yield return null;
        if (_fader != null)
            _fader.FadeOut(_fadeTime);
        _loading = false;
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

