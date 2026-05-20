using UnityEngine;

public class TongueAttach : ITongueAttach
{
    public void TongueAttachActive(LineRenderer lr, Vector3 startPoint, Vector3 endPoint)
    {
        Debug.Log("Tongue attached from " + startPoint + " to " + endPoint);
        lr.SetPosition(0, startPoint);
        lr.SetPosition(1, endPoint);
    }

    public void TongueAttachDeactive(LineRenderer lr)
    {
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.zero);
    }
}
