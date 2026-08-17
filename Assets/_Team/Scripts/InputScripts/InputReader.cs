using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayersControlls;

[CreateAssetMenu(fileName = "InputReade", menuName = "ScriptableObjects/InputReade", order = 1)]
public class InputReader : ScriptableObject, IInputReade, IKeyboardActions, IGamepadActions
{
    public event Action<Vector2> MoveBoard = delegate { };
    public event Action<Vector2> LookBoard = delegate { };
    public event Action<Vector2> MovePad = delegate { };
    public event Action<Vector2> LookPad = delegate { };
    public event Action<bool> JumpBoard = delegate { };
    public event Action<bool> JumpPad = delegate { };
    public event Action<bool> InteractBoard = delegate { };
    public event Action<bool> InteractPad = delegate { };
    public event Action<bool> AimBoard = delegate { };
    public event Action<bool> FireTongue = delegate { };
    
    public PlayersControlls InputAction { get; private set; }

    // keyboard input
    public Vector2 DirectionBoard => InputAction.Keyboard.MoveKeyboard.ReadValue<Vector2>();
    public Vector2 LookDirectionBoard => InputAction.Keyboard.LookKeyboard.ReadValue<Vector2>();
    public bool InteractBoardInput => InputAction.Keyboard.InteractionKeyboard.WasPressedThisFrame();
    public bool JumpBoardInputStart => InputAction.Keyboard.JumpKeyboard.WasPressedThisFrame();
    public bool JumpBoardInputRelease => InputAction.Keyboard.JumpKeyboard.WasReleasedThisFrame();
    public bool AimBoardInput => InputAction.Keyboard.AimKeyboard.IsPressed();
    public bool FireTongueInput => InputAction.Keyboard.FireTongue.IsPressed();

    // gamepad input
    public Vector2 DirectionPad => InputAction.Gamepad.MoveGamepad.ReadValue<Vector2>();
    public Vector2 LookDirectionPad => InputAction.Gamepad.LookGamepad.ReadValue<Vector2>();
    public bool InteractPadInput => InputAction.Gamepad.InteractionGamepad.WasPressedThisFrame();
    public bool JumpPadInputStart => InputAction.Gamepad.JumpGamepad.WasPressedThisFrame();
    public bool JumpPadInputRelease => InputAction.Gamepad.JumpGamepad.WasReleasedThisFrame();

    public void EnablePlayerInput()
    {
        if (InputAction == null)
        {
            InputAction = new PlayersControlls();
            InputAction.Keyboard.SetCallbacks(this);
            InputAction.Gamepad.SetCallbacks(this);
        }

        // Ativar os mapas de ação
        InputAction.Keyboard.Enable();
        InputAction.Gamepad.Enable();
        InputAction.Enable();
    }

    public void OnMoveKeyboard(InputAction.CallbackContext context)
    {
        MoveBoard.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLookKeyboard(InputAction.CallbackContext context)
    {
        LookBoard.Invoke(context.ReadValue<Vector2>());
    }

    public void OnMoveGamepad(InputAction.CallbackContext context)
    {
        MovePad.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLookGamepad(InputAction.CallbackContext context) 
    {
        LookPad.Invoke(context.ReadValue<Vector2>());
    }

    public void OnInteractionKeyboard(InputAction.CallbackContext context)
    {
        InteractBoard.Invoke(context.ReadValueAsButton());
    }

    public void OnInteractionGamepad(InputAction.CallbackContext context) 
    {
        InteractPad.Invoke(context.ReadValueAsButton());
    }

    public void OnAimKeyboard(InputAction.CallbackContext context)
    {
        AimBoard.Invoke(context.ReadValueAsButton());
    }

    public void OnFireTongue(InputAction.CallbackContext context)
    {
        FireTongue.Invoke(context.ReadValueAsButton());
    }

    public void OnJumpKeyboard(InputAction.CallbackContext context) 
    {
        JumpBoard.Invoke(context.ReadValueAsButton());
    }

    public void OnJumpGamepad(InputAction.CallbackContext context) 
    {
        JumpPad.Invoke(context.ReadValueAsButton());
    }
}
