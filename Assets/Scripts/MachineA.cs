using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MachineA : Machine
{

    // Input and output product codes
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
        // Debug.Log("Past IsServer");
        if (!IsOperational()) return;
        Debug.Log("Past IsOperational");
        if (isProcessing) return;
        // Debug.Log("Past IsProcessing");

        CheckInvalidInput();
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
            Product product = obj.GetComponent<Product>();
            if (product == null)
            {
                InvalidProductInInput(null);
                return;
            }

            // Check if the product is valid input
            bool valid = false;
            foreach (int code in inputProductCode)
            {
                if (product.productCode == code)
                {
                    valid = true;
                    break;
                }
            }

            if (!valid)
            {
                InvalidProductInInput(product);
                return;
            }

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
            StartCoroutine(ProcessProduct(product));
            return;
        }
    }

    private IEnumerator ProcessProduct(Product product)
    {
        isProcessing = true;

        ProductInputted(product);

        // Consume it: disable or destroy as needed
        product.gameObject.SetActive(false);

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

    public void ProductInputted(Product product)
    {
        Debug.Log("Product with code " + product.productCode + " inputted into machine " + machineName);
    }

    public void InvalidProductInInput(Product product)
    {
        if (product != null)
        {
            Debug.Log("INVALID INPUT: Product with code " + product.productCode + " blocking machine " + machineName);
        }
        else
        {
            Debug.Log("INVALID INPUT: Unidentified blocking machine " + machineName);
        }

        assemblyLineManager.StopAllBelts();
    }
}
