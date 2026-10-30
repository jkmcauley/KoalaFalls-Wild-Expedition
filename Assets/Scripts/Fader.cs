using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Fader : MonoBehaviour
{
    [SerializeField] private CanvasGroup _cg;
    [SerializeField] private float _startFade = 1f;
    bool _skipStartFade;

    void Awake()
    {
        if (gameObject.name != "FadeCanvas")
        {
            Transform leftover = transform.Find("FadePanel");
            if (leftover != null)
                leftover.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        EnsureFadePanel();
    }

    void Start()
    {
        if (_skipStartFade || !enabled)
            return;
        if (_startFade > 0f)
            FadeOut(_startFade);
    }

    public void UseForTransitions()
    {
        _skipStartFade = true;
        EnsureFadePanel();
        SetAlpha(0f);
    }

    public void SetAlpha(float alpha)
    {
        EnsureFadePanel();
        _cg.alpha = alpha;
        bool block = alpha > 0.01f;
        _cg.blocksRaycasts = block;
        Image fadeImage = _cg.GetComponent<Image>();
        if (fadeImage != null)
            fadeImage.raycastTarget = block;
        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = block;
    }

    public void FadeOut(float seconds)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(seconds, _cg != null ? _cg.alpha : 1f, 0f));
    }

    public void FadeIn(float seconds)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(seconds, _cg != null ? _cg.alpha : 0f, 1f));
    }

    void EnsureFadePanel()
    {
        if (_cg != null)
            return;

        Transform existing = transform.Find("FadePanel");
        GameObject panel = existing != null ? existing.gameObject : new GameObject("FadePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        panel.transform.SetAsLastSibling();
        panel.layer = gameObject.layer;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image image = panel.GetComponent<Image>();
        if (image == null)
            image = panel.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        _cg = panel.GetComponent<CanvasGroup>();
        if (_cg == null)
            _cg = panel.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;
        _cg.blocksRaycasts = _cg.alpha > 0.01f;
        _cg.interactable = false;
        image.raycastTarget = _cg.blocksRaycasts;
    }

    IEnumerator FadeRoutine(float seconds, float from, float to)
    {
        if (_cg == null)
            yield break;

        _cg.blocksRaycasts = true;
        Image fadeImage = _cg.GetComponent<Image>();
        if (fadeImage != null)
            fadeImage.raycastTarget = true;
        _cg.alpha = from;
        if (seconds <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }

        SetAlpha(to);
    }
}
