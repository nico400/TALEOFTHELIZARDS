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

    [Header("Settings Data")]
    [Tooltip("Asset de configurações contendo a sensibilidade do mouse e do gamepad. Se não for atribuído, usa GameSettings.Instance.")]
    [SerializeField] private GameSettings m_settings;
    public GameSettings Settings => m_settings != null ? m_settings : GameSettings.Instance;

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

            // Se o jogo estiver pausado (Time.timeScale == 0), a câmera não gira (nem com mouse nem gamepad)
            if (Time.timeScale == 0f)
                return 0f;

            IPlayerInput input = controller.Input;
            bool _usingMouse = input.CurrentControlScheme == "KeyboardMouse";

            // Mouse solto (para clicar em menus/pause): a camera nao gira
            if (_usingMouse && !input.IsCursorLocked)
                return 0f;

            float value = hint switch
            {
                IInputAxisOwner.AxisDescriptor.Hints.X => input.Look.x,
                IInputAxisOwner.AxisDescriptor.Hints.Y => input.Look.y,
                _ => 0f
            };

            float gain;
            if (_usingMouse)
            {
                float sens = controller.Settings != null ? controller.Settings.SensMouse : KeyboardMouseGain;
                gain = Mathf.Sign(KeyboardMouseGain) * Mathf.Abs(sens);
            }
            else
            {
                float sens = controller.Settings != null ? controller.Settings.SensGamepad : GamepadGain;
                gain = Mathf.Sign(GamepadGain) * Mathf.Abs(sens);
            }

            return value * gain;
        }
    }
}
