using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Elevator : NetworkBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float speed = 2f;
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Vector3 lastPosition;
    private Coroutine moveRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        targetPosition = rb.position;
        lastPosition = rb.position;
    }

    public void ToggleElevator()
    {
        if (IsServer)
        {
            StartMovement(targetPosition == startPoint.position ? endPoint.position : startPoint.position);
        }
        else
        {
            SubmitToggleServerRpc();
        }
    }

    public void MoveToStart()
    {
        if (IsServer)
        {
            StartMovement(startPoint.position);
        }
        else
        {
            SubmitMoveToStartServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitToggleServerRpc()
    {
        ToggleElevator();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitMoveToStartServerRpc()
    {
        MoveToStart();
    }

    private void StartMovement(Vector3 target)
    {
        targetPosition = target;
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveElevator(target));
    }

    private IEnumerator MoveElevator(Vector3 target)
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        while (Vector3.Distance(rb.position, target) > 0.01f)
        {
            lastPosition = rb.position;
            rb.MovePosition(Vector3.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(target);
        rb.position = target;
        rb.linearVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        targetPosition = target;
        moveRoutine = null;
    }

    public void HandleTriggerCollision(Rigidbody otherRb)
    {
        if (moveRoutine != null)
        {
            Vector3 platformMovement = rb.position - lastPosition;
            otherRb.MovePosition(otherRb.position + platformMovement);
        }
    }
}
