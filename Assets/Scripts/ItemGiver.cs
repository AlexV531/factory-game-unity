using UnityEngine;
using Unity.Netcode;

public class ItemGiver : Interactable
{
    [Header("Item Settings")]
    public GameObject itemPrefab; // The item to give (must have NetworkObject component)
    public Transform spawnPoint; // Where to spawn the item (optional)
    public bool destroyAfterUse = false;
    public float spawnDistance = 1f; // Distance in front of player to spawn

    public override void Interact(ulong clientId)
    {
        base.Interact(clientId);

        if (!IsServer) return;

        if (itemPrefab == null)
        {
            Debug.LogError("ItemGiver: No item prefab assigned!");
            return;
        }

        // Find the player's PlayerInteracter component
        PlayerInteracter[] interacters = FindObjectsByType<PlayerInteracter>(FindObjectsSortMode.None);
        PlayerInteracter playerInteracter = null;

        foreach (var interacter in interacters)
        {
            if (interacter.OwnerClientId == clientId)
            {
                playerInteracter = interacter;
                break;
            }
        }

        if (playerInteracter == null)
        {
            Debug.LogError($"Could not find PlayerInteracter for client {clientId}");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }
        else
        {
            // Spawn at the player's grab point or in front of them
            if (playerInteracter.grabPoint != null)
            {
                spawnPosition = playerInteracter.grabPoint.position;
                spawnRotation = playerInteracter.grabPoint.rotation;
            }
            else
            {
                spawnPosition = playerInteracter.transform.position + playerInteracter.transform.forward * spawnDistance;
                spawnRotation = playerInteracter.transform.rotation;
            }
        }

        // Spawn the item
        GameObject spawnedItem = Instantiate(itemPrefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = spawnedItem.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("ItemGiver: Item prefab must have a NetworkObject component!");
            Destroy(spawnedItem);
            return;
        }

        // Spawn on network
        networkObject.Spawn();

        // Auto-grab the item for the player
        GrabbableObject grabbable = spawnedItem.GetComponent<GrabbableObject>();
        if (grabbable != null && playerInteracter.grabPoint != null)
        {
            // Directly add the grabber - the GrabbableObject will handle notifying the client
            grabbable.AddGrabber(clientId, playerInteracter.grabPoint);
        }

        // Optionally destroy this giver after use
        if (destroyAfterUse)
        {
            NetworkObject.Despawn();
            Destroy(gameObject);
        }
    }
}
