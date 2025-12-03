using UnityEngine;
using UnityEngine.Events;

public abstract class Task : MonoBehaviour
{
    [Header("Task Settings")]
    [SerializeField] protected string taskName;
    [SerializeField] protected string taskDescription;
    [SerializeField] protected float timeLimit = 0f; // 0 = no time limit
    [SerializeField] protected Machine machine = null; // null = no related machine

    [Header("Task Events")]
    public UnityEvent OnTaskStarted;
    public UnityEvent OnTaskCompleted;
    public UnityEvent OnTaskFailed;

    protected bool isActive = false;
    protected bool isCompleted = false;
    protected bool hasFailed = false;
    protected float elapsedTime = 0f;

    // Properties
    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public bool HasFailed => hasFailed;
    public float TimeRemaining => Mathf.Max(0, timeLimit - elapsedTime);
    public float TimeLimit => timeLimit;
    public string TaskName => taskName;
    public string TaskDescription => taskDescription;

    protected virtual void Update()
    {
        if (!isActive || isCompleted || hasFailed)
            return;

        elapsedTime += Time.deltaTime;

        // Check time limit
        if (timeLimit > 0 && elapsedTime >= timeLimit)
        {
            FailTask();
            return;
        }

        // Check task completion condition
        if (CheckCompletion())
        {
            CompleteTask();
        }
    }

    // Start the task
    public virtual void StartTask()
    {
        if (isActive)
            return;

        isActive = true;
        isCompleted = false;
        hasFailed = false;
        elapsedTime = 0f;

        OnTaskStarted?.Invoke();
        OnTaskStartedCustom();
    }

    // Complete the task
    protected virtual void CompleteTask()
    {
        if (!isActive || isCompleted || hasFailed)
            return;

        isCompleted = true;
        isActive = false;

        OnTaskCompleted?.Invoke();
        OnTaskCompletedCustom();
    }

    // Fail the task
    protected virtual void FailTask()
    {
        if (!isActive || isCompleted || hasFailed)
            return;

        hasFailed = true;
        isActive = false;

        if (machine != null)
        {
            machine.PowerOff();
        }

        OnTaskFailed?.Invoke();
        OnTaskFailedCustom();
    }

    // Abstract method - child classes must implement their completion logic
    protected abstract bool CheckCompletion();

    // Virtual methods for custom behavior in child classes
    protected virtual void OnTaskStartedCustom() { }
    protected virtual void OnTaskCompletedCustom() { }
    protected virtual void OnTaskFailedCustom() { }

    // Public method to manually complete task (for external triggers)
    public void ForceComplete()
    {
        CompleteTask();
    }

    // Public method to manually fail task (for external triggers)
    public void ForceFail()
    {
        FailTask();
    }

    // Reset the task
    public virtual void ResetTask()
    {
        isActive = false;
        isCompleted = false;
        hasFailed = false;
        elapsedTime = 0f;
    }
}
