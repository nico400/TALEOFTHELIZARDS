using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// Coloque este script no mesmo GameObject que tem o componente Video Player.
[RequireComponent(typeof(VideoPlayer))]
public class VideoTransicao : MonoBehaviour {
    [Tooltip("Nome exato da cena que deve abrir quando o vídeo terminar")]
    public string proximaCena = "Fase2";

    [Tooltip("Permite pular o vídeo apertando uma tecla/botão")]
    public bool podePular = true;
    public Key teclaPular = Key.Space;

    private VideoPlayer videoPlayer;
    private bool trocando = false;

    void Awake() {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        // Evento chamado quando o vídeo chega ao fim
        videoPlayer.loopPointReached += AoTerminarVideo;
    }

    void Start() {
        // Prepara antes de tocar para evitar tela preta/travada no início
        videoPlayer.prepareCompleted += vp => vp.Play();
        videoPlayer.Prepare();
    }

    void Update() {
        if (!podePular) return;

        bool apertouTeclado = Keyboard.current != null &&
                              Keyboard.current[teclaPular].wasPressedThisFrame;

        // Botão "A" no Xbox / "X" no PlayStation
        bool apertouControle = Gamepad.current != null &&
                               Gamepad.current.buttonSouth.wasPressedThisFrame;

        if (apertouTeclado || apertouControle)
            CarregarProximaCena();
    }

    void AoTerminarVideo(VideoPlayer vp) {
        CarregarProximaCena();
    }

    void CarregarProximaCena() {
        if (trocando) return;   // evita carregar duas vezes
        trocando = true;
        SceneManager.LoadScene(proximaCena);
    }

    void OnDestroy() {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= AoTerminarVideo;
    }
}
