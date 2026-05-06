using UnityEngine;

public class ScannerEnvieronment : IScannerEnvironment
{
    float checkDistanceObstacle;
    float checkHeightObstacle;
    public ScannerEnvieronment(float checkDistanceObstacle, float checkHeightObstacle)
    {
        this.checkDistanceObstacle = checkDistanceObstacle;
        this.checkHeightObstacle = checkHeightObstacle;
    }

    public bool ScanObstacleHeight(Vector3 rayPos, Vector3 rayDir, LayerMask scanLayerMask, out RaycastHit hit)
    {
        if (Physics.Raycast(rayPos, rayDir, out hit, checkHeightObstacle, scanLayerMask))
        {
            Debug.DrawRay(rayPos, rayDir * checkHeightObstacle, Color.green);
            return true;
        }
        else
        {
            Debug.DrawRay(rayPos, rayDir * checkHeightObstacle, Color.red);
            return false;
        }

    }

    public bool ScanObstacleInDirection(Vector3 rayPos, Vector3 rayDir, LayerMask scanLayerMask, out RaycastHit hit)
    {
        if (Physics.Raycast(rayPos, rayDir, out hit, checkDistanceObstacle, scanLayerMask))
        {
            Debug.DrawRay(rayPos, rayDir * checkDistanceObstacle, Color.green);
            return true;
        }
        else
        {
            Debug.DrawRay(rayPos, rayDir * checkDistanceObstacle, Color.red);
            return false;
        }
            
    }
}
