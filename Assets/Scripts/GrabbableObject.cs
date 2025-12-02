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
    public Transform grabTransform;

    // Network list of grabber client IDs
    private NetworkList<ulong> grabberClientIds;

    // Dictionary to track grab points on server
    private Dictionary<ulong, Transform> grabPointsDict = new Dictionary<ulong, Transform>();

    // Track the original owner
    private ulong originalOwnerId;
    private bool ownershipTransferred = false;

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

        if (IsServer)
        {
            originalOwnerId = OwnerClientId;
        }

        if (rb != null)
        {
            originalLinearDamping = rb.linearDamping;
            originalAngularDamping = rb.angularDamping;
            
            // Only owner simulates physics
            rb.isKinematic = !IsOwner;
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
        // Only owner updates damping
        if (!IsOwner || rb == null) return;
        
        // Update damping based on whether object is grabbed
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

            // Transfer ownership to first grabber
            if (grabberClientIds.Count == 1 && !ownershipTransferred)
            {
                NetworkObject.ChangeOwnership(clientId);
                ownershipTransferred = true;
            }

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

            // If this was the owner and there are other grabbers, transfer to next grabber
            if (clientId == OwnerClientId && grabberClientIds.Count > 0)
            {
                NetworkObject.ChangeOwnership(grabberClientIds[0]);
            }
            // If no more grabbers, return to original owner
            else if (grabberClientIds.Count == 0)
            {
                NetworkObject.ChangeOwnership(originalOwnerId);
                ownershipTransferred = false;
            }

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
        // Server checks distance and removes grabbers if needed
        if (IsServer)
        {
            CheckGrabberDistances();
        }

        // Owner controls physics
        if (IsOwner && grabberClientIds.Count > 0)
        {
            ApplyGrabForces();
        }
    }

    void CheckGrabberDistances()
    {
        if (grabberClientIds.Count == 0) return;

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
    }

    void ApplyGrabForces()
    {
        // Find all PlayerInteracter components to get grab points
        PlayerInteracter[] interacters = FindObjectsByType<PlayerInteracter>(FindObjectsSortMode.None);

        Vector3 averageTarget = Vector3.zero;
        int validGrabPoints = 0;

        foreach (var clientId in grabberClientIds)
        {
            // Find the PlayerInteracter for this client
            foreach (var interacter in interacters)
            {
                if (interacter.OwnerClientId == clientId && interacter.grabPoint != null)
                {
                    averageTarget += interacter.grabPoint.position;
                    validGrabPoints++;
                    break;
                }
            }
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

            if (grabTransform != null)
                rb.AddForceAtPosition(totalForce, grabTransform.position);
            else
                rb.AddForce(totalForce);

            // Dampen angular velocity
            rb.angularVelocity *= 0.95f;
        }
    }
}
