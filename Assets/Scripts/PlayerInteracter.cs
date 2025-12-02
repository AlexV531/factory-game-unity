using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInteracter : NetworkBehaviour
{
    public Transform grabPoint;
    public Transform cameraRoot;
    public float grabDistance = 3f;
    public LayerMask interactableLayer;
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
        RaycastHit hit;
        if (Physics.Raycast(cameraRoot.position, cameraRoot.forward, out hit, grabDistance, interactableLayer))
        {
            Debug.Log("Object interacted with");
            
            // First check for GrabbableObject
            GrabbableObject grobject = hit.collider.GetComponent<GrabbableObject>();
            if (grobject != null)
            {
                Debug.Log("Grabbable object component found");
                grabbedObject = grobject;
                
                // Find the parent NetworkObject
                NetworkObject netObj = grobject.GetComponentInParent<NetworkObject>();
                if (netObj != null)
                {
                    Debug.Log("Grabbable object Network object component found");
                    // Get the path from the NetworkObject to this specific child
                    string childPath = GetRelativePath(netObj.transform, grobject.transform);
                    RequestGrabServerRpc(netObj.NetworkObjectId, childPath);
                }
            }
            else
            {
                // If no GrabbableObject, check for Interactable
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    Debug.Log("Interactable component found");
                    NetworkObject netObj = interactable.GetComponentInParent<NetworkObject>();
                    if (netObj != null)
                    {
                        string childPath = GetRelativePath(netObj.transform, interactable.transform);
                        RequestInteractServerRpc(netObj.NetworkObjectId, childPath);
                    }
                }
            }
        }
    }

    void Release()
    {
        Debug.Log("Release");
        if (grabbedObject == null)
            return;
        
        NetworkObject netObj = grabbedObject.GetComponentInParent<NetworkObject>();
        if (netObj != null)
        {
            string childPath = GetRelativePath(netObj.transform, grabbedObject.transform);
            RequestReleaseServerRpc(netObj.NetworkObjectId, childPath);
        }
        grabbedObject = null;
    }

    // Helper method to get the relative path from parent to child
    string GetRelativePath(Transform parent, Transform child)
    {
        if (parent == child)
            return "";
        
        string path = child.name;
        Transform current = child.parent;
        
        while (current != null && current != parent)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }

    // Helper method to find a child by path
    Transform FindChildByPath(Transform parent, string path)
    {
        if (string.IsNullOrEmpty(path))
            return parent;
        
        return parent.Find(path);
    }

    [Rpc(SendTo.Server)]
    void RequestGrabServerRpc(ulong objectId, string childPath)
    {
        Debug.Log("Grab server rpc called");
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            Transform targetTransform = FindChildByPath(netObj.transform, childPath);
            if (targetTransform != null)
            {
                Debug.Log("Target transform found");
                GrabbableObject grabbable = targetTransform.GetComponent<GrabbableObject>();
                if (grabbable != null)
                {
                    Debug.Log("Grabbable object component found by server");
                    grabbable.AddGrabber(OwnerClientId, grabPoint);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    void RequestReleaseServerRpc(ulong objectId, string childPath)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            Transform targetTransform = FindChildByPath(netObj.transform, childPath);
            if (targetTransform != null)
            {
                GrabbableObject grabbable = targetTransform.GetComponent<GrabbableObject>();
                if (grabbable != null)
                {
                    grabbable.RemoveGrabber(OwnerClientId);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    void RequestInteractServerRpc(ulong objectId, string childPath)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            Transform targetTransform = FindChildByPath(netObj.transform, childPath);
            if (targetTransform != null)
            {
                Interactable interactable = targetTransform.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.Interact(OwnerClientId);
                }
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
