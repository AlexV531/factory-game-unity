using Unity.Cinemachine;
using UnityEngine;

public class PlayerCameraConnector : MonoBehaviour
{
    public CinemachineCamera cinemachineCamera;
    public Transform target;

    void Start()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }

        if (cinemachineCamera != null && target != null)
        {
            cinemachineCamera.Target.TrackingTarget = target;
            cinemachineCamera.Follow = target;
            cinemachineCamera.LookAt = target;
        }
    }
}
