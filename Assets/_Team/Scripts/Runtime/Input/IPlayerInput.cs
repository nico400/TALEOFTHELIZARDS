using System;
using UnityEngine;

// Intenções do jogador, independente do dispositivo (teclado/mouse, gamepad, IA, replay...)
public interface IPlayerInput
{
    Vector2 Move { get; }
    Vector2 Look { get; }
    bool IsAiming { get; }
    bool IsFiringTongue { get; }

    event Action JumpStarted;
    event Action JumpCanceled;
    event Action Interact;
    event Action<bool> Aim;
    event Action<bool> FireTongue;

    // Mouse travado no centro da tela (só teclado e mouse)
    bool IsCursorLocked { get; }
    event Action<bool> CursorLockChanged;

    // Nome do control scheme ativo ("KeyboardMouse" ou "Gamepad")
    string CurrentControlScheme { get; }
    event Action<string> ControlSchemeChanged;
}
