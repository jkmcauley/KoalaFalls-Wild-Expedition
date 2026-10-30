using System;
using UnityEngine;
using Unity.Services.LevelPlay;

/// <summary>
/// Rewarded and interstitial ads through Unity LevelPlay. Paste the app key
/// and ad unit ids on Assets/Resources/AdSettings.
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [SerializeField] string _androidAppKey;
    [SerializeField] string _iosAppKey;
    [SerializeField] string _androidRewardedAdUnitId;
    [SerializeField] string _iosRewardedAdUnitId;
    [SerializeField] string _androidInterstitialAdUnitId;
    [SerializeField] string _iosInterstitialAdUnitId;

    LevelPlayRewardedAd _rewardedAd;
    LevelPlayInterstitialAd _interstitialAd;
    Action _onReward;
    Action _onInterstitialClosed;
    bool _rewardGranted;
    bool _interstitialFinished;
    bool _initialized;

    public bool IsInitialized => _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
            return;

        var go = new GameObject("AdsManager");
        go.AddComponent<AdsManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplySettings(Resources.Load<AdSettings>("AdSettings"));
    }

    void Start()
    {
        string appKey = AppKey;
        if (string.IsNullOrEmpty(appKey))
        {
            Debug.Log("AdsManager: fill in the LevelPlay app key on Assets/Resources/AdSettings.");
            return;
        }

        if (string.IsNullOrEmpty(RewardedAdUnitId) && string.IsNullOrEmpty(InterstitialAdUnitId))
        {
            Debug.Log("AdsManager: fill in a rewarded or interstitial ad unit id on Assets/Resources/AdSettings.");
            return;
        }

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        UnhookRewarded();
        UnhookInterstitial();
    }

    public void ShowRewarded(Action onReward)
    {
#if UNITY_EDITOR
        onReward?.Invoke();
#else
        if (_rewardedAd == null || !_rewardedAd.IsAdReady())
        {
            Debug.Log("Rewarded ad is not ready yet.");
            _rewardedAd?.LoadAd();
            return;
        }

        _onReward = onReward;
        _rewardGranted = false;
        _rewardedAd.ShowAd();
#endif
    }

    public void ShowInterstitial(Action onClosed)
    {
#if UNITY_EDITOR
        onClosed?.Invoke();
#else
        if (_interstitialAd == null || !_interstitialAd.IsAdReady())
        {
            Debug.Log("Interstitial ad is not ready yet.");
            _interstitialAd?.LoadAd();
            onClosed?.Invoke();
            return;
        }

        _onInterstitialClosed = onClosed;
        _interstitialFinished = false;
        _interstitialAd.ShowAd();
#endif
    }

    void ApplySettings(AdSettings settings)
    {
        if (settings == null)
            return;

        if (string.IsNullOrEmpty(_androidAppKey))
            _androidAppKey = settings.androidAppKey;
        if (string.IsNullOrEmpty(_iosAppKey))
            _iosAppKey = settings.iosAppKey;
        if (string.IsNullOrEmpty(_androidRewardedAdUnitId))
            _androidRewardedAdUnitId = settings.androidRewardedAdUnitId;
        if (string.IsNullOrEmpty(_iosRewardedAdUnitId))
            _iosRewardedAdUnitId = settings.iosRewardedAdUnitId;
        if (string.IsNullOrEmpty(_androidInterstitialAdUnitId))
            _androidInterstitialAdUnitId = settings.androidInterstitialAdUnitId;
        if (string.IsNullOrEmpty(_iosInterstitialAdUnitId))
            _iosInterstitialAdUnitId = settings.iosInterstitialAdUnitId;
    }

    void OnInitSuccess(LevelPlayConfiguration config)
    {
        _initialized = true;

        if (!string.IsNullOrEmpty(RewardedAdUnitId))
        {
            _rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
            _rewardedAd.OnAdLoaded += OnAdLoaded;
            _rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
            _rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
            _rewardedAd.OnAdRewarded += OnAdRewarded;
            _rewardedAd.OnAdClosed += OnAdClosed;
            _rewardedAd.LoadAd();
        }

        if (!string.IsNullOrEmpty(InterstitialAdUnitId))
        {
            _interstitialAd = new LevelPlayInterstitialAd(InterstitialAdUnitId);
            _interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
            _interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
            _interstitialAd.OnAdClosed += OnInterstitialClosed;
            _interstitialAd.LoadAd();
        }
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        _initialized = false;
        Debug.LogWarning($"LevelPlay init failed: {error}");
    }

    void OnAdLoaded(LevelPlayAdInfo info) { }

    void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded ad failed to load: {error}");
        CancelInvoke(nameof(LoadRewarded));
        Invoke(nameof(LoadRewarded), 5f);
    }

    void OnAdDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        _onReward = null;
        Debug.LogWarning($"Rewarded ad failed to show: {error}");
        LoadRewarded();
    }

    void OnAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        if (_rewardGranted)
            return;

        _rewardGranted = true;
        Action callback = _onReward;
        _onReward = null;
        callback?.Invoke();
    }

    void OnAdClosed(LevelPlayAdInfo info)
    {
        _onReward = null;
        LoadRewarded();
    }

    void LoadRewarded()
    {
        _rewardedAd?.LoadAd();
    }

    void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Interstitial ad failed to load: {error}");
        CancelInvoke(nameof(LoadInterstitial));
        Invoke(nameof(LoadInterstitial), 5f);
    }

    void OnInterstitialDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogWarning($"Interstitial ad failed to show: {error}");
        FinishInterstitial();
        LoadInterstitial();
    }

    void OnInterstitialClosed(LevelPlayAdInfo info)
    {
        FinishInterstitial();
        LoadInterstitial();
    }

    void FinishInterstitial()
    {
        if (_interstitialFinished)
            return;

        _interstitialFinished = true;
        Action callback = _onInterstitialClosed;
        _onInterstitialClosed = null;
        callback?.Invoke();
    }

    void LoadInterstitial()
    {
        _interstitialAd?.LoadAd();
    }

    void UnhookRewarded()
    {
        if (_rewardedAd == null)
            return;

        _rewardedAd.OnAdLoaded -= OnAdLoaded;
        _rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
        _rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        _rewardedAd.OnAdRewarded -= OnAdRewarded;
        _rewardedAd.OnAdClosed -= OnAdClosed;
        _rewardedAd = null;
    }

    void UnhookInterstitial()
    {
        if (_interstitialAd == null)
            return;

        _interstitialAd.OnAdLoadFailed -= OnInterstitialLoadFailed;
        _interstitialAd.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
        _interstitialAd.OnAdClosed -= OnInterstitialClosed;
        _interstitialAd = null;
    }

    string AppKey
    {
        get
        {
#if UNITY_ANDROID
            return _androidAppKey;
#elif UNITY_IOS
            return _iosAppKey;
#else
            return _androidAppKey;
#endif
        }
    }

    string RewardedAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return _androidRewardedAdUnitId;
#elif UNITY_IOS
            return _iosRewardedAdUnitId;
#else
            return _androidRewardedAdUnitId;
#endif
        }
    }

    string InterstitialAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return _androidInterstitialAdUnitId;
#elif UNITY_IOS
            return _iosInterstitialAdUnitId;
#else
            return _androidInterstitialAdUnitId;
#endif
        }
    }
}
