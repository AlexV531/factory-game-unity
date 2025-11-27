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

    private Machine[] machines; // Track all machines

    private void Start()
    {
        // Find all conveyor belts in the scene
        conveyorBelts = FindObjectsByType<ConveyorBelt>(FindObjectsSortMode.None);
        targetSpeeds = new float[conveyorBelts.Length];

        for (int i = 0; i < conveyorBelts.Length; i++)
        {
            targetSpeeds[i] = conveyorBelts[i].GetCurrentSpeed();
        }

        // Find all machines in the scene
        machines = FindObjectsByType<Machine>(FindObjectsSortMode.None);

        Debug.Log($"AssemblyLineManager found {conveyorBelts.Length} belts and {machines.Length} machines");
    }

    public void ToggleBelts()
    {
        if (beltsRunning)
        {
            StopAllBelts();
        }
        else
        {
            StartAllBelts();
        }
    }

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

    public void StartAllBelts()
    {
        if (!IsServer) return;

        // Check all machines are powered on before starting
        if (AnyMachinePoweredOff())
        {
            Debug.LogWarning("Cannot start assembly line: some machines are powered off");
            return;
        }

        if (!isRampingUp)
        {
            StartCoroutine(RampUpBelts());
        }

        beltsRunning = true;
        Debug.Log("Assembly line starting...");
    }

    private bool AnyMachinePoweredOff()
    {
        foreach (Machine machine in machines)
        {
            if (machine != null && !machine.IsPoweredOn())
            {
                return true;
            }
        }
        return false;
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

        while (elapsed < rampUpTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rampUpTime;
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

    // Optional: refresh lists dynamically
    public void RefreshBeltAndMachineList()
    {
        if (!IsServer) return;

        conveyorBelts = FindObjectsByType<ConveyorBelt>(FindObjectsSortMode.None);
        targetSpeeds = new float[conveyorBelts.Length];
        for (int i = 0; i < conveyorBelts.Length; i++)
        {
            targetSpeeds[i] = conveyorBelts[i].GetCurrentSpeed();
        }

        machines = FindObjectsByType<Machine>(FindObjectsSortMode.None);
    }

    public bool IsLineRunning()
    {
        return beltsRunning;
    }
}
