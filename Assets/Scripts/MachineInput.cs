using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// public class MachineInput : NetworkBehaviour
// {
//     public Machine machine;

//     void OnTriggerStay(Collider other)
//     {
//         if (!IsServer) return;

//         if (!machine.IsPoweredOn()) return;

//         Product inputtedProduct = other.GetComponent<Product>();
//         if (inputtedProduct != null)
//         {
//             foreach (int validProductCode in machine.inputProductCode)
//             {
//                 if (inputtedProduct.productCode == validProductCode)
//                 {
//                     machine.ProductInputted(inputtedProduct);
//                     return;
//                 }
//             }
//         }
//         machine.InvalidProductInInput(inputtedProduct);
//     }
// }

public class MachineInput : NetworkBehaviour
{
    private HashSet<GameObject> objectsInside = new HashSet<GameObject>();

    public HashSet<GameObject> GetObjectsInside()
    {
        return objectsInside; // direct access as requested
    }

    public void RemoveObject(GameObject obj)
    {
        if (!IsServer) return; // ensure server-only modification
        objectsInside.Remove(obj);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        objectsInside.Add(other.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        objectsInside.Remove(other.gameObject);
    }
}
