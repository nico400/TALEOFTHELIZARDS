using UnityEngine;

// Hitbox que mata: o personagem que encostar morre e renasce no ultimo spawn dele.
// Use embaixo do mapa (queda), em agua, espinhos...
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    [Tooltip("Quem morre ao encostar (pode marcar os dois)")]
    [SerializeField] private InteractorCharacter m_killableCharacters = InteractorCharacter.Draco | InteractorCharacter.Quirino;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        if (!other.attachedRigidbody.TryGetComponent(out PlayerRespawn _player)) return;

        if ((m_killableCharacters & _player.Character) != 0)
            _player.Die();
    }
}
