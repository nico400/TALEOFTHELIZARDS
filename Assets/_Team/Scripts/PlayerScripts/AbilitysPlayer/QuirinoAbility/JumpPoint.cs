using UnityEngine;

// Objeto para onde o Quirino pode dar um pulo direcionado.
// O filho Target marca onde o pe do Quirino pousa (deixe na superficie de cima).
// O objeto precisa de um Collider para entrar na area de deteccao do Quirino.
public class JumpPoint : MonoBehaviour
{
    [Tooltip("Filho que marca onde o Quirino pousa. Vazio = posicao deste objeto")]
    [SerializeField] private Transform m_target;

    public Vector3 LandPosition => m_target != null ? m_target.position : transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(LandPosition, 0.25f);
        Gizmos.DrawLine(LandPosition, LandPosition + Vector3.up * 0.75f);
    }
}
