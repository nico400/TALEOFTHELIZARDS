using System;
using UnityEngine;

[Serializable]
public class TimedInputBuffer : TimedInputBuffer<bool>     
{
    public void Set() => Set(true);
    public bool TryConsume() => TryConsume(out _);
}

[Serializable]
public class TimedInputBuffer<T>
{
    [SerializeField] T m_bufferValue;
    [SerializeField] float m_holdTime = 0.25f;

    float m_expiryTime = float.NegativeInfinity;
    bool m_hasBufferedInput = false;

    public bool HasBufferedInput => m_hasBufferedInput && Time.time <= m_expiryTime;
    public float HoldTime => m_holdTime;
    public T Value => m_bufferValue;

    public void Set(T value) 
    {
        m_bufferValue = value;
        m_hasBufferedInput = true;
        m_expiryTime = Time.time + m_holdTime;
    }

    public void Consume() 
    {
        m_hasBufferedInput = false;
        m_expiryTime = float.NegativeInfinity;
        m_bufferValue = default;
    }

    public bool TryConsume(out T value) 
    {
        if (!HasBufferedInput) 
        {
            value = default;
            return false;
        }

        value = m_bufferValue;
        Consume();
        return true;
    }

    public void SetHoldTime(float seconds) => m_holdTime = Mathf.Max(0, seconds);
}
