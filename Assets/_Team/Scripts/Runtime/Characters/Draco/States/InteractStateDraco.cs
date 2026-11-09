using UnityEngine;

// Draco preso num Interactable que trava (ex: CoopInteractable esperando o Quirino).
// Fica parado sem andar/pular; Interagir de novo solta. Sai sozinho quando o objeto solta.
public class InteractStateDraco : State<ControllerDraco>
{
    public InteractStateDraco(ControllerDraco controller) : base(controller) { }

    public override void Enter()
    {
        Controller.Animations.PlayIdle();
    }

    public override void Update()
    {
        // Interagindo, o TryInteract so para a interacao
        if (Controller.Interactor.IsLocked)
            Controller.TryInteract();

        if (!Controller.Interactor.IsLocked)
            Controller.SwitchState(Controller.LocomotionState);
    }

    public override void FixedUpdate()
    {
        Controller.Motor.MoveOnGround(Vector2.zero); // desacelera ate parar
    }

    // Pulo apertado enquanto preso nao sai depois de soltar
    public override void Exit()
    {
        Controller.JumpBuffer.Consume();
    }
}
