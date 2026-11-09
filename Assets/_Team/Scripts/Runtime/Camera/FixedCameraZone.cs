using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// Zona de câmera fixa: enquanto o personagem está dentro do trigger, a câmera dele vai para a
// CinemachineCamera desta zona (posição parada, sem o jogador girar). Ao sair, volta para a 3ª pessoa.
// Em split-screen cada player tem o seu Brain/canal, então a zona cria uma cópia da câmera para cada
// personagem que entra, no canal dele. A transição usa o Default Blend do CinemachineBrain.
[RequireComponent(typeof(Collider))]
public class FixedCameraZone : MonoBehaviour
{
    [Tooltip("CinemachineCamera posicionada no ponto fixo. Position Control: None. " +
             "Rotation Control: None (totalmente parada) ou Rotation Composer (gira só para olhar o player)")]
    [SerializeField] private CinemachineCamera m_zoneCamera;

    [Tooltip("Quem troca de câmera ao entrar (pode marcar os dois)")]
    [SerializeField] private InteractorCharacter m_affectedCharacters = InteractorCharacter.Draco | InteractorCharacter.Quirino;

    [Tooltip("Usa o personagem como LookAt (só faz diferença se a câmera tiver Rotation Composer/Hard Look At)")]
    [SerializeField] private bool m_lookAtPlayer = true;

    [Tooltip("Prioridade da câmera fixa. Precisa ser maior que a da câmera de 3ª pessoa (0); " +
             "em zonas sobrepostas, a de maior prioridade ganha")]
    [SerializeField] private int m_priority = 10;

    // Uma cópia da câmera por personagem + quantos colliders dele estão dentro (o player pode ter vários)
    private readonly Dictionary<Rigidbody, CinemachineCamera> m_cameras = new();
    private readonly Dictionary<Rigidbody, int> m_overlaps = new();

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Awake()
    {
        if (m_zoneCamera == null)
        {
            Debug.LogError("Zone Camera não foi atribuída!", this);
            return;
        }

        // O original serve só de modelo: não pode ficar ativo em nenhum canal
        m_zoneCamera.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody _body = other.attachedRigidbody;
        if (!IsAffected(_body)) return;

        m_overlaps.TryGetValue(_body, out int _count);
        m_overlaps[_body] = _count + 1;
        if (_count > 0) return;

        CinemachineCamera _camera = GetCameraFor(_body);
        if (_camera != null)
            _camera.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody _body = other.attachedRigidbody;
        if (_body == null || !m_overlaps.TryGetValue(_body, out int _count)) return;

        if (_count > 1)
        {
            m_overlaps[_body] = _count - 1;
            return;
        }

        m_overlaps.Remove(_body);
        if (m_cameras.TryGetValue(_body, out CinemachineCamera _camera) && _camera != null)
            _camera.gameObject.SetActive(false);
    }

    // Zona desligada (ou cena descarregando): devolve a câmera de 3ª pessoa para todo mundo
    private void OnDisable()
    {
        foreach (CinemachineCamera _camera in m_cameras.Values)
        {
            if (_camera != null)
                _camera.gameObject.SetActive(false);
        }
        m_overlaps.Clear();
    }

    private bool IsAffected(Rigidbody body)
    {
        if (body == null || m_zoneCamera == null) return false;
        if (!body.TryGetComponent(out PlayerRespawn _player)) return false;

        return (m_affectedCharacters & _player.Character) != 0;
    }

    private CinemachineCamera GetCameraFor(Rigidbody body)
    {
        if (m_cameras.TryGetValue(body, out CinemachineCamera _camera))
            return _camera;

        // O Brain do player diz em qual canal a câmera dele escuta
        CinemachineBrain _brain = body.GetComponentInChildren<CinemachineBrain>();
        if (_brain == null)
        {
            Debug.LogError($"{body.name} não tem CinemachineBrain nos filhos!", this);
            return null;
        }

        _camera = Instantiate(m_zoneCamera, m_zoneCamera.transform.parent);
        _camera.name = $"{m_zoneCamera.name} ({body.name})";
        _camera.OutputChannel = _brain.ChannelMask;
        _camera.Priority = m_priority;
        if (m_lookAtPlayer)
            _camera.LookAt = body.transform;

        m_cameras[body] = _camera;
        return _camera;
    }

    private void OnDrawGizmos()
    {
        if (m_zoneCamera == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, m_zoneCamera.transform.position);
        Gizmos.DrawWireSphere(m_zoneCamera.transform.position, 0.3f);
    }
}
