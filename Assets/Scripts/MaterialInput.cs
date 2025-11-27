using Unity.Netcode;
using UnityEngine;

public class MaterialInput : NetworkBehaviour
{
    [Header("Target Machine")]
    public MaterialRequiredMachine targetMachine;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Check if the object is a GrabbableObject
        GrabbableObject grabbable = other.GetComponent<GrabbableObject>();
        if (grabbable == null) return;

        // Check if it has a Material component and add it to the machine
        Material material = other.GetComponent<Material>();
        if (material != null && targetMachine != null)
        {
            targetMachine.AddMaterial(material.materialValue);
        }

        // Despawn networked objects, otherwise destroy locally
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        else
        {
            Destroy(other.gameObject);
        }
    }
}