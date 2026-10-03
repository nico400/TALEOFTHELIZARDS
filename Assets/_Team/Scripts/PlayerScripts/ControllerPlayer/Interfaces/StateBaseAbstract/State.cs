
using UnityEngine;

// Base sem controller: e o que o SwitchState de qualquer state machine recebe
public abstract class State
{
    public virtual void Enter() { }

    // Leitura de input e transicoes de estado
    public virtual void Update() { }

    // Tudo que mexe no Rigidbody (velocidade, rotacao)
    public virtual void FixedUpdate() { }

    public virtual void Exit() { }
}

// Cada player herda daqui passando o proprio controller (Draco, Quirino...)
public abstract class State<TController> : State where TController : MonoBehaviour
{
    protected readonly TController Controller;

    protected State(TController controller)
    {
        Controller = controller;
    }
}
