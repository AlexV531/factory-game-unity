using Unity.Netcode;
using UnityEngine;

public class ElevatorPlatform : NetworkBehaviour
{
    public Elevator elevator;

    private void OnTriggerStay(Collider other)
    {
        if (elevator != null)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                elevator.HandleTriggerCollision(rb);
            }
        }
    }
}
