using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GrabbableObject : NetworkBehaviour
{
    [Header("Grab Settings")]
    public float grabForceMultiplier = 50f;
    public float dampingMultiplier = 10f;
    public float maxGrabForce = 100f;
    public float dragWhenGrabbed = 5f;
    public float maxGrabDistance = 2f; // Auto-release if beyond this distance

    // Network list of grabber client IDs
    private NetworkList<ulong> grabberClientIds;

    // Dictionary to track grab points (server-side only)
    private Dictionary<ulong, Transform> grabPointsDict = new Dictionary<ulong, Transform>();

    private Rigidbody rb;
    private float originalLinearDamping;
    private float originalAngularDamping;

    void Awake()
    {
        grabberClientIds = new NetworkList<ulong>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (rb != null)
        {
            originalLinearDamping = rb.linearDamping;
            originalAngularDamping = rb.angularDamping;
        }

        // Subscribe to list changes to update damping
        grabberClientIds.OnListChanged += OnGrabberListChanged;
    }

    public override void OnNetworkDespawn()
    {
        grabberClientIds.OnListChanged -= OnGrabberListChanged;
    }

    void OnGrabberListChanged(NetworkListEvent<ulong> changeEvent)
    {
        // Update damping based on whether object is grabbed
        if (!IsServer) return;

        if (grabberClientIds.Count > 0 && changeEvent.Type == NetworkListEvent<ulong>.EventType.Add)
        {
            rb.linearDamping = dragWhenGrabbed;
            rb.angularDamping = dragWhenGrabbed;
        }
        else if (grabberClientIds.Count == 0)
        {
            rb.linearDamping = originalLinearDamping;
            rb.angularDamping = originalAngularDamping;
        }
    }

    // Called by server when a player wants to grab
    public void AddGrabber(ulong clientId, Transform grabPoint)
    {
        if (!IsServer) return;

        Debug.Log("Adding grabber");

        if (!grabberClientIds.Contains(clientId))
        {
            grabberClientIds.Add(clientId);
            grabPointsDict[clientId] = grabPoint;

            // Notify the client that grab was successful
            NotifyGrabClientRpc(clientId);
        }
    }

    // Called by server when a player releases
    public void RemoveGrabber(ulong clientId)
    {
        if (!IsServer) return;

        if (grabberClientIds.Contains(clientId))
        {
            grabberClientIds.Remove(clientId);
            grabPointsDict.Remove(clientId);

            // Notify the client that release was successful
            NotifyReleaseClientRpc(clientId);
        }
    }

    [ClientRpc]
    void NotifyGrabClientRpc(ulong clientId)
    {
        // Only notify the specific client
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        // Find the local player's grabber component
        PlayerInteracter[] grabbers = FindObjectsByType<PlayerInteracter>(FindObjectsSortMode.None);
        foreach (var grabber in grabbers)
        {
            if (grabber.IsOwner)
            {
                grabber.OnGrabConfirmed(this);
                break;
            }
        }
    }

    [ClientRpc]
    void NotifyReleaseClientRpc(ulong clientId)
    {
        // Only notify the specific client
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        // Find the local player's grabber component
        PlayerInteracter[] grabbers = FindObjectsByType<PlayerInteracter>(FindObjectsSortMode.None);
        foreach (var grabber in grabbers)
        {
            if (grabber.IsOwner)
            {
                grabber.OnReleaseConfirmed();
                break;
            }
        }
    }

    void FixedUpdate()
    {
        // Only server handles physics
        if (!IsServer) return;

        if (grabberClientIds.Count > 0)
        {
            // Average the target positions of all grabbers
            Vector3 averageTarget = Vector3.zero;
            int validGrabPoints = 0;
            List<ulong> grabbersToRemove = new List<ulong>();

            foreach (var clientId in grabberClientIds)
            {
                if (grabPointsDict.TryGetValue(clientId, out Transform grabPoint))
                {
                    if (grabPoint != null)
                    {
                        float distance = Vector3.Distance(grabPoint.position, transform.position);

                        // Check if too far - mark for removal
                        if (distance > maxGrabDistance)
                        {
                            grabbersToRemove.Add(clientId);
                        }
                        else
                        {
                            averageTarget += grabPoint.position;
                            validGrabPoints++;
                        }
                    }
                    else
                    {
                        // Grab point no longer exists, remove grabber
                        grabbersToRemove.Add(clientId);
                    }
                }
            }

            // Remove grabbers that are too far or invalid
            foreach (var clientId in grabbersToRemove)
            {
                RemoveGrabber(clientId);
            }

            if (validGrabPoints > 0)
            {
                averageTarget /= validGrabPoints;

                // Calculate direction and distance
                Vector3 direction = averageTarget - transform.position;
                float distance = direction.magnitude;

                // Spring force
                Vector3 springForce = direction.normalized * (distance * grabForceMultiplier);

                // Damping force
                Vector3 dampingForce = -rb.linearVelocity * dampingMultiplier;

                // Combine and clamp
                Vector3 totalForce = springForce + dampingForce;
                totalForce = Vector3.ClampMagnitude(totalForce, maxGrabForce);

                rb.AddForce(totalForce);

                // Dampen angular velocity
                rb.angularVelocity *= 0.95f;
            }
        }
    }
}
