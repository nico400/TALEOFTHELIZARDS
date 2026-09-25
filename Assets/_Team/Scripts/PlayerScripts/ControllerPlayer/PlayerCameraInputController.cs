using System;
using Unity.Cinemachine;
using UnityEngine;

// Substitui o CinemachineInputAxisController: em vez de ler a action "Look" global (que escuta
// todos os dispositivos), lê o Look do InputReader deste jogador, que já está pareado ao dispositivo dele.
[ExecuteAlways]
[AddComponentMenu("Cinemachine/Helpers/Player Camera Input Controller")]
public class PlayerCameraInputController : InputAxisControllerBase<PlayerCameraInputController.Reader>
{
    [Tooltip("InputReader do jogador dono desta câmera")]
    [SerializeField] private InputReader m_inputReader;
    public IPlayerInput Input => m_inputReader;

    private void Update()
    {
        if (Application.isPlaying)
            UpdateControllers();
    }

    // Valores iniciais de cada eixo quando o componente é adicionado (eixo Y invertido)
    protected override void InitializeControllerDefaultsForAxis(in IInputAxisOwner.AxisDescriptor axis, Controller controller)
    {
        if (axis.Hint != IInputAxisOwner.AxisDescriptor.Hints.Y) return;

        controller.Input.KeyboardMouseGain = -controller.Input.KeyboardMouseGain;
        controller.Input.GamepadGain = -controller.Input.GamepadGain;
    }

    [Serializable]
    public sealed class Reader : IInputAxisReader
    {
        [Tooltip("Multiplicador para o mouse (delta em pixels). Negativo inverte o eixo.")]
        public float KeyboardMouseGain = 9f;

        [Tooltip("Multiplicador para o analógico (-1 a 1). Negativo inverte o eixo.")]
        public float GamepadGain = 120f;

        public float GetValue(UnityEngine.Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            if (context is not PlayerCameraInputController controller || controller.Input == null)
                return 0f;

            IPlayerInput input = controller.Input;
            float value = hint switch
            {
                IInputAxisOwner.AxisDescriptor.Hints.X => input.Look.x,
                IInputAxisOwner.AxisDescriptor.Hints.Y => input.Look.y,
                _ => 0f
            };

            float gain = input.CurrentControlScheme == "KeyboardMouse" ? KeyboardMouseGain : GamepadGain;
            return value * gain;
        }
    }
}
