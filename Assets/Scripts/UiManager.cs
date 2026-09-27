using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class UiManager : MonoBehaviour

{


    private static UiManager _instance;

    public static UiManager Instance

    {

        get

        {

            if (_instance == null)

            {

                _instance = FindFirstObjectByType<UiManager>();

            }

            return _instance;

        }

    }

    [SerializeField] private TMPro.TextMeshProUGUI _gemCountText;
    [SerializeField] private TMPro.TextMeshProUGUI _hudGemCountText;
    [SerializeField] private TMPro.TextMeshProUGUI _koalaScoreText;
    [SerializeField] private Player _player;
    [SerializeField] private Image _healthBar;
    [SerializeField] private Sprite[] _healthImages;
    [SerializeField] private Image _koalaImage;
    [SerializeField] private Image[] _koalaImages;
    readonly List<Image> _lighting = new List<Image>();
    [SerializeField] private Image[] _livesImage;
    [SerializeField] private float _fadeSeconds = .5f;
    Sprite _lifeSprite;

    private void Awake()

    {

        if (_instance != null && _instance != this)

        {

            Destroy(gameObject);

            return;

        }

        _instance = this;

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    private void Start()
    {
        BindHud();
    }

    void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _lighting.Clear();
        BindHud();
    }

    void BindHud()
    {
        _player = FindFirstObjectByType<Player>();
        BindHealthBar();
        BindGemTexts();
        EnsureKoalaScoreText();
        BindLivesImages();
        ApplyLivesDisplay(GameManager.Instance != null ? GameManager.Instance.Lives : (_player != null ? _player._lives : 3));
        if (GameManager.Instance != null)
            UpdateKoalaScoreUi(GameManager.Instance.KoalasRescued);

        if (_player == null)
            return;

        ShowGemCount(_player._coinValue);
        UpdateHealthUI(_player._health, _player._maxHealth);
    }

    void BindLivesImages()
    {
        CacheLifeSprite();
        if (HasLiveImages(_livesImage))
            return;

        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<(int index, Image image)> found = new List<(int, Image)>();
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || !image.name.StartsWith("livesImg"))
                continue;

            int index = LifeIndexFromName(image.name);
            if (index < 0)
                continue;
            found.Add((index, image));
        }

        found.Sort((a, b) => a.index.CompareTo(b.index));
        if (found.Count == 0)
        {
            CreateLivesImages();
            return;
        }

        _livesImage = new Image[found.Count];
        for (int i = 0; i < found.Count; i++)
            _livesImage[i] = found[i].image;
        CacheLifeSprite();
    }

    void CacheLifeSprite()
    {
        if (_lifeSprite != null || _livesImage == null)
            return;
        for (int i = 0; i < _livesImage.Length; i++)
        {
            if (_livesImage[i] != null && _livesImage[i].sprite != null)
            {
                _lifeSprite = _livesImage[i].sprite;
                return;
            }
        }
    }

    void CreateLivesImages()
    {
        if (_lifeSprite == null)
            return;

        Canvas canvas = FindGameplayCanvas();
        if (canvas == null)
            return;

        _livesImage = new Image[3];
        for (int i = 0; i < _livesImage.Length; i++)
        {
            GameObject go = new GameObject("livesImg" + i);
            go.layer = 5;
            go.transform.SetParent(canvas.transform, false);
            Image image = go.AddComponent<Image>();
            image.sprite = _lifeSprite;
            image.raycastTarget = false;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(75f, 75f);
            rect.anchoredPosition = new Vector2(-40f - i * 85f, -20f);
            _livesImage[i] = image;
        }
    }

    void EnsureKoalaScoreText()
    {
        if (_koalaScoreText != null)
            return;

        Canvas canvas = FindGameplayCanvas();
        if (canvas == null)
            return;

        GameObject go = new GameObject("KoalaScoreText");
        go.layer = 5;
        go.transform.SetParent(canvas.transform, false);
        TMPro.TextMeshProUGUI text = go.AddComponent<TMPro.TextMeshProUGUI>();
        TMPro.TextMeshProUGUI template = _hudGemCountText != null ? _hudGemCountText : _gemCountText;
        if (template != null)
        {
            text.font = template.font;
            text.color = template.color;
            text.fontSharedMaterial = template.fontSharedMaterial;
        }
        text.fontSize = 72f;
        text.alignment = TMPro.TextAlignmentOptions.Right;
        text.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(200f, 80f);
        rect.anchoredPosition = new Vector2(-40f, -120f);
        _koalaScoreText = text;
    }

    static Canvas FindGameplayCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.name == "FadeCanvas")
                continue;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;
            if (fallback == null)
                fallback = canvas;
        }
        return fallback;
    }

    static bool HasLiveImages(Image[] images)
    {
        if (images == null || images.Length == 0)
            return false;
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                return false;
        }
        return true;
    }

    static int LifeIndexFromName(string name)
    {
        int value = -1;
        for (int i = 0; i < name.Length; i++)
        {
            if (!char.IsDigit(name[i]))
                continue;
            if (value < 0)
                value = 0;
            value = value * 10 + (name[i] - '0');
        }
        return value;
    }

    void ApplyLivesDisplay(int lives)
    {
        if (_livesImage == null)
            return;

        for (int i = 0; i < _livesImage.Length; i++)
        {
            Image image = _livesImage[i];
            if (image == null)
                continue;
            Color color = image.color;
            color.a = i < lives ? 1f : 0f;
            image.color = color;
        }
    }

    void BindHealthBar()
    {
        if (_healthBar != null)
            return;

        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == "healthbarimage")
            {
                _healthBar = images[i];
                return;
            }
        }
    }

    void BindGemTexts()
    {
        TMPro.TextMeshProUGUI[] texts =
            FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TMPro.TextMeshProUGUI text = texts[i];
            if (text == null)
                continue;
            if (_hudGemCountText == null && text.name == "coinValue.text")
                _hudGemCountText = text;
            else if (_gemCountText == null && text.name == "CoinAmount.text")
                _gemCountText = text;
            else if (_koalaScoreText == null && (text.name == "KoalaScoreText" || text.name == "KoalaScoreText.text"))
                _koalaScoreText = text;
        }
    }



    public void ShowGemCount(int gemCount)
    {
        string text = $"{gemCount} Coins";
        if (_gemCountText != null)
        {
            _gemCountText.text = text;
        }

        if (_hudGemCountText != null)
        {
            _hudGemCountText.text = text;
        }
    }

    public void UpdateHealthUI(float health, float maxHealth)
    {
        if (_healthBar == null || _healthImages == null || _healthImages.Length == 0)
            return;

        if (maxHealth <= 0f)
            maxHealth = 100f;

        int last = _healthImages.Length - 1;
        int index;
        if (health <= 0f || last <= 0)
            index = 0;
        else
            index = Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(health / maxHealth) * last), 1, last);

        _healthBar.sprite = _healthImages[index];
        _healthBar.enabled = true;
    }

    public void UpdateKoalaScoreUi(int koalasRescued)
    {
        if (_koalaScoreText == null)
            BindGemTexts();
        if (_koalaScoreText == null)
            return;

        _koalaScoreText.text = koalasRescued.ToString();
    }

    public void RefreshLives(int lives)
    {
        BindLivesImages();
        ApplyLivesDisplay(lives);
    }

    public void UpdateLives(int lives)
    {
        UpdateLives(lives, null);
    }

    public void UpdateLives(int lives, Action onFaded)
    {
        BindLivesImages();
        if (_livesImage == null || lives < 0 || lives >= _livesImage.Length || _livesImage[lives] == null)
        {
            onFaded?.Invoke();
            return;
        }

        StartCoroutine(FadeLife(_livesImage[lives], onFaded));
    }

    IEnumerator FadeLife(Image image, Action onFaded)
    {
        Color color = image.color;
        float startAlpha = color.a;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _fadeSeconds);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            image.color = color;
            yield return null;
        }

        color.a = 0f;
        image.color = color;
        onFaded?.Invoke();
    }

}














