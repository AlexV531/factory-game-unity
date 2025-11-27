using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class ButtonInteractable : Interactable
{
    [Header("Button Settings")]
    [SerializeField] private UnityEvent onButtonPressed;
    [SerializeField] private UnityEvent<ulong> onButtonPressedWithClientId;

    public override void Interact(ulong clientId)
    {
        base.Interact(clientId);

        // Invoke the event set in inspector
        onButtonPressed?.Invoke();
        onButtonPressedWithClientId?.Invoke(clientId);
    }
}