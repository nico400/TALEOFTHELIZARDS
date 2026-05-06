using UnityEngine;

public interface IScannerEnvironment
{
    bool ScanObstacleInDirection(Vector3 rayPos, Vector3 rayDir, LayerMask scanLayerMask, out RaycastHit hit);
    bool ScanObstacleHeight(Vector3 rayPos, Vector3 rayDir, LayerMask scanLayerMask, out RaycastHit hit);
}
