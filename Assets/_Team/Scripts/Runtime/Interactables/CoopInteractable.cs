using UnityEngine;

// Metade de uma interacao em dupla: o personagem que interage fica preso "esperando"
// ate o outro interagir com o outro CoopInteractable do mesmo CoopInteractionGroup.
// Apertar Interagir de novo solta. Os eventos de comecar/parar da base servem para
// o feedback de "esperando"; o evento final fica no grupo.
public class CoopInteractable : Interactable
{
    // Preenchido pelo grupo que lista este objeto
    public CoopInteractionGroup Group { get; internal set; }

    public override InteractionMode Mode => InteractionMode.Toggle;

    // Preso parado nao sai do alcance; parar e so pelo botao ou pelo grupo
    public override bool StopWhenOutOfRange => false;

    public override bool LocksInteractor => true;

    protected override bool ShowIndicator => base.ShowIndicator && (Group == null || Group.AcceptsInteraction);

    // Grupo ja ativado (e de uso unico) nao aceita mais ninguem
    public override bool CanInteract(PlayerInteractor interactor)
    {
        return base.CanInteract(interactor) && (Group == null || Group.AcceptsInteraction);
    }
}
