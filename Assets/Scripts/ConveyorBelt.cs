using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ConveyorBelt : NetworkBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private Vector3 direction = Vector3.forward;
    [SerializeField] private bool isRunning = true;

    private List<Rigidbody> objectsOnBelt = new List<Rigidbody>();

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && !objectsOnBelt.Contains(rb))
        {
            objectsOnBelt.Add(rb);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && objectsOnBelt.Contains(rb))
        {
            objectsOnBelt.Remove(rb);
        }
    }

    private void FixedUpdate()
    {
        // Only the server should move objects
        if (!IsServer || !isRunning) return;

        Vector3 movement = transform.TransformDirection(direction.normalized) * speed * Time.fixedDeltaTime;

        foreach (Rigidbody rb in objectsOnBelt)
        {
            if (rb != null)
            {
                rb.MovePosition(rb.position + movement);
            }
        }
    }

    // Public methods to control the belt
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetSpeedServerRpc(float newSpeed)
    {
        speed = newSpeed;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StartBeltServerRpc()
    {
        isRunning = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StopBeltServerRpc()
    {
        isRunning = false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ToggleBeltServerRpc()
    {
        isRunning = !isRunning;
    }
    
    // Local control methods (server only)
    public void SetSpeed(float newSpeed)
    {
        if (IsServer)
        {
            speed = newSpeed;
        }
    }

    public void StartBelt()
    {
        if (IsServer)
        {
            isRunning = true;
        }
    }

    public void StopBelt()
    {
        if (IsServer)
        {
            isRunning = false;
        }
    }

    public void ToggleBelt()
    {
        if (IsServer)
        {
            isRunning = !isRunning;
        }
    }

    public float GetCurrentSpeed()
    {
        return speed;
    }
}
