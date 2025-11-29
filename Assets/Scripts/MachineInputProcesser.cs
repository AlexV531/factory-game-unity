using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class MachineInputProcessor : NetworkBehaviour
{
    public MachineInput machineInput;
    public ProcessingMachine machine;

    public int[] inputProductCode;

    void Update()
    {
        if (!IsServer) return;
        if (!machine.IsOperational()) return;
        if (machine.IsProcessing()) return;

        CheckInvalidInput();

        // Check IsOperational again, if the machine was turned off due to invalid input it should not consume the invalid input
        if (!machine.IsOperational()) return;

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
                Debug.Log("Invalid input detected in machine " + machine.machineName);
                machine.PowerOff();
                return;
            }
        }
    }

    private bool IsValidInput(int code)
    {
        if (inputProductCode.Length <= 0) // If no valid input codes everything is valid
        {
            return true;
        }

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

        // Get the first object only
        GameObject obj = null;
        foreach (GameObject o in objs)
        {
            obj = o;
            break; // Only process one item
        }

        if (obj == null) return;

        // Process the object
        machine.InputItem(obj.GetComponent<Product>().productCode);

        // Remove the object
        machineInput.RemoveObject(obj);
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        else
        {
            Destroy(obj);
        }
    }
}