using UnityEngine;

// Liga a pintura ao Ctrl+Z/Ctrl+Y do Unity: cada pincelada grava uma nova versão aqui pelo Undo,
// e a LayeredPaintWindow guarda os pixels de cada versão para restaurar quando o Undo mudar o número.
public class LayeredPaintUndoState : ScriptableObject
{
    public int version;
}
