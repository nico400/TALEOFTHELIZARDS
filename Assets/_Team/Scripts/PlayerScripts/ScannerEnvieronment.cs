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
            Debug.DrawRay(rayPos, rayDir * checkHeightObstacle, Color.blue);
            // Calculate the distance from the ray position to the hit point
            // Calcular a distância da posição do raio até o ponto de impacto
            float distanceToHitPointToObstacle = Vector3.Distance(rayPos, hit.point);
            if (distanceToHitPointToObstacle <= 0.5f)
            {
                // Calculate the angle between the hit normal and the up vector
                // Calcular o ângulo entre a normal do impacto e o vetor up
                float angleNormalPointToObstacle = Vector3.Angle(hit.normal, Vector3.up);

                // If the angle is less than or equal to 15 degrees, consider it a valid obstacle
                // Se o ângulo for menor ou igual a 15 graus, considere como um obstáculo válido
                if (angleNormalPointToObstacle <= 15f)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        else
            return false;

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
