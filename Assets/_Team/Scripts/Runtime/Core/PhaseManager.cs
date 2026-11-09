using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    [SerializeField] private AudioClip m_musicClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(m_musicClip != null) {
            AudioManager.Instance.PlayMusic(m_musicClip);
        }
    }
}
