using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Monitors hardware devices and raises events when a gamepad is connected or disconnected.
/// Used by the menu to enforce co-op hardware requirements (keyboard+mouse AND gamepad).
/// Single Responsibility: hardware detection only.
/// </summary>
public class GamepadDetector : MonoBehaviour
{
    /// <summary>Fires whenever the gamepad-connected state changes. True = at least one gamepad present.</summary>
    public event Action<bool> OnGamepadStateChanged;

    /// <summary>True when at least one gamepad is connected.</summary>
    public bool IsGamepadConnected { get; private set; }

#if UNITY_EDITOR
    [Header("Debug")]
    [Tooltip("Permite usar o menu de jogo mesmo sem um gamepad conectado. Detalhe, terá apenas no editor, não afeta o build final.")]
    [SerializeField] private bool m_ignoreTheDetection = false;
#endif

    private void OnEnable()
    {
        InputSystem.onDeviceChange += HandleDeviceChange;
        Refresh();
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= HandleDeviceChange;
    }

    /// <summary>Forces a re-check of connected gamepads.</summary>
    public void Refresh()
    {
#if UNITY_EDITOR
        if(m_ignoreTheDetection) {
            if (!IsGamepadConnected) {
                IsGamepadConnected = true;
                OnGamepadStateChanged?.Invoke(IsGamepadConnected);
            }
            return;
        }
#endif

        bool _previous = IsGamepadConnected;
        IsGamepadConnected = Gamepad.all.Count > 0;

        if (IsGamepadConnected != _previous)
            OnGamepadStateChanged?.Invoke(IsGamepadConnected);
    }

    private void HandleDeviceChange(InputDevice _device, InputDeviceChange _change)
    {
        if (_device is not Gamepad) return;

        switch (_change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Removed:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Disconnected:
                Refresh();
                break;
        }
    }

#if UNITY_EDITOR
    private void OnValidate() {
        Refresh();
    }
#endif
}

