using UnityEngine;

// Hitbox de save point: quando o Draco ou o Quirino passa, os DOIS trocam
// o spawn pelos pontos guardados aqui (um para cada personagem).
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("GameObject vazio onde o Quirino renasce depois deste checkpoint")]
    [SerializeField] private Transform m_quirinoSpawnPoint;
    [Tooltip("GameObject vazio onde o Draco renasce depois deste checkpoint")]
    [SerializeField] private Transform m_dracoSpawnPoint;

    [Tooltip("So ativa uma vez: voltar por um checkpoint antigo nao desfaz o progresso")]
    [SerializeField] private bool m_activateOnce = true;

    private bool m_activated;

    // Garante que o collider ja vem como trigger ao adicionar o componente
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (m_activated && m_activateOnce) return;

        // attachedRigidbody: objeto na boca do Draco tem rigidbody proprio e nao conta
        if (other.attachedRigidbody == null || !other.attachedRigidbody.TryGetComponent(out PlayerRespawn _)) return;

        Activate();
    }

    public void Activate()
    {
        m_activated = true;

        foreach (PlayerRespawn _player in PlayerRespawn.Active)
            _player.SetSpawnPoint(GetSpawnPointFor(_player.Character));
    }

    private Transform GetSpawnPointFor(InteractorCharacter character)
    {
        return character switch
        {
            InteractorCharacter.Quirino => m_quirinoSpawnPoint,
            InteractorCharacter.Draco => m_dracoSpawnPoint,
            _ => null,
        };
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = m_activated ? Color.green : Color.yellow;
        if (m_quirinoSpawnPoint != null)
        {
            Gizmos.DrawLine(transform.position, m_quirinoSpawnPoint.position);
            Gizmos.DrawWireSphere(m_quirinoSpawnPoint.position, 0.3f);
        }
        if (m_dracoSpawnPoint != null)
        {
            Gizmos.DrawLine(transform.position, m_dracoSpawnPoint.position);
            Gizmos.DrawWireSphere(m_dracoSpawnPoint.position, 0.3f);
        }
    }
}
