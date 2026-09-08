using UnityEngine;

public struct SurfaceSample 
{
    public Vector3 Normal;
    public float Distance;
    public string Origin;

    public SurfaceSample(Vector3 normal, float distance, string origin) 
    {
        Normal = normal;
        Distance = distance;
        Origin = origin;
    }
}
