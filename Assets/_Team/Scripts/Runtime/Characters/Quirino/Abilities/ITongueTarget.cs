// Objetos que a lingua do Quirino pode prender.
// Sem isso, a lingua bate no objeto e volta.
public interface ITongueTarget
{
    // Lingua presa aqui vira ponte para o Draco andar por cima
    bool IsWalkable { get; }

    // Depois do OnTongueHeld a lingua solta sozinha
    bool ReleaseAfterHeld { get; }

    void OnTongueAttached(ControllerQuirino quirino);

    // Lingua presa por TongueHeldEventTime segundos (dispara uma vez)
    void OnTongueHeld(ControllerQuirino quirino);

    void OnTongueReleased(ControllerQuirino quirino);
}
