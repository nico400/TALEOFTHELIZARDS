using Unity.VectorGraphics;
using UnityEngine;

public class LoadScene : MonoBehaviour
{
    [SerializeField] private string m_sceneName;
    public void LoadSceneByName()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(m_sceneName);
    }

    private void OnTriggerEnter(Collider other) 
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(m_sceneName);
    }
}
