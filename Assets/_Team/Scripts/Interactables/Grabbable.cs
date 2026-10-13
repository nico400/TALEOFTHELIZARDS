using System;
using UnityEngine;
using UnityEngine.Events;

// Objeto que o Draco pega com Interagir e carrega na boca.
// Enquanto segurado fica preso no ponto da boca, sem fisica e sem colisao.
// Liga as acoes de pegar/soltar pelo Inspector ou pelos eventos em codigo.
public class Grabbable : MonoBehaviour
{
    [SerializeField] private UnityEvent m_onGrabbed;
    [SerializeField] private UnityEvent m_onReleased;

    // Para scripts que precisam saber qual objeto foi pego/solto
    public event Action<Grabbable> Grabbed = delegate { };
    public event Action<Grabbable> Released = delegate { };

    public bool IsHeld { get; private set; }
    public bool CanBeGrabbed => !IsHeld && isActiveAndEnabled;

    private Rigidbody m_rigidbody;
    protected Rigidbody Body => m_rigidbody;
    private Collider[] m_colliders;
    private bool[] m_collidersEnabled;
    private Transform m_originalParent;
    private bool m_wasKinematic;
    private RigidbodyInterpolation m_interpolation;

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_colliders = GetComponentsInChildren<Collider>();
        m_collidersEnabled = new bool[m_colliders.Length];
    }

    public void Grab(Transform holdPoint)
    {
        if (!CanBeGrabbed) return;
        IsHeld = true;
        OnGrab();

        if (m_rigidbody != null)
        {
            m_wasKinematic = m_rigidbody.isKinematic;
            m_interpolation = m_rigidbody.interpolation;

            if (!m_rigidbody.isKinematic)
            {
                m_rigidbody.linearVelocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }

            m_rigidbody.isKinematic = true;
            // Interpolacao briga com o transform seguindo o pai
            m_rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        // Sem colisao para nao empurrar o Draco nem prender em parede
        for (int i = 0; i < m_colliders.Length; i++)
        {
            m_collidersEnabled[i] = m_colliders[i].enabled;
            m_colliders[i].enabled = false;
        }

        m_originalParent = transform.parent;
        transform.SetParent(holdPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        m_onGrabbed.Invoke();
        Grabbed.Invoke(this);
    }

    // velocity: velocidade com que sai da boca (ex: a do Draco, para nao parar no ar)
    public void Release(Vector3 velocity)
    {
        if (!IsHeld) return;
        IsHeld = false;

        transform.SetParent(m_originalParent, true);

        for (int i = 0; i < m_colliders.Length; i++)
            m_colliders[i].enabled = m_collidersEnabled[i];

        if (m_rigidbody != null)
        {
            m_rigidbody.isKinematic = m_wasKinematic;
            m_rigidbody.interpolation = m_interpolation;

            if (!m_rigidbody.isKinematic)
                m_rigidbody.linearVelocity = velocity;
        }

        OnRelease();

        m_onReleased.Invoke();
        Released.Invoke(this);
    }

    // Antes de salvar o estado da fisica e prender na boca
    protected virtual void OnGrab() { }

    // Depois de voltar a fisica e colisao, antes dos eventos de soltar
    protected virtual void OnRelease() { }
}
