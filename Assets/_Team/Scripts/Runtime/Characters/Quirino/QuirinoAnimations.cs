using UnityEngine;

// Animacoes do Quirino: as basicas + boca aberta (lingua), mirando e em cima de um JumpPoint
public class QuirinoAnimations : AnimationsManager
{
    [Header("Nomes dos estados extras do Quirino")]
    [Tooltip("Boca aberta esticando a lingua")]
    [SerializeField] private string m_openMouth = "OpenMouth";
    [Tooltip("Boca fechada mirando")]
    [SerializeField] private string m_aim = "Aim";
    [Tooltip("Em cima do Target de um JumpPoint")]
    [SerializeField] private string m_perched = "Perched";

    private int m_openMouthHash;
    private int m_aimHash;
    private int m_perchedHash;

    protected override void Awake()
    {
        base.Awake();

        m_openMouthHash = Animator.StringToHash(m_openMouth);
        m_aimHash = Animator.StringToHash(m_aim);
        m_perchedHash = Animator.StringToHash(m_perched);
    }

    public void PlayOpenMouth() => Play(m_openMouthHash);
    public void PlayAim() => Play(m_aimHash);
    public void PlayPerched() => Play(m_perchedHash);
}
