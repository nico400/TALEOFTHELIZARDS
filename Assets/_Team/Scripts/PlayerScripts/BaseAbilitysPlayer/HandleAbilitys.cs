using UnityEngine;

public class HandleAbilitys : MonoBehaviour
{
    public enum PersoAbility 
    {
        Draco,
        Quirino
    }

    [SerializeField] private PersoAbility m_persoAbility;
    CharacterAbility m_ability;

    private void Awake() 
    {
        if (m_ability == null) 
        {
            m_ability = m_persoAbility == PersoAbility.Quirino ? new CharacterAbility(new QuirinoTogueAbility()) : new CharacterAbility(new DracoWalkInWallsAbility());
        }

        m_ability.Ability();
    }
}
