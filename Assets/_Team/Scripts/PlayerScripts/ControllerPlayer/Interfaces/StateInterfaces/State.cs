
using UnityEngine;

public abstract class State
{
    protected readonly StateMachineControllerDraco Controller;

    protected State(StateMachineControllerDraco controller)
    {
        Controller = controller;
    }

    public virtual void Enter() { }

    // Leitura de input e transicoes de estado
    public virtual void Update() { }

    // Tudo que mexe no Rigidbody (velocidade, rotacao)
    public virtual void FixedUpdate() { }

    public virtual void Exit() { }
}
