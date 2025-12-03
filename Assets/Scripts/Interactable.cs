using UnityEngine;
using Unity.Netcode;

public class Interactable : NetworkBehaviour
{
    [Header("Interaction Type")]
    [SerializeField] protected bool requiresSustainedInteraction = false;

    public virtual void Interact(ulong clientId)
    {
        // Override this in derived classes for one-time interactions
        Debug.Log($"Player {clientId} interacted with {gameObject.name}");
    }

    public virtual void StartSustainedInteraction(ulong clientId)
    {
        // Override this in derived classes for continuous interactions
        Debug.Log($"Player {clientId} started sustained interaction with {gameObject.name}");
    }

    public virtual void UpdateSustainedInteraction(ulong clientId, float deltaTime)
    {
        // Override this in derived classes for per-frame updates during interaction
    }

    public virtual void EndSustainedInteraction(ulong clientId)
    {
        // Override this in derived classes when interaction ends
        Debug.Log($"Player {clientId} ended sustained interaction with {gameObject.name}");
    }

    public bool RequiresSustainedInteraction => requiresSustainedInteraction;
}
