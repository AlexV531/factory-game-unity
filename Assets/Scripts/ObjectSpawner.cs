using UnityEngine;
using Unity.Netcode;

public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] protected GameObject prefabToSpawn;
    [SerializeField] protected Transform spawnPoint;

    private bool readyToSpawn = true;

    private void Start()
    {
        // Use this object's position if no spawn point specified
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    public virtual void SpawnObject()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("No prefab assigned to NetworkObjectSpawner!");
            return;
        }

        // Instantiate the object
        GameObject obj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        
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

    public virtual bool ReadyToSpawn()
    {
        return readyToSpawn;
    }
}