using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using static PlayersControlls;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObjects/InputReader", order = 1)]
public class InputReader : ScriptableObject, IPlayerInput, IGameplayActions
{
    // Dispositivo que este jogador controla. Em co-op, cada jogador tem seu próprio asset de InputReader.
    public enum DeviceSlot { Any, KeyboardMouse, Gamepad }

    [SerializeField] private DeviceSlot m_deviceSlot = DeviceSlot.Any;
    [Tooltip("Qual gamepad conectado usar (0 = primeiro). Só vale para DeviceSlot.Gamepad.")]
    [SerializeField, Min(0)] private int m_gamepadIndex;

    [Tooltip("Trava o mouse no centro da tela ao ativar o gameplay (só teclado e mouse)")]
    [SerializeField] private bool m_lockCursorOnEnable = true;

    public event Action JumpStarted = delegate { };
    public event Action JumpCanceled = delegate { };
    public event Action Interact = delegate { };
    public event Action<bool> Aim = delegate { };
    public event Action<bool> FireTongue = delegate { };
    public event Action<bool> CursorLockChanged = delegate { };
    public event Action<string> ControlSchemeChanged = delegate { };

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool IsAiming { get; private set; }
    public bool IsFiringTongue { get; private set; }
    public string CurrentControlScheme { get; private set; }
    public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

    private PlayersControlls m_controls;
    private InputDevice m_lastDevice;
    private bool m_isJumpHeld;
    private bool m_isListeningDevices;

    public void EnableGameplay()
    {
        if (m_controls == null)
        {
            m_controls = new PlayersControlls();
            m_controls.Gameplay.SetCallbacks(this);
        }

        if (!m_isListeningDevices)
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            m_isListeningDevices = true;
        }

        PairDevices();
        m_controls.Gameplay.Enable();

        if (m_lockCursorOnEnable && UsesKeyboardMouse)
            SetCursorLocked(true);
    }

    // Gamepad nao usa cursor; Any pode estar no teclado
    private bool UsesKeyboardMouse => m_deviceSlot != DeviceSlot.Gamepad;

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        CursorLockChanged.Invoke(locked);
    }

    public void DisableGameplay()
    {
        StopListeningDevices();
        m_controls?.Gameplay.Disable();
        ResetState();
    }

    // Restringe as actions aos dispositivos deste jogador, para que um jogador não controle o outro
    private void PairDevices()
    {
        switch (m_deviceSlot)
        {
            case DeviceSlot.KeyboardMouse:
                m_controls.bindingMask = InputBinding.MaskByGroup("KeyboardMouse");
                m_controls.devices = new ReadOnlyArray<InputDevice>(ConnectedDevices(Keyboard.current, Mouse.current));
                SetControlScheme("KeyboardMouse");
                break;

            case DeviceSlot.Gamepad:
                Gamepad gamepad = m_gamepadIndex < Gamepad.all.Count ? Gamepad.all[m_gamepadIndex] : null;
                if (gamepad == null)
                    Debug.LogWarning($"{name}: nenhum gamepad conectado no índice {m_gamepadIndex}.", this);

                m_controls.bindingMask = InputBinding.MaskByGroup("Gamepad");
                m_controls.devices = new ReadOnlyArray<InputDevice>(ConnectedDevices(gamepad));
                SetControlScheme("Gamepad");
                break;

            default:
                m_controls.bindingMask = null;
                m_controls.devices = null;
                break;
        }
    }

    private static InputDevice[] ConnectedDevices(params InputDevice[] devices)
    {
        return Array.FindAll(devices, device => device != null);
    }

    // Refaz o pareamento quando um controle é conectado/desconectado
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (m_controls == null || m_deviceSlot == DeviceSlot.Any) return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Removed:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Disconnected:
                PairDevices();
                break;
        }
    }

    private void StopListeningDevices()
    {
        if (!m_isListeningDevices) return;

        InputSystem.onDeviceChange -= OnDeviceChange;
        m_isListeningDevices = false;
    }

    // Chamado pela Unity ao descarregar o asset (ex.: sair do Play Mode com Domain Reload desligado)
    private void OnDisable()
    {
        StopListeningDevices();
        if (m_controls == null) return;

        m_controls.Gameplay.RemoveCallbacks(this);
        m_controls.Dispose();
        m_controls = null;
        ResetState();
    }

    private void ResetState()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;
        IsAiming = false;
        IsFiringTongue = false;
        m_isJumpHeld = false;
        m_lastDevice = null;
    }

    private void TrackDevice(InputAction.CallbackContext context)
    {
        InputDevice device = context.control?.device;
        if (device == null || device == m_lastDevice) return;
        m_lastDevice = device;

        InputControlScheme? scheme = InputControlScheme.FindControlSchemeForDevice(device, m_controls.controlSchemes);
        if (scheme != null) SetControlScheme(scheme.Value.name);
    }

    private void SetControlScheme(string scheme)
    {
        if (scheme == CurrentControlScheme) return;

        CurrentControlScheme = scheme;
        ControlSchemeChanged.Invoke(CurrentControlScheme);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        TrackDevice(context);
        Move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        TrackDevice(context);
        Look = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        TrackDevice(context);
        if (context.performed && !m_isJumpHeld)
        {
            m_isJumpHeld = true;
            JumpStarted.Invoke();
        }
        else if (context.canceled && m_isJumpHeld)
        {
            m_isJumpHeld = false;
            JumpCanceled.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        TrackDevice(context);
        if (context.performed) Interact.Invoke();
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        TrackDevice(context);
        if (context.performed && !IsAiming) Aim.Invoke(IsAiming = true);
        else if (context.canceled && IsAiming) Aim.Invoke(IsAiming = false);
    }

    // Tecla Esc no teclado: alterna entre mouse travado e livre
    public void OnToggleCursorLock(InputAction.CallbackContext context)
    {
        TrackDevice(context);
        if (context.performed && UsesKeyboardMouse)
            SetCursorLocked(!IsCursorLocked);
    }

    public void OnFireTongue(InputAction.CallbackContext context)
    {
        TrackDevice(context);
        if (context.performed && !IsFiringTongue) FireTongue.Invoke(IsFiringTongue = true);
        else if (context.canceled && IsFiringTongue) FireTongue.Invoke(IsFiringTongue = false);
    }
}
