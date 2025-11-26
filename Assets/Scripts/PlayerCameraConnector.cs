using Unity.Cinemachine;
using UnityEngine;

public class PlayerCameraConnector : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;
    public CameraFollowRoot normalCamera;
    public Transform target;

    void Start()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        if (normalCamera == null)
        {
            normalCamera = FindAnyObjectByType<CameraFollowRoot>();
        }

        if (cinemachineCamera != null && target != null)
        {
            cinemachineCamera.Target.TrackingTarget = target;
            cinemachineCamera.Follow = target;
            cinemachineCamera.LookAt = target;
        }
        else if (normalCamera != null && target != null)
        {
            normalCamera.SetTarget(target);
        }
    }
}
