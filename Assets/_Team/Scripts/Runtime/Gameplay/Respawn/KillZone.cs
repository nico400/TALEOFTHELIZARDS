using UnityEngine;

// Hitbox que mata: o personagem que encostar morre e renasce no ultimo spawn dele.
// Use embaixo do mapa (queda), em agua, espinhos...
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    [Tooltip("Quem morre ao encostar (pode marcar os dois)")]
    [SerializeField] private InteractorCharacter m_killableCharacters = InteractorCharacter.Draco | InteractorCharacter.Quirino;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    [Tooltip("Objetos pegaveis soltos que encostarem voltam para onde comecaram")]
    [SerializeField] private bool m_resetGrabbables = true;

    private void OnTriggerEnter(Collider other)
    {
        if (m_resetGrabbables)
        {
            Grabbable _grabbable = other.GetComponentInParent<Grabbable>();
            if (_grabbable != null && !_grabbable.IsHeld)
            {
                _grabbable.ResetToStart();
                return;
            }
        }

        if (other.attachedRigidbody == null) return;
        if (!other.attachedRigidbody.TryGetComponent(out PlayerRespawn _player)) return;

        if ((m_killableCharacters & _player.Character) != 0)
            _player.Die();
    }
}
