using UnityEngine;

public interface ITongueAttach
{
    void TongueAttachActive(LineRenderer lr, Vector3 startPoint, Vector3 endPoint);
    void TongueAttachDeactive(LineRenderer lr);
}
