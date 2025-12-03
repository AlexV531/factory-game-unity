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

    private Interactable currentSustainedInteractable;
    private ulong currentInteractableNetworkId;
    private string currentInteractableChildPath;
    private bool isHoldingInteract = false;

    public void OnInteract(InputValue value)
    {
        // Only allow input from the local player
        if (!IsOwner) return;

        if (value.isPressed)
        {
            Debug.Log("Interact pressed");
            isHoldingInteract = true;

            if (grabbedObject == null)
                TryInteract();
            else
                Release();
        }
        else
        {
            // Button released
            isHoldingInteract = false;
            Debug.Log("Interact released");
            if (currentSustainedInteractable != null)
            {
                EndSustainedInteraction();
            }
        }
    }

    private void Update()
    {
        // Update sustained interaction every frame
        if (isHoldingInteract && currentSustainedInteractable != null && IsOwner)
        {
            RequestUpdateSustainedInteractionServerRpc(
                currentInteractableNetworkId,
                currentInteractableChildPath,
                Time.deltaTime
            );
        }
    }

    void TryInteract()
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

                        // Check if this is a sustained interactable
                        if (interactable.RequiresSustainedInteraction)
                        {
                            currentSustainedInteractable = interactable;
                            currentInteractableNetworkId = netObj.NetworkObjectId;
                            currentInteractableChildPath = childPath;
                            RequestStartSustainedInteractionServerRpc(netObj.NetworkObjectId, childPath);
                        }
                        else
                        {
                            RequestInteractServerRpc(netObj.NetworkObjectId, childPath);
                        }
                    }
                }
            }
        }
    }

    void EndSustainedInteraction()
    {
        if (currentSustainedInteractable != null)
        {
            Debug.Log("Player ending sustained interaction.");
            RequestEndSustainedInteractionServerRpc(
                currentInteractableNetworkId,
                currentInteractableChildPath
            );

            currentSustainedInteractable = null;
            currentInteractableNetworkId = 0;
            currentInteractableChildPath = null;
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

    [Rpc(SendTo.Server)]
    void RequestStartSustainedInteractionServerRpc(ulong objectId, string childPath)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            Transform targetTransform = FindChildByPath(netObj.transform, childPath);
            if (targetTransform != null)
            {
                Interactable interactable = targetTransform.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.StartSustainedInteraction(OwnerClientId);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    void RequestUpdateSustainedInteractionServerRpc(ulong objectId, string childPath, float deltaTime)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            Transform targetTransform = FindChildByPath(netObj.transform, childPath);
            if (targetTransform != null)
            {
                Interactable interactable = targetTransform.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.UpdateSustainedInteraction(OwnerClientId, deltaTime);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    void RequestEndSustainedInteractionServerRpc(ulong objectId, string childPath)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject netObj))
        {
            Transform targetTransform = FindChildByPath(netObj.transform, childPath);
            if (targetTransform != null)
            {
                Interactable interactable = targetTransform.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.EndSustainedInteraction(OwnerClientId);
                }
            }
        }
    }

    public void OnGrabConfirmed(GrabbableObject obj)
    {
        grabbedObject = obj;
    }

    public void OnReleaseConfirmed()
    {
        grabbedObject = null;
    }
}
