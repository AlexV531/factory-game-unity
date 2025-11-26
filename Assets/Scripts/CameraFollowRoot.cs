using Unity.Netcode;
using UnityEngine;

public class CameraFollowRoot : MonoBehaviour
{
    public Transform cameraRoot;

    public void SetTarget(Transform cameraRoot)
    {
        this.cameraRoot = cameraRoot;
    }

    void Update()
    {
        transform.position = cameraRoot.transform.position;
        transform.rotation = cameraRoot.transform.rotation;
    }
}
