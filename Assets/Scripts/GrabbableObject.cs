using UnityEngine;
using System.Collections.Generic;

public class GrabbableObject : MonoBehaviour
{
    [Header("Grab Settings")]
    public float grabForceMultiplier = 50f;
    public float dampingMultiplier = 10f; // Reduces oscillation
    public float maxGrabForce = 100f; // Prevents extreme forces
    public float dragWhenGrabbed = 5f; // Air resistance when grabbed

    public List<PlayerGrabber> currentGrabbers = new List<PlayerGrabber>();

    private Rigidbody rb;
    private float originalLinearDamping;
    private float originalAngularDamping;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalLinearDamping = rb.linearDamping;
        originalAngularDamping = rb.angularDamping;
    }

    public void AddGrabber(PlayerGrabber grabber)
    {
        if (!currentGrabbers.Contains(grabber))
        {
            currentGrabbers.Add(grabber);

            // Increase damping when grabbed to reduce oscillation
            if (currentGrabbers.Count == 1)
            {
                rb.linearDamping = dragWhenGrabbed;
                rb.angularDamping = dragWhenGrabbed;
            }
        }
    }

    public void RemoveGrabber(PlayerGrabber grabber)
    {
        currentGrabbers.Remove(grabber);

        // Restore original damping when released
        if (currentGrabbers.Count == 0)
        {
            rb.linearDamping = originalLinearDamping;
            rb.angularDamping = originalAngularDamping;
        }
    }

    void FixedUpdate()
    {
        if (currentGrabbers.Count > 0)
        {
            // Average the target positions of all grabbers
            Vector3 averageTarget = Vector3.zero;
            foreach (var grabber in currentGrabbers)
            {
                averageTarget += grabber.grabPoint.position;
            }
            averageTarget /= currentGrabbers.Count;

            // Calculate direction and distance
            Vector3 direction = averageTarget - transform.position;
            float distance = direction.magnitude;

            // Spring force (proportional to distance)
            Vector3 springForce = direction.normalized * (distance * grabForceMultiplier);

            // Damping force (opposes velocity, reduces oscillation)
            Vector3 dampingForce = -rb.linearVelocity * dampingMultiplier;

            // Combine forces and clamp
            Vector3 totalForce = springForce + dampingForce;
            totalForce = Vector3.ClampMagnitude(totalForce, maxGrabForce);

            rb.AddForce(totalForce);

            // Optional: Dampen angular velocity to reduce spinning
            rb.angularVelocity *= 0.95f;
        }
    }
}
