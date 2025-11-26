using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerGrabber : NetworkBehaviour
{
    public Transform grabPoint;
    public Transform cameraRoot;
    public float grabDistance = 3f;
    public LayerMask grabbableLayer;

    private GrabbableObject grabbedObject;

    public void OnInteract(InputValue value)
    {
        // Only allow input from the local player
        if (!IsOwner) return;

        if (value.isPressed)
        {
            Debug.Log("Interact pressed");
            if (grabbedObject == null)
                TryGrab();
            else
                Release();
        }
    }

    void TryGrab()
    {
        Debug.Log("Try grab");
        RaycastHit hit;
        if (Physics.Raycast(cameraRoot.position, cameraRoot.forward, out hit, grabDistance, grabbableLayer))
        {
            Debug.Log("Grabbable object interacted with");
            GrabbableObject grobject = hit.collider.GetComponent<GrabbableObject>();
            if (grobject != null)
            {
                Debug.Log("Grabbable object component found");
                grabbedObject = grobject;

                // Request grab from server
                RequestGrabServerRpc(grobject.NetworkObjectId);
            }
        }
    }

    void Release()
    {
        Debug.Log("Release");
        if (grabbedObject == null)
            return;

        // Request release from server
        RequestReleaseServerRpc(grabbedObject.NetworkObjectId);
        grabbedObject = null;
    }

    [ServerRpc]
    void RequestGrabServerRpc(ulong objectId)
    {
        // Find the network object and add this grabber to it
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            GrabbableObject grabbable = netObj.GetComponent<GrabbableObject>();
            if (grabbable != null)
            {
                grabbable.AddGrabber(OwnerClientId, grabPoint);
            }
        }
    }

    [ServerRpc]
    void RequestReleaseServerRpc(ulong objectId)
    {
        // Find the network object and remove this grabber from it
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            GrabbableObject grabbable = netObj.GetComponent<GrabbableObject>();
            if (grabbable != null)
            {
                grabbable.RemoveGrabber(OwnerClientId);
            }
        }
    }

    // Called by the grabbable object when successfully grabbed
    public void OnGrabConfirmed(GrabbableObject obj)
    {
        grabbedObject = obj;
    }

    // Called by the grabbable object when released
    public void OnReleaseConfirmed()
    {
        grabbedObject = null;
    }
}
