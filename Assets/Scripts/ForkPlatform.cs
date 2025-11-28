using UnityEngine;
using System.Collections.Generic;

public class ForkPlatform : MonoBehaviour
{
    private HashSet<Rigidbody> objectsOnForks = new HashSet<Rigidbody>();
    private Vector3 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            objectsOnForks.Add(rb);

            // Freeze tipping rotation
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            objectsOnForks.Add(rb);

            // Ensure constraints remain applied
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            objectsOnForks.Remove(rb);

            // Restore full rotation now that it's off the forks
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    private void FixedUpdate()
    {
        Vector3 movement = transform.position - lastPosition;

        // Only vertical following
        movement = new Vector3(0f, movement.y, 0f);

        if (movement.sqrMagnitude > 0f)
        {
            foreach (Rigidbody rb in objectsOnForks)
            {
                if (rb != null)
                {
                    rb.MovePosition(rb.position + movement);
                }
            }
        }

        lastPosition = transform.position;
    }
}
