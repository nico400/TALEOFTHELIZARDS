using UnityEngine;

// Draco grudado em superficie escalavel (tag "Climb"): anda alinhado com a normal
// e fica parado grudado quando nao tem input. Sai quando perde a superficie.
public class ClimbStateDraco : State<ControllerDraco>
{
    public ClimbStateDraco(ControllerDraco controller) : base(controller) { }

    public override void Update()
    {
        if (Controller.TryInteract()) return;

        if (Controller.JumpBuffer.HasBufferedInput)
        {
            Controller.SwitchState(Controller.JumpState);
            return;
        }

        // Saiu da parede (passou o NormalGraceTime sem achar superficie escalavel)
        if (!Controller.Motor.IsOnClimbSurface)
        {
            Controller.SwitchState(Controller.LocomotionState);
            return;
        }

        Controller.Animations.PlayLocomotion(Controller.HasMoveInput);
    }

    public override void FixedUpdate()
    {
        if (!Controller.Motor.IsOnClimbSurface) return; // o Update troca de estado

        Controller.Motor.MoveOnSurface(Controller.HasMoveInput);
    }
}
