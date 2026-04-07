using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayersControlls;

[CreateAssetMenu(fileName = "InputReade", menuName = "ScriptableObjects/InputReade", order = 1)]
public class InputReade : ScriptableObject, IInputReade, IGameplayActions
{
    public event Action<Vector2> Move = delegate { };
    public event Action<Vector2> Look = delegate { };

    public PlayersControlls InputAction { get; private set; }

    public Vector2 Direction => InputAction.Gameplay.Move.ReadValue<Vector2>();
    public Vector2 LookDirection => InputAction.Gameplay.Look.ReadValue<Vector2>();

    public void EnablePlayerInput()
    {
        if (InputAction == null)
        {
            InputAction = new PlayersControlls();
            InputAction.Gameplay.SetCallbacks(this);
        }
        InputAction.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Move.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        Look.Invoke(context.ReadValue<Vector2>());
    }
}
