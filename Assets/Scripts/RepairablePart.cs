using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class RepairablePart : Interactable
{
    [Header("Part Settings")]
    [SerializeField] private string partName = "Machine Part";
    [SerializeField] private UnityEngine.Material brokenMaterial;
    [SerializeField] private UnityEngine.Material repairedMaterial;

    private Renderer partRenderer;
    private float repairTimeRequired;
    private float currentRepairProgress = 0f;
    private bool isRepaired = false;
    private Dictionary<ulong, bool> playersRepairing = new Dictionary<ulong, bool>();

    public delegate void PartRepairedDelegate(RepairablePart part);
    public event PartRepairedDelegate OnPartRepaired;

    public bool IsRepaired => isRepaired;
    public float RepairProgress => currentRepairProgress / repairTimeRequired;

    private void Awake()
    {
        requiresSustainedInteraction = true;
        partRenderer = GetComponent<Renderer>();
    }

    public void Initialize(float repairTime)
    {
        repairTimeRequired = repairTime;
        currentRepairProgress = 0f;
        isRepaired = false;
        playersRepairing.Clear();

        if (partRenderer != null && brokenMaterial != null)
        {
            partRenderer.material = brokenMaterial;
        }
    }

    private void Update()
    {
        if (isRepaired)
            return;

        // Check if any player is currently repairing
        bool anyoneRepairing = false;
        foreach (var kvp in playersRepairing)
        {
            if (kvp.Value)
            {
                anyoneRepairing = true;
                break;
            }
        }

        if (anyoneRepairing)
        {
            currentRepairProgress += Time.deltaTime;

            if (currentRepairProgress >= repairTimeRequired)
            {
                CompletePart();
            }
        }
    }

    // public override void Interact(ulong clientId)
    // {
    //     base.Interact(clientId);

    //     if (isRepaired)
    //     {
    //         Debug.Log($"{partName} is already repaired!");
    //         return;
    //     }

    //     // Toggle repair state for this player
    //     if (!playersRepairing.ContainsKey(clientId))
    //     {
    //         playersRepairing[clientId] = false;
    //     }

    //     if (playersRepairing[clientId])
    //     {
    //         StopRepair(clientId);
    //     }
    //     else
    //     {
    //         StartRepair(clientId);
    //     }
    // }

    public override void StartSustainedInteraction(ulong clientId)
    {
        StartRepair(clientId);
    }

    public override void EndSustainedInteraction(ulong clientId)
    {
        StopRepair(clientId);
    }

    public void StartRepair(ulong clientId)
    {
        if (isRepaired)
            return;

        playersRepairing[clientId] = true;
        Debug.Log($"Player {clientId} started repairing {partName}...");

        if (IsServer)
        {
            UpdateRepairStateClientRpc(clientId, true);
        }
    }

    public void StopRepair(ulong clientId)
    {
        if (isRepaired)
            return;

        if (playersRepairing.ContainsKey(clientId))
        {
            playersRepairing[clientId] = false;
        }

        Debug.Log($"Player {clientId} stopped repairing {partName}. Progress: {RepairProgress * 100:F0}%");

        if (IsServer)
        {
            UpdateRepairStateClientRpc(clientId, false);
        }
    }

    [ClientRpc]
    private void UpdateRepairStateClientRpc(ulong clientId, bool isRepairing)
    {
        if (IsServer)
            return;

        playersRepairing[clientId] = isRepairing;
    }

    private void CompletePart()
    {
        isRepaired = true;
        playersRepairing.Clear();
        currentRepairProgress = repairTimeRequired;

        if (partRenderer != null && repairedMaterial != null)
        {
            partRenderer.material = repairedMaterial;
        }

        Debug.Log($"{partName} repaired!");

        if (IsServer)
        {
            MarkPartRepairedClientRpc();
        }

        OnPartRepaired?.Invoke(this);
    }

    [ClientRpc]
    private void MarkPartRepairedClientRpc()
    {
        if (IsServer)
            return;

        isRepaired = true;
        currentRepairProgress = repairTimeRequired;

        if (partRenderer != null && repairedMaterial != null)
        {
            partRenderer.material = repairedMaterial;
        }
    }

    public void ResetPart()
    {
        currentRepairProgress = 0f;
        isRepaired = false;
        playersRepairing.Clear();

        if (partRenderer != null && brokenMaterial != null)
        {
            partRenderer.material = brokenMaterial;
        }

        if (IsServer)
        {
            ResetPartClientRpc();
        }
    }

    [ClientRpc]
    private void ResetPartClientRpc()
    {
        if (IsServer)
            return;

        currentRepairProgress = 0f;
        isRepaired = false;
        playersRepairing.Clear();

        if (partRenderer != null && brokenMaterial != null)
        {
            partRenderer.material = brokenMaterial;
        }
    }
}
