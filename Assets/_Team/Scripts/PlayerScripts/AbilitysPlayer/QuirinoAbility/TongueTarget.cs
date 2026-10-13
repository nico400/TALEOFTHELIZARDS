using UnityEngine;
using UnityEngine.Events;

// Alvo generico da lingua: liga as acoes pelo Inspector.
// Deixar os eventos vazios = a lingua so gruda, sem acao.
// Para algo mais especifico, crie outro script que implemente ITongueTarget.
public class TongueTarget : MonoBehaviour, ITongueTarget
{
    [SerializeField] private UnityEvent m_onAttached;
    [SerializeField] private UnityEvent m_onHeld;
    [SerializeField] private UnityEvent m_onReleased;

    public void OnTongueAttached(ControllerQuirino quirino) => m_onAttached.Invoke();
    public void OnTongueHeld(ControllerQuirino quirino) => m_onHeld.Invoke();
    public void OnTongueReleased(ControllerQuirino quirino) => m_onReleased.Invoke();
}
