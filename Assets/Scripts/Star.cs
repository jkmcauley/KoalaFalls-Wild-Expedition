using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Star : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    private bool _ignoreUntilExit;
    bool _shopOpen;
    int _canvasSort;

    void Awake()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<Collider2D>() == null || child.GetComponent<Star>() != null)
                continue;

            child.gameObject.AddComponent<Star>()._panel = _panel;
        }

        if (_panel == null)
            return;

        Button[] buttons = _panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name != "Close")
                continue;
            buttons[i].onClick.AddListener(Close);
            break;
        }
    }

    void Update()
    {
        if (_shopOpen && (_panel == null || !_panel.activeInHierarchy))
        {
            Close();
            return;
        }
        if (_panel == null || !_panel.activeSelf)
            return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GetComponent<Collider2D>() == null)
            return;
        if (other.GetComponent<Player>() == null && other.GetComponentInParent<Player>() == null)
            return;
        if (_panel == null || _ignoreUntilExit || _shopOpen)
            return;

        _panel.SetActive(true);
        if (!_panel.activeInHierarchy)
        {
            Debug.LogWarning("Shop panel is inactive in the hierarchy, so it cannot overlay the game.");
            _panel.SetActive(false);
            return;
        }

        Canvas canvas = _panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            _canvasSort = canvas.sortingOrder;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Mathf.Max(_canvasSort, 1000);
        }

        _shopOpen = true;
        Time.timeScale = 0f;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (GetComponent<Collider2D>() == null)
            return;
        if (other.GetComponent<Player>() == null && other.GetComponentInParent<Player>() == null)
            return;

        _ignoreUntilExit = false;
    }

    public void Close()
    {
        if (_panel != null)
        {
            Canvas canvas = _panel.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = _canvasSort;
            _panel.SetActive(false);
        }

        _shopOpen = false;
        Time.timeScale = 1f;

        Star[] stars = GetComponentsInChildren<Star>();
        for (int i = 0; i < stars.Length; i++)
            stars[i]._ignoreUntilExit = true;
    }

    void OnDestroy()
    {
        if (_panel != null && _panel.activeSelf)
            Time.timeScale = 1f;
    }
}
