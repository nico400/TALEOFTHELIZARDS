using UnityEngine;

// Quirino preso num Interactable que trava (ex: CoopInteractable esperando o Draco).
// Fica parado sem andar/pular/mirar. O Interagir do controller solta; aqui so volta
// para a locomocao quando a interacao acabou.
public class InteractStateQuirino : State<ControllerQuirino>
{
    public InteractStateQuirino(ControllerQuirino controller) : base(controller) { }

    public override void Enter()
    {
        Controller.Animations.PlayIdle();
    }

    public override void Update()
    {
        if (!Controller.Interactor.IsLocked)
            Controller.SwitchState(Controller.LocomotionState);
    }

    public override void FixedUpdate()
    {
        Controller.Motor.MoveOnGround(Vector2.zero); // desacelera ate parar
    }

    // Pulo/tiro apertados enquanto preso nao saem depois de soltar
    public override void Exit()
    {
        Controller.JumpBuffer.Consume();
        Controller.ConsumeFireTongue();
    }
}
