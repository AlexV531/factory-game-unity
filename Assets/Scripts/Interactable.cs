using UnityEngine;
using Unity.Netcode;

public class Interactable : NetworkBehaviour
{
    public virtual void Interact(ulong clientId)
    {
        // Override this in derived classes
        Debug.Log($"Player {clientId} interacted with {gameObject.name}");
    }
}