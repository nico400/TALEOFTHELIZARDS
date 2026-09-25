using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/CreateSODataPlayer")]
public class SODataPlayer : ScriptableObject
{
    [SerializeField] private InputReader m_inputReader;
    public IPlayerInput Input => m_inputReader;

    [SerializeField] private float m_speedPlayer;
    public float SpeedPlayer => m_speedPlayer;
}
