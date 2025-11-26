using Unity.Netcode;
using UnityEngine;
// using Cinemachine;

public class PlayerCameraConnector : NetworkBehaviour
{
    public Transform cameraRoot;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // var vcam = GameObject.FindAnyObjectByType<CinemachineCamera>();
        // vcam.TrackingTarget.Target = cameraRoot;
    }
}