using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MachineA : Machine
{
    public int[] inputProductCode;

    // References
    public MachineInput machineInput;          // Reference to the trigger input component
    public ObjectSpawner outputSpawner;        // Where output products are spawned

    // Settings
    public float processingTime = 2f;          // Time to process one input

    // Internal state
    private bool isProcessing;

    private void Update()
    {
        if (!IsServer) return;
        if (!IsOperational()) return;
        if (isProcessing) return;

        CheckInvalidInput();

        // Check IsOperational again, if the machine was turned off due to invalid input it should not consume the invalid input
        if (!IsOperational()) return;

        TryProcessNextItem();
    }

    private void CheckInvalidInput()
    {
        if (!machineInput) return;

        HashSet<GameObject> objs = machineInput.GetObjectsInside();

        foreach (GameObject obj in objs)
        {
            Product product = obj.GetComponent<Product>();

            // If product is null or code is invalid
            if (product == null || !IsValidInput(product.productCode))
            {
                Debug.Log("Invalid input detected in machine " + machineName);
                PowerOff();
                return;
            }
        }
    }

    // Helper to check if a product code is valid
    private bool IsValidInput(int code)
    {
        foreach (int validCode in inputProductCode)
        {
            if (code == validCode) return true;
        }
        return false;
    }

    private void TryProcessNextItem()
    {
        IReadOnlyCollection<GameObject> objs = machineInput.GetObjectsInside();
        if (objs.Count == 0) return;

        foreach (GameObject obj in objs)
        {
            // Found a valid input, remove it from the machineInput collection
            machineInput.RemoveObject(obj);

            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(); // networked despawn
            }
            else
            {
                Destroy(obj); // local object
            }

            // Found a valid input, process it
            StartCoroutine(ProcessProduct());
            return;
        }
    }

    private IEnumerator ProcessProduct()
    {
        isProcessing = true;

        float timer = 0f;
        while (timer < processingTime)
        {
            if (IsOperational())
            {
                timer += Time.deltaTime; // only count time when powered on
            }
            yield return null; // wait for next frame
        }

        // Produce the output
        SpawnOutputProduct();

        isProcessing = false;
    }


    private void SpawnOutputProduct()
    {
        outputSpawner.SpawnObject();
    }
}
