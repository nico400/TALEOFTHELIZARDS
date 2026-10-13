using UnityEngine;

// Lingua presa no alvo enquanto o botao de atirar estiver segurado.
// Depois de TongueHeldEventTime segundos avisa o alvo uma vez.
public class TongueAttachedStateQuirino : State<ControllerQuirino>
{
    private float m_attachedTime;
    private bool m_heldEventFired;

    public TongueAttachedStateQuirino(ControllerQuirino controller) : base(controller) { }

    public override void Enter()
    {
        m_attachedTime = 0f;
        m_heldEventFired = false;
        Controller.Motor.SetCameraOffset(Controller.Settings.AimCameraOffset);
        Controller.Tongue.IsAttached = true;
        Controller.Tongue.Target.OnTongueAttached(Controller);
    }

    public override void Update()
    {
        // Soltou o botao, o alvo sumiu ou ficou longe demais: recolhe
        if (!Controller.PlayerInput.IsFiringTongue || !Controller.Tongue.IsAttachInRange)
        {
            Controller.SwitchState(Controller.TongueRetractState);
            return;
        }

        Controller.Tongue.Draw();
    }

    public override void FixedUpdate()
    {
        Controller.Motor.StopOnGround();

        QuirinoTongue _tongue = Controller.Tongue;
        if (!_tongue.HasTarget) return;

        _tongue.SnapTipToAttachPoint(); // acompanha o alvo se ele se mexer

        m_attachedTime += Time.fixedDeltaTime;
        if (!m_heldEventFired && m_attachedTime >= Controller.Settings.TongueHeldEventTime)
        {
            m_heldEventFired = true;
            _tongue.Target.OnTongueHeld(Controller);
        }
    }

    public override void Exit()
    {
        Controller.Motor.ResetCameraOffset();
        Controller.Tongue.IsAttached = false; // derruba o Draco se estiver em cima

        if (Controller.Tongue.HasTarget)
            Controller.Tongue.Target.OnTongueReleased(Controller);
    }
}
