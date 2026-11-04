using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using LFramework.Utility;

// Coloque este script no mesmo GameObject que tem o componente Video Player.
[RequireComponent(typeof(VideoPlayer))]
public class TransitionVideo : MonoBehaviour {
    [Tooltip("Nome exato da cena que deve abrir quando o vídeo terminar")]
    public string nextScene = "Fase2";

    [Tooltip("Permite pular o vídeo apertando uma tecla/botão")]
    public bool canJump = true;
    public Key keyJump = Key.Space;

    [Header("References of Screen Fader")]
    [SerializeField] private ScreenFader m_screenFader;

    private VideoPlayer m_videoPlayer;
    private bool m_changing = false;

    private void Awake() {
        m_videoPlayer = GetComponent<VideoPlayer>();
        m_videoPlayer.playOnAwake = false;
        m_videoPlayer.isLooping = false;

        if(m_screenFader == null) {
            EditorLogger.LogWarning("Screen Fader reference is missing. Please assign it in the inspector.");
        }

        // Evento chamado quando o vídeo chega ao fim
        m_videoPlayer.loopPointReached += WhenEndVideo;
    }

    private void Start() {
        // Prepara antes de tocar para evitar tela preta/travada no início
        m_videoPlayer.prepareCompleted += vp => vp.Play();
        m_videoPlayer.Prepare();

        if(AudioManager.Instance != null) {
            AudioManager.Instance.StopMusic();
        }

        if (m_screenFader != null) {
            m_screenFader.FadeIn();
        }
    }

    private void Update() {
        if (!canJump) return;

        bool _pressedKeyboard = Keyboard.current != null &&
                              Keyboard.current[keyJump].wasPressedThisFrame;

        // Botão "A" no Xbox / "X" no PlayStation
        bool _pressedGamepad = Gamepad.current != null &&
                               Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (_pressedKeyboard || _pressedGamepad)
            LoadNextScene();
    }

    private void WhenEndVideo(VideoPlayer vp) {
        if(m_screenFader != null) {
            m_screenFader.FadeOut(LoadNextScene);
        }
        else {
            LoadNextScene();
        }
    }

    private void LoadNextScene() {
        if (m_changing) return;   // evita carregar duas vezes
        m_changing = true;
        SceneManager.LoadScene(nextScene);
    }

    private void OnDestroy() {
        if (m_videoPlayer != null)
            m_videoPlayer.loopPointReached -= WhenEndVideo;
    }
}
