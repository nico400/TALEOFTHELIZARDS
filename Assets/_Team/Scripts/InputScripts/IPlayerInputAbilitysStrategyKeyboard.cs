using Unity.Cinemachine;
using UnityEngine;

public interface IPlayerInputAbilitysStrategyKeyboard 
{
    void ExecuteAbility(IGetPositionTongueFinal getPosFinalTogueComp, Vector3 posFinalTogue, Transform camTransform, bool isAim, bool isAttach, CinemachineCameraOffset cinemachineOffset, ITongueAttach togueAttachComp, Transform startToguePos, LineRenderer lr);
    void InitComponents(IGetPositionTongueFinal getPosFinalTogueComp, ITongueAttach togueAttachComp, float tongueRange, float radius, string tag, LayerMask layerAttach, LayerMask layerObstacle);
}
