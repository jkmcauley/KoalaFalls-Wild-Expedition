using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TouchControlsFix : MonoBehaviour
{
    public static TouchControlsFix Instance { get; private set; }
    public static Vector2 Move { get; private set; }
    public static bool JumpPressed { get; private set; }
    public static bool JumpHeld { get; private set; }
    public static bool AttackPressed { get; private set; }
    public static bool AttackHeld { get; private set; }

    InputSystem_Actions _uiActions;
    RectTransform _stickArea;
    RectTransform _knob;
    RectTransform _jumpButton;
    RectTransform _attackButton;
    float _stickRange = 50f;
    int _stickPointer = int.MinValue;
    bool _jumpWasHeld;
    bool _attackWasHeld;
    int _boundFrame = -1;
    readonly List<Pointer> _pointers = new List<Pointer>(8);

    struct Pointer
    {
        public int id;
        public Vector2 screen;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null)
        {
            Instance.FixAll();
            return;
        }

        GameObject go = new GameObject("TouchControlsFix");
        DontDestroyOnLoad(go);
        go.AddComponent<TouchControlsFix>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = MobilePerf.TargetFps;
        QualitySettings.vSyncCount = 0;
        SceneManager.sceneLoaded += OnSceneLoaded;
        FixAll();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
            Move = Vector2.zero;
            JumpPressed = JumpHeld = AttackPressed = AttackHeld = false;
        }
        _uiActions?.Dispose();
        _uiActions = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _stickPointer = int.MinValue;
        Move = Vector2.zero;
        FixAll();
        if (isActiveAndEnabled)
            StartCoroutine(FixNextFrame());
    }

    IEnumerator FixNextFrame()
    {
        yield return null;
        FixAll();
    }

    public void FixAll()
    {
        BindUiModule();
        BindControls();
    }

    void Update()
    {
        CollectPointers();
        SampleStick();
        SampleButtons();
    }

    void BindControls()
    {
        _stickArea = null;
        _knob = null;
        _jumpButton = null;
        _attackButton = null;
        _stickRange = 50f;

        OnScreenStick[] sticks = FindObjectsByType<OnScreenStick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sticks.Length; i++)
        {
            OnScreenStick stick = sticks[i];
            if (stick == null)
                continue;

            _knob = stick.transform as RectTransform;
            _stickRange = Mathf.Max(16f, stick.movementRange);
            Transform parent = stick.transform.parent;
            _stickArea = parent != null ? parent as RectTransform : _knob;
            if (parent != null && parent.name == "Joystick background")
            {
                Image background = parent.GetComponent<Image>();
                if (background != null)
                    background.raycastTarget = false;
            }

            Image knob = stick.GetComponent<Image>();
            if (knob != null)
            {
                knob.raycastTarget = true;
                knob.raycastPadding = new Vector4(-100f, -100f, -100f, -100f);
            }

            stick.enabled = false;
        }

        OnScreenButton[] buttons = FindObjectsByType<OnScreenButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            OnScreenButton button = buttons[i];
            if (button == null)
                continue;

            RectTransform rect = button.transform as RectTransform;
            string path = button.controlPath ?? string.Empty;
            if (path.IndexOf("buttonSouth", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _jumpButton = rect;
            else if (path.IndexOf("buttonWest", System.StringComparison.OrdinalIgnoreCase) >= 0)
                _attackButton = rect;

            button.enabled = false;
        }
    }

    void CollectPointers()
    {
        _pointers.Clear();
        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                if (!touches[i].press.isPressed)
                    continue;
                _pointers.Add(new Pointer { id = i, screen = touches[i].position.ReadValue() });
            }
        }

        if (_pointers.Count == 0 && Mouse.current != null && Mouse.current.leftButton.isPressed)
            _pointers.Add(new Pointer { id = -1, screen = Mouse.current.position.ReadValue() });
    }

    void SampleStick()
    {
        if (_stickArea == null)
        {
            Move = Vector2.zero;
            _stickPointer = int.MinValue;
            return;
        }

        Camera cam = UiCamera(_stickArea);
        Pointer pointer = default;
        bool found = false;

        if (_stickPointer != int.MinValue)
        {
            for (int i = 0; i < _pointers.Count; i++)
            {
                if (_pointers[i].id != _stickPointer)
                    continue;
                pointer = _pointers[i];
                found = true;
                break;
            }
        }

        if (!found)
        {
            _stickPointer = int.MinValue;
            for (int i = 0; i < _pointers.Count; i++)
            {
                if (!Contains(_stickArea, _pointers[i].screen, cam) && (_knob == null || !Contains(_knob, _pointers[i].screen, cam)))
                    continue;
                pointer = _pointers[i];
                _stickPointer = pointer.id;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Move = Vector2.zero;
            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_stickArea, pointer.screen, cam, out Vector2 local);
        if (local.magnitude > _stickRange)
            local = local.normalized * _stickRange;

        Move = local / _stickRange;
        if (_knob != null)
            _knob.anchoredPosition = local;
    }

    void SampleButtons()
    {
        bool jump = IsHeld(_jumpButton);
        bool attack = IsHeld(_attackButton);
        JumpPressed = jump && !_jumpWasHeld;
        AttackPressed = attack && !_attackWasHeld;
        JumpHeld = jump;
        AttackHeld = attack;
        _jumpWasHeld = jump;
        _attackWasHeld = attack;
    }

    bool IsHeld(RectTransform rect)
    {
        if (rect == null)
            return false;

        Camera cam = UiCamera(rect);
        for (int i = 0; i < _pointers.Count; i++)
        {
            if (Contains(rect, _pointers[i].screen, cam))
                return true;
        }
        return false;
    }

    static bool Contains(RectTransform rect, Vector2 screen, Camera cam)
    {
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, cam);
    }

    static Camera UiCamera(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }

    void BindUiModule()
    {
        InputSystemUIInputModule module = FindFirstObjectByType<InputSystemUIInputModule>();
        if (module == null)
            return;

        if (_boundFrame == Time.frameCount && _uiActions != null)
            return;

        _boundFrame = Time.frameCount;
        _uiActions?.Dispose();
        _uiActions = new InputSystem_Actions();
        _uiActions.UI.Enable();
        module.actionsAsset = _uiActions.asset;
        module.point = InputActionReference.Create(_uiActions.UI.Point);
        module.leftClick = InputActionReference.Create(_uiActions.UI.Click);
        module.middleClick = InputActionReference.Create(_uiActions.UI.MiddleClick);
        module.rightClick = InputActionReference.Create(_uiActions.UI.RightClick);
        module.scrollWheel = InputActionReference.Create(_uiActions.UI.ScrollWheel);
        module.move = InputActionReference.Create(_uiActions.UI.Navigate);
        module.submit = InputActionReference.Create(_uiActions.UI.Submit);
        module.cancel = InputActionReference.Create(_uiActions.UI.Cancel);
    }
}
