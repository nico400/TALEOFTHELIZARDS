using UnityEngine;

public class CharacterAbility
{
    IAbilityPlayer m_abilityPlayer;
    public CharacterAbility(IAbilityPlayer abilityPlayer) 
    {
        m_abilityPlayer = abilityPlayer;
    }

    public void Ability() 
    {
        m_abilityPlayer.ExecuteAbility();
    }
}
