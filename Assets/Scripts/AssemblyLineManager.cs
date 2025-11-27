using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class AssemblyLineManager : NetworkBehaviour
{
    [SerializeField] private float rampUpTime = 3f; // Time to reach full speed
    
    private ConveyorBelt[] conveyorBelts;
    private float[] targetSpeeds; // Store each belt's original speed
    private bool isRampingUp = false;
    private bool beltsRunning = true;

    private void Start()
    {
        // Find all conveyor belts in the scene
        conveyorBelts = FindObjectsByType<ConveyorBelt>(FindObjectsSortMode.None);
        targetSpeeds = new float[conveyorBelts.Length];
        
        // Store their initial speeds
        for (int i = 0; i < conveyorBelts.Length; i++)
        {
            targetSpeeds[i] = conveyorBelts[i].GetCurrentSpeed();
        }
        
        Debug.Log($"AssemblyLineManager found {conveyorBelts.Length} conveyor belts");
    }

    public void ToggleBelts()
    {
        Debug.Log("Toggling belts");

        if (beltsRunning)
        {
            StopAllBelts();
        }
        else
        {
            StartAllBelts();
        }
    }

    // Stop all belts immediately
    public void StopAllBelts()
    {
        if (!IsServer) return;

        StopAllCoroutines();
        isRampingUp = false;
        
        foreach (ConveyorBelt belt in conveyorBelts)
        {
            if (belt != null)
            {
                belt.StopBelt();
            }
        }

        beltsRunning = false;
        
        Debug.Log("All conveyor belts stopped");
    }

    // Start all belts with gradual ramp up
    public void StartAllBelts()
    {
        if (!IsServer) return;

        if (!isRampingUp)
        {
            StartCoroutine(RampUpBelts());
        }

        beltsRunning = true;

        Debug.Log("All conveyor belts stopped");
    }

    private IEnumerator RampUpBelts()
    {
        isRampingUp = true;
        
        // Start all belts at speed 0
        for (int i = 0; i < conveyorBelts.Length; i++)
        {
            if (conveyorBelts[i] != null)
            {
                conveyorBelts[i].SetSpeed(0f);
                conveyorBelts[i].StartBelt();
            }
        }
        
        float elapsed = 0f;
        
        // Gradually increase speed over rampUpTime
        while (elapsed < rampUpTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rampUpTime;
            
            // Use smoothstep for smoother acceleration
            float smoothT = t * t * (3f - 2f * t);
            
            for (int i = 0; i < conveyorBelts.Length; i++)
            {
                if (conveyorBelts[i] != null)
                {
                    float currentSpeed = Mathf.Lerp(0f, targetSpeeds[i], smoothT);
                    conveyorBelts[i].SetSpeed(currentSpeed);
                }
            }
            
            yield return null;
        }
        
        // Ensure we hit exact target speeds
        for (int i = 0; i < conveyorBelts.Length; i++)
        {
            if (conveyorBelts[i] != null)
            {
                conveyorBelts[i].SetSpeed(targetSpeeds[i]);
            }
        }
        
        isRampingUp = false;
        Debug.Log("All conveyor belts at full speed");
    }

    // RPC versions for clients to call
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StopAllBeltsServerRpc()
    {
        StopAllBelts();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StartAllBeltsServerRpc()
    {
        StartAllBelts();
    }

    // Utility method to refresh the list of belts (if belts are added/removed dynamically)
    public void RefreshBeltList()
    {
        if (!IsServer) return;

        conveyorBelts = FindObjectsByType<ConveyorBelt>(FindObjectsSortMode.None);
        targetSpeeds = new float[conveyorBelts.Length];
        
        for (int i = 0; i < conveyorBelts.Length; i++)
        {
            targetSpeeds[i] = conveyorBelts[i].GetCurrentSpeed();
        }
    }
}