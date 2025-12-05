using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class MachineTaskManager : NetworkBehaviour
{
    [Header("Events")]
    public UnityEvent OnTasksActivated;
    public UnityEvent OnTasksDeactivated;

    private List<Task> tasks = new List<Task>();
    private NetworkVariable<int> activeTaskCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool wasActive = false;

    public int ActiveTaskCount => activeTaskCount.Value;
    public bool HasActiveTasks => activeTaskCount.Value > 0;
    public IReadOnlyList<Task> Tasks => tasks.AsReadOnly();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        activeTaskCount.OnValueChanged += OnActiveTaskCountChanged;
        RefreshTasks();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        activeTaskCount.OnValueChanged -= OnActiveTaskCountChanged;
    }

    private void Awake()
    {
        RefreshTasks();
    }

    private void Update()
    {
        if (IsServer)
        {
            UpdateActiveTaskCount();
        }
    }

    private void OnActiveTaskCountChanged(int previousValue, int newValue)
    {
        bool isActive = newValue > 0;
        bool wasActiveBefore = previousValue > 0;
        if (isActive && !wasActiveBefore)
        {
            OnTasksActivated?.Invoke();
        }
        else if (!isActive && wasActiveBefore)
        {
            OnTasksDeactivated?.Invoke();
        }
    }

    public void RefreshTasks()
    {
        tasks.Clear();
        tasks.AddRange(GetComponents<Task>());
    }

    private void UpdateActiveTaskCount()
    {
        int count = tasks.Count(task => task != null && task.IsActive);
        if (activeTaskCount.Value != count)
        {
            activeTaskCount.Value = count;
        }
    }

    public void RegisterTask(Task task)
    {
        if (!tasks.Contains(task))
        {
            tasks.Add(task);
        }
    }

    public void UnregisterTask(Task task)
    {
        tasks.Remove(task);
    }

    public List<Task> GetActiveTasks()
    {
        return tasks.Where(task => task != null && task.IsActive).ToList();
    }

    public List<Task> GetCompletedTasks()
    {
        return tasks.Where(task => task != null && task.IsCompleted).ToList();
    }

    public List<Task> GetFailedTasks()
    {
        return tasks.Where(task => task != null && task.HasFailed).ToList();
    }
}
