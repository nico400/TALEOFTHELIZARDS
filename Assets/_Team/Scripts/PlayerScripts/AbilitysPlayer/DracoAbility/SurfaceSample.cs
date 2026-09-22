using UnityEngine;

public struct SurfaceSample 
{
    public Vector3 Normal;
    public Vector3 Point;
    public float Distance;
    public string Origin;

    public SurfaceSample(Vector3 normal, Vector3 point, float distance, string origin) 
    {
        Normal = normal;
        Point = point;
        Distance = distance;
        Origin = origin;
    }
}
