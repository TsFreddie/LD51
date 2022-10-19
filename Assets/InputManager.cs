using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum InputType
{
    Keyboard,
    Gamepad,
    Touch,
}

public class InputManager : MonoBehaviour
{
    private struct MenuInfo
    {
        public CanvasGroup CanvasGroup;
        public GameObject FirstSelected;
        public GameObject CurrentSelected;
    }

    public static InputManager Instance { get; private set; }

    public PlayerInput PlayerInput;
    public EventSystem EventSystem;

    private GameObject _initFirstSelected;
    public CanvasGroup DefaultCanvasGroup;
    public BaseInputModule InputModule;
    public Action<CanvasGroup> OnBack;

    private CanvasGroup _currentCanvasGroup;

    private Stack<MenuInfo> _menuStack = new Stack<MenuInfo>();

    public InputType CurrentInputType { get; private set; }

    protected void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PlayerInput.onControlsChanged += OnControlsChanged;
        PlayerInput.actions["UI/Click"].performed += OnMouseClickPerformed;
        PlayerInput.actions["UI/Navigate"].performed += OnNavigatePerformed;
        PlayerInput.actions["UI/Cancel"].performed += OnCancelPerformed;

        _currentCanvasGroup = DefaultCanvasGroup;
        _initFirstSelected = EventSystem.firstSelectedGameObject;
    }

    private void OnCancelPerformed(InputAction.CallbackContext obj)
    {
        Back();
    }

    public void Back()
    {
        if (_menuStack.Count <= 0) return;

        var last = _menuStack.Pop();

        EventSystem.firstSelectedGameObject = last.FirstSelected;
        EventSystem.SetSelectedGameObject(last.CurrentSelected);

        last.CanvasGroup.interactable = true;
        _currentCanvasGroup.interactable = false;

        OnBack?.Invoke(_currentCanvasGroup);
        _currentCanvasGroup = last.CanvasGroup;
    }

    public void OpenMenu(CanvasGroup group, GameObject firstSelected)
    {
        if (_currentCanvasGroup != null)
            _currentCanvasGroup.interactable = false;
        group.interactable = true;

        if (_currentCanvasGroup != null)
            _menuStack.Push(new MenuInfo()
            {
                CanvasGroup = _currentCanvasGroup,
                FirstSelected = EventSystem.firstSelectedGameObject,
                CurrentSelected = EventSystem.currentSelectedGameObject,
            });

        _currentCanvasGroup = group;

        EventSystem.firstSelectedGameObject = firstSelected;
        EventSystem.SetSelectedGameObject(firstSelected);
    }

    public void NullifyMenu()
    {
        if (_currentCanvasGroup != null)
            _currentCanvasGroup.interactable = false;

        while (_menuStack.Count > 0)
        {
            var menu = _menuStack.Pop();
            if (menu.CanvasGroup != null)
                menu.CanvasGroup.interactable = false;
        }

        _currentCanvasGroup = null;
        EventSystem.firstSelectedGameObject = null;
        EventSystem.SetSelectedGameObject(null);
    }

    public void SwitchToTitleMenu()
    {
        OpenMenu(DefaultCanvasGroup, _initFirstSelected);
    }

    private void OnNavigatePerformed(InputAction.CallbackContext obj)
    {
        if (EventSystem.currentSelectedGameObject == null)
        {
            EventSystem.SetSelectedGameObject(EventSystem.firstSelectedGameObject);
        }
    }

    private void OnMouseClickPerformed(InputAction.CallbackContext obj)
    {
        // Force keyboard when mouse clicked
        if (PlayerInput.currentControlScheme != "Keyboard")
        {
            PlayerInput.SwitchCurrentControlScheme("Keyboard");
        }
    }

    private void OnControlsChanged(PlayerInput playerInput)
    {
        switch (playerInput.currentControlScheme)
        {
        case "Keyboard":
            CurrentInputType = InputType.Keyboard;
            break;
        case "Gamepad":
            CurrentInputType = InputType.Gamepad;
            break;
        case "Touch":
            CurrentInputType = InputType.Touch;
            break;
        }
    }

    public void SetGameMode()
    {
        InputModule.enabled = false;
    }

    public void SetMenuMode()
    {
        InputModule.enabled = true;
    }
}
