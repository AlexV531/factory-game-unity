using UnityEngine;
using Unity.Netcode;

public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform spawnPoint;
    
    private float timer = 0f;
    private bool isSpawning = false;

    private void Start()
    {
        // Use this object's position if no spawn point specified
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (spawnOnStart && IsServer)
        {
            StartSpawning();
        }
    }

    private void Update()
    {
        // Only the server/host should spawn objects
        if (!IsServer || !isSpawning) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnObject();
            timer = 0f;
        }
    }

    public void SpawnObject()
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

    // Public methods to control spawning
    public void StartSpawning()
    {
        Debug.Log("StartSpawning");
        if (IsServer)
        {
            Debug.Log("Starting object spawning");
            isSpawning = true;
            timer = 0f;
        }
    }

    public void StopSpawning()
    {
        if (IsServer)
        {
            isSpawning = false;
        }
    }

    public void SetSpawnInterval(float interval)
    {
        if (IsServer)
        {
            spawnInterval = Mathf.Max(0.1f, interval);
        }
    }

    public void SpawnNow()
    {
        if (IsServer)
        {
            SpawnObject();
            timer = 0f;
        }
    }
}