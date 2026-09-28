using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class TouchControlsFix : MonoBehaviour
{
    InputSystem_Actions _uiActions;

    void OnEnable()
    {
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
        FixAll();
    }

    void OnDisable()
    {
        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();
    }

    public void FixAll()
    {
        BindUiModule();
        FixJoystickHitAreas();
    }

    void BindUiModule()
    {
        InputSystemUIInputModule module = FindFirstObjectByType<InputSystemUIInputModule>();
        if (module == null)
            return;

        if (_uiActions == null)
            _uiActions = new InputSystem_Actions();

        _uiActions.UI.Enable();
        module.actionsAsset = _uiActions.asset;
        module.point = _uiActions.UI.Point;
        module.leftClick = _uiActions.UI.Click;
        module.middleClick = _uiActions.UI.MiddleClick;
        module.rightClick = _uiActions.UI.RightClick;
        module.scrollWheel = _uiActions.UI.ScrollWheel;
        module.move = _uiActions.UI.Navigate;
        module.submit = _uiActions.UI.Submit;
        module.cancel = _uiActions.UI.Cancel;
        module.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
    }

    static void FixJoystickHitAreas()
    {
        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].gameObject.name == "Joystick background")
                images[i].raycastTarget = false;
        }

        OnScreenStick[] sticks = FindObjectsByType<OnScreenStick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sticks.Length; i++)
        {
            OnScreenStick stick = sticks[i];
            if (stick == null)
                continue;
            stick.useIsolatedInputActions = true;
            RectTransform rt = stick.transform as RectTransform;
            if (rt != null && rt.sizeDelta.x < 250f)
                rt.sizeDelta = new Vector2(280f, 280f);
        }
    }
}
