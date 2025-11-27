using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
