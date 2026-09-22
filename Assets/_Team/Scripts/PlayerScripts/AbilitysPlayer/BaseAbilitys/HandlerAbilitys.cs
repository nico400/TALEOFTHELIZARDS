using System;
using UnityEngine;

public class HandlerAbilitys : MonoBehaviour
{
    public enum PersoAbility 
    {
        Draco,
        Quirino
    }

    [SerializeField] private PersoAbility m_persoAbility;
    [SerializeField] private HandlerPlayer m_handlerPlayer;
    [SerializeField] private Transform m_cam;
    [SerializeField] private Rigidbody m_rb;
    CharacterAbility m_ability;

    private void Awake() 
    {
        if (m_ability == null) 
        {
            m_ability = m_persoAbility == PersoAbility.Quirino ? 
                new CharacterAbility(new QuirinoTogueAbility()) : 
                new CharacterAbility(new DracoWalkInWallsAbility(() => m_handlerPlayer.InputReader.DirectionBoard, m_cam, m_rb));
        }
    }

    private void FixedUpdate() 
    {
        if (m_ability == null) return;
        m_ability.Ability(transform);
    }
}
