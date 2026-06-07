using Unity.Cinemachine;
using UnityEngine;
public class KeyboardInputStrategy : IPlayerInputStrategy, IPlayerInputAbilitysStrategyKeyboard
{
    public void ExecuteAbility(IGetPositionTongueFinal getPosFinalTogueComp, Vector3 posFinalTogue, Transform camTransform, bool isAim, bool isAttach, CinemachineCameraOffset cinemachineOffset, ITongueAttach togueAttachComp, Transform startToguePos, LineRenderer lr) 
    {
        if (getPosFinalTogueComp != null && posFinalTogue == Vector3.zero)
            posFinalTogue = getPosFinalTogueComp.PositionFinalTongue(camTransform.position + (camTransform.forward * 2), camTransform.forward);

        // Lógica para ataque com língua
        if (isAim)
        {
            cinemachineOffset.Offset = new Vector3(1f, 0f, 0.75f);

            if (isAttach && posFinalTogue != Vector3.zero)
                if (togueAttachComp != null)
                    togueAttachComp.TongueAttachActive(lr, startToguePos.position, posFinalTogue);

        }
        else
            cinemachineOffset.Offset = Vector3.zero;

        if (!isAttach || !isAim)
        {
            togueAttachComp.TongueAttachDeactive(lr);
            posFinalTogue = Vector3.zero;
        }
    }
    public Vector3 GetDirectionMove(Transform cameraTransform, InputReader reader)
    {
        Vector3 dir = cameraTransform.forward * reader.DirectionBoard.y +
            cameraTransform.right * reader.DirectionBoard.x;
        dir.y = 0f;
        dir = dir.normalized;
        
        return dir;
    }
    public void InitComponents(IGetPositionTongueFinal getPosFinalTogueComp, ITongueAttach togueAttachComp, float tongueRange, float radius, string tag, LayerMask layerAttach, LayerMask layerObstacle) 
    {
        getPosFinalTogueComp = new GetPositionTongueFinal(tongueRange, radius, tag, layerAttach, layerObstacle);
        togueAttachComp = new TongueAttach();
    }
}
