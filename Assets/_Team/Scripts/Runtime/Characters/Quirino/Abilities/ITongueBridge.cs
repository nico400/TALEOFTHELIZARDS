using UnityEngine;

// O que o Draco precisa saber da lingua para andar em cima dela.
// Assim o Draco nao depende dos estados do Quirino.
public interface ITongueBridge
{
    // Lingua presa em um alvo que permite andar
    bool IsWalkable { get; }

    // Boca do Quirino
    Vector3 BridgeStart { get; }

    // Ponto onde a lingua esta presa
    Vector3 BridgeEnd { get; }
}
