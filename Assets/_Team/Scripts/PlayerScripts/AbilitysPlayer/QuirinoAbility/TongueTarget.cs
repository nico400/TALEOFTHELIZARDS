using UnityEngine;
using UnityEngine.Events;

// Alvo generico da lingua: liga as acoes pelo Inspector.
// Deixar os eventos vazios = a lingua so gruda, sem acao.
// Para algo mais especifico, crie outro script que implemente ITongueTarget.
public class TongueTarget : MonoBehaviour, ITongueTarget
{
    [Tooltip("Lingua presa aqui vira ponte: o Draco pode subir nela com Interagir")]
    [SerializeField] private bool m_isWalkable;
    public bool IsWalkable => m_isWalkable;

    [Tooltip("Depois do On Held disparar, a lingua solta sozinha (so se o On Held tiver alguma acao)")]
    [SerializeField] private bool m_releaseAfterHeld = true;
    public bool ReleaseAfterHeld => m_releaseAfterHeld && m_onHeld.GetPersistentEventCount() > 0;

    [SerializeField] private UnityEvent m_onAttached;
    [SerializeField] private UnityEvent m_onHeld;
    [SerializeField] private UnityEvent m_onReleased;

    public void OnTongueAttached(ControllerQuirino quirino) => m_onAttached.Invoke();
    public void OnTongueHeld(ControllerQuirino quirino) => m_onHeld.Invoke();
    public void OnTongueReleased(ControllerQuirino quirino) => m_onReleased.Invoke();
}
