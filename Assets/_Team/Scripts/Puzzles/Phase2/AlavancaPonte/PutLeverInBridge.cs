using UnityEngine;

public class PutLeverInBridge : MonoBehaviour
{
    [SerializeField] private GameObject m_leverFake;
    [SerializeField] private GameObject m_leverReal;
    [SerializeField] private float distaceToPutLever = 1.5f;

    public void OnPutLeverInBridge()
    {
        if (Vector3.Distance(m_leverFake.transform.position, m_leverReal.transform.position) <= distaceToPutLever)
        {
            m_leverFake.SetActive(false);
            m_leverReal.SetActive(true);
        }
    }

    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(m_leverReal.transform.position, distaceToPutLever);
    }
}
