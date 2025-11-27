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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        targetPosition = rb.position;
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

    [ServerRpc]
    private void SubmitToggleServerRpc(ServerRpcParams rpcParams = default)
    {
        ToggleElevator();
    }

    [ServerRpc]
    private void SubmitMoveToStartServerRpc(ServerRpcParams rpcParams = default)
    {
        MoveToStart();
    }

    private void StartMovement(Vector3 target)
    {
        targetPosition = target;
        StopAllCoroutines();
        StartCoroutine(MoveElevator(target));
    }

    private IEnumerator MoveElevator(Vector3 target)
    {
        while (Vector3.Distance(rb.position, target) > 0.01f)
        {
            rb.MovePosition(Vector3.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(target);
    }
}
