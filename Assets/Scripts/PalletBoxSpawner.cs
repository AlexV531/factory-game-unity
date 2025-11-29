using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PalletBoxSpawner : ObjectSpawner
{
    private int objectsInTrigger = 0;
    
    private void OnTriggerEnter(Collider other)
    {
        objectsInTrigger++;
    }
    
    private void OnTriggerExit(Collider other)
    {
        objectsInTrigger--;
        if (objectsInTrigger < 0) objectsInTrigger = 0;
    }
    
    public void SpawnObject(List<int> productCodes)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("No prefab assigned to NetworkObjectSpawner!");
            return;
        }
        
        if (!ReadyToSpawn())
        {
            Debug.LogWarning("Cannot spawn - spawn area is blocked!");
            return;
        }
        
        // Instantiate the object
        GameObject obj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        
        PalletBox palletBox = obj.GetComponent<PalletBox>();
        if (palletBox != null)
        {
            palletBox.productCodes = productCodes;
        }
        
        // Get NetworkObject component and spawn it on the network
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError("Prefab does not have a NetworkObject component!");
            Destroy(obj);
        }
    }
    
    public override bool ReadyToSpawn()
    {
        return objectsInTrigger <= 0;
    }
}
