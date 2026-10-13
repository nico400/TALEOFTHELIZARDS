using System;
using UnityEngine;

public class HandleTogue : MonoBehaviour
{
    private enum TogueState
    {
        Retracted, 
        Extending,
        Attached,
        Retracting
    }
    private TogueState m_state = TogueState.Retracted;

    private void FixedUpdate() 
    {
        switch (m_state) 
        {
            case TogueState.Retracted:
                // Handle logic for retracted state
                break;
            case TogueState.Extending:
                // Handle logic for extending state
                break;
            case TogueState.Attached:
                // Handle logic for attached state
                break;
            case TogueState.Retracting:
                // Handle logic for retracting state
                break;
        }
    }
}
