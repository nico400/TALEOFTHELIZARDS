using UnityEngine;

public interface IInputReade 
{
    Vector2 Direction { get;}
    Vector2 LookDirection { get; }
    void EnablePlayerInput();
}
