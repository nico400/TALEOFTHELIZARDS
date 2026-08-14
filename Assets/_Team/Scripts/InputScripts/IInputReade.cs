using UnityEngine;

public interface IInputReade
{
    Vector2 DirectionBoard { get; }
    Vector2 LookDirectionBoard { get; }
    Vector2 DirectionPad { get; }
    Vector2 LookDirectionPad { get; }
    bool InteractBoardInput { get; }
    bool InteractPadInput { get; }
    bool AimBoardInput { get; }
    bool FireTongueInput { get; }
    void EnablePlayerInput();
}
