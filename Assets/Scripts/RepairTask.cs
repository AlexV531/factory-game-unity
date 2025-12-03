using UnityEngine;
using System.Collections.Generic;

public class RepairMachineTask : Task
{
    [Header("Repair Settings")]
    [SerializeField] private List<RepairablePart> brokenParts = new List<RepairablePart>();
    [SerializeField] private float repairTimePerPart = 3f;

    private int partsRepaired = 0;

    protected override void OnTaskStartedCustom()
    {
        partsRepaired = 0;

        // Initialize all parts
        foreach (RepairablePart part in brokenParts)
        {
            if (part != null)
            {
                part.Initialize(repairTimePerPart);
                part.OnPartRepaired += OnPartRepaired;
            }
        }
    }

    protected override bool CheckCompletion()
    {
        return partsRepaired >= brokenParts.Count;
    }

    private void OnPartRepaired(RepairablePart part)
    {
        partsRepaired++;
        Debug.Log($"Part repaired! {partsRepaired}/{brokenParts.Count} complete");
    }

    protected override void OnTaskCompletedCustom()
    {
        Debug.Log("Machine fully repaired!");
    }

    protected override void OnTaskFailedCustom()
    {
        Debug.Log("Failed to repair machine in time!");

        // Reset all parts
        foreach (RepairablePart part in brokenParts)
        {
            if (part != null)
            {
                part.ResetPart();
            }
        }
    }

    public override void ResetTask()
    {
        base.ResetTask();
        partsRepaired = 0;

        foreach (RepairablePart part in brokenParts)
        {
            if (part != null)
            {
                part.ResetPart();
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        foreach (RepairablePart part in brokenParts)
        {
            if (part != null)
            {
                part.OnPartRepaired -= OnPartRepaired;
            }
        }
    }
}

