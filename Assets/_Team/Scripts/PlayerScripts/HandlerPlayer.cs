using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HandlerPlayer : MonoBehaviour
{
    enum StateInput { Keyboard, Gamepad }
    [SerializeField] StateInput currentInput = StateInput.Keyboard;

    [Header("Input Controller")]
    [SerializeField] InputReader inputReader;

    [Header("Player Components")]
    [SerializeField] CharacterController characterController;
    [SerializeField] Transform playerCamera;

    [Header("Player Movement Config")]
    [SerializeField] float maxSpeed = 5f;
    [SerializeField] float acceleration = 10f;
    [SerializeField] float deceleration = 10f;

    IMovimentation movementComponent;
    Vector3 DirectionMove;

    private void Awake()
    {
        if (inputReader == null)
            Debug.LogError("InputReader não foi atribuído!", this);
        if (playerCamera == null)
            Debug.LogError("Player Camera não foi atribuída!", this);

        inputReader.EnablePlayerInput();
        movementComponent = new Movimentation(maxSpeed, acceleration, deceleration);
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        switch (currentInput)
        {
            case StateInput.Keyboard:
                HandleKeyboardInput();
                break;
            case StateInput.Gamepad:
                HandleGamepadInput();
                break;
        }
    }

    private void FixedUpdate()
    {
        Vector2 inputDirection = new Vector2(DirectionMove.x, DirectionMove.z);
        movementComponent.Move(inputDirection, characterController);
    }

    private void HandleKeyboardInput()
    {
        DirectionMove = playerCamera.forward * inputReader.DirectionBoard.y +
            playerCamera.right * inputReader.DirectionBoard.x;
        DirectionMove.y = 0f;
        DirectionMove = DirectionMove.normalized;
    }

    private void HandleGamepadInput()
    {
        DirectionMove = playerCamera.forward * inputReader.DirectionPad.y +
            playerCamera.right * inputReader.DirectionPad.x;
        DirectionMove.y = 0f;
        DirectionMove = DirectionMove.normalized;
    }
}
