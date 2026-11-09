using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fica no Draco e no Quirino: morte e renascimento do personagem.
// Comeca com o spawn do Inspector; o Checkpoint troca ele quando alguem passa.
// Quem "mata" o personagem (KillZone, inimigo...) so chama Die().
// O controller escuta Died/Respawned para limpar o estado (lingua, objeto na boca...)
// e nao roda os estados enquanto IsDead.
public class PlayerRespawn : MonoBehaviour
{
    // Personagens ativos na cena: o Checkpoint atualiza os dois por aqui
    private static readonly List<PlayerRespawn> s_active = new();
    public static IReadOnlyList<PlayerRespawn> Active => s_active;

    [Tooltip("Qual personagem e este (o Checkpoint usa para escolher o ponto certo)")]
    [SerializeField] private InteractorCharacter m_character = InteractorCharacter.Draco;

    [Tooltip("GameObject vazio onde o personagem renasce no inicio da fase")]
    [SerializeField] private Transform m_spawnPoint;

    [Tooltip("Tempo morto antes de renascer (animacao, fade...)")]
    [SerializeField, Min(0f)] private float m_respawnDelay = 0.5f;

    [SerializeField] private Rigidbody m_rigidbody;

    public InteractorCharacter Character => m_character;
    public Transform SpawnPoint => m_spawnPoint;
    public bool IsDead { get; private set; }

    // Assim que morre (antes do delay)
    public event Action Died = delegate { };
    // Depois de teleportar
    public event Action Respawned = delegate { };

    private void Awake()
    {
        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable() => s_active.Add(this);

    private void OnDisable()
    {
        s_active.Remove(this);

        // A corrotina para junto: nao deixa o personagem congelado
        if (IsDead)
        {
            IsDead = false;
            if (m_rigidbody != null) m_rigidbody.isKinematic = false;
        }
    }

    public void SetSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint == null) return;
        m_spawnPoint = spawnPoint;
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // Antes de congelar: os Exit() dos estados ainda mexem na velocidade
        Died.Invoke();

        // Congela no lugar ate renascer
        if (m_rigidbody != null)
        {
            m_rigidbody.linearVelocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
            m_rigidbody.isKinematic = true;
        }

        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        if (m_respawnDelay > 0f)
            yield return new WaitForSeconds(m_respawnDelay);

        Respawn();
    }

    // Renasce na hora (sem morrer antes)
    public void Respawn()
    {
        if (m_spawnPoint == null)
        {
            Debug.LogWarning($"{name}: Spawn Point nao configurado, nao da para renascer.", this);
            if (IsDead)
            {
                IsDead = false;
                if (m_rigidbody != null) m_rigidbody.isKinematic = false;
            }
            return;
        }

        StopAllCoroutines();

        Vector3 _position = m_spawnPoint.position;
        Quaternion _rotation = m_spawnPoint.rotation;

        // Move o transform e o rigidbody juntos para a interpolacao nao puxar de volta
        transform.SetPositionAndRotation(_position, _rotation);
        if (m_rigidbody != null)
        {
            m_rigidbody.isKinematic = false;
            m_rigidbody.position = _position;
            m_rigidbody.rotation = _rotation;
            m_rigidbody.linearVelocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
        }

        IsDead = false;
        Respawned.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        if (m_spawnPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, m_spawnPoint.position);
        Gizmos.DrawWireSphere(m_spawnPoint.position, 0.3f);
    }
}
