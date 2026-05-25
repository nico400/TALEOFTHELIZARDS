using UnityEngine;

public class AttachObjects : MonoBehaviour, IAttach
{
    [SerializeField] private Transform m_attachPos;
    public Transform AttachPos => m_attachPos;
}
