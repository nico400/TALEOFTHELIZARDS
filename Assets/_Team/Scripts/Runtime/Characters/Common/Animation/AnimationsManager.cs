using UnityEngine;

// Base das animacoes dos players: os estados chamam PlayX() e aqui faz o CrossFade
// para o estado do Animator com o nome configurado. Nao precisa de transicoes no Animator.
public abstract class AnimationsManager : MonoBehaviour
{
    [Tooltip("Animator do modelo. Vazio = procura nos filhos")]
    [SerializeField] private Animator m_animator;

    [Tooltip("Tempo de mistura entre uma animacao e outra (segundos)")]
    [SerializeField] private float m_crossFadeDuration = 0.1f;

    [Header("Nomes dos estados no Animator")]
    [SerializeField] private string m_idle = "Idle";
    [SerializeField] private string m_walk = "Walk";
    [SerializeField] private string m_startJump = "StartJump";
    [SerializeField] private string m_fall = "Fall";

    private int m_idleHash;
    private int m_walkHash;
    private int m_startJumpHash;
    private int m_fallHash;

    private int m_currentHash;

    protected virtual void Awake()
    {
        if (m_animator == null)
            m_animator = GetComponentInChildren<Animator>();

        if (m_animator == null)
            Debug.LogWarning($"{name}: nenhum Animator encontrado, as animacoes nao vao tocar.", this);

        m_idleHash = Animator.StringToHash(m_idle);
        m_walkHash = Animator.StringToHash(m_walk);
        m_startJumpHash = Animator.StringToHash(m_startJump);
        m_fallHash = Animator.StringToHash(m_fall);
    }

    public void PlayIdle() => Play(m_idleHash);
    public void PlayWalk() => Play(m_walkHash);
    public void PlayStartJump() => Play(m_startJumpHash);
    public void PlayFall() => Play(m_fallHash);

    // Parado ou andando de acordo com o input
    public void PlayLocomotion(bool moving)
    {
        if (moving) PlayWalk();
        else PlayIdle();
    }

    // Ignora se ja estiver tocando a mesma (pode ser chamado todo frame)
    protected void Play(int hash)
    {
        if (m_animator == null || hash == m_currentHash) return;

        m_animator.CrossFadeInFixedTime(hash, m_crossFadeDuration);
        m_currentHash = hash;
    }
}
