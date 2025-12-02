using UnityEngine;
using System.Collections;

public class TrainController : MonoBehaviour
{
    [Header("Points")]
    public Transform startPoint;
    public Transform stopPoint;
    public Transform endPoint;

    [Header("Timing")]
    public float speed = 20f;
    public float stopDuration = 5f;
    public float processCycleTime = 30f; // Time between each process start

    [Header("Auto Start")]
    public bool autoStart = true;

    private bool isProcessRunning = false;
    private float nextProcessTime = 0f;

    void Start()
    {
        if (autoStart && startPoint != null)
        {
            transform.position = startPoint.position;
            nextProcessTime = Time.time + processCycleTime;
        }
    }

    void Update()
    {
        if (!autoStart) return;

        if (!isProcessRunning && Time.time >= nextProcessTime)
        {
            StartCoroutine(RunProcess());
        }
    }

    public void ManualStartProcess()
    {
        if (!isProcessRunning)
        {
            StartCoroutine(RunProcess());
        }
    }

    private IEnumerator RunProcess()
    {
        isProcessRunning = true;

        Debug.Log("Process started.");

        // Teleport to start point
        if (startPoint != null)
            transform.position = startPoint.position;
        Debug.Log("Moved to start point.");

        // Move to stop point
        yield return StartCoroutine(MoveToPoint(stopPoint));
        Debug.Log("Moved to stop point.");

        // Stop at the stop point
        yield return new WaitForSeconds(stopDuration);

        // Move to end point
        yield return StartCoroutine(MoveToPoint(endPoint));
        Debug.Log("Moved to end point.");

        // Process complete, schedule next one
        nextProcessTime = Time.time + processCycleTime;
        isProcessRunning = false;
    }

    private IEnumerator MoveToPoint(Transform target)
    {
        if (target == null) yield break;

        while (Vector3.Distance(transform.position, target.position) > 0.01f)
        {
            Debug.Log("Moving");

            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

            // Rotate to face movement direction
            Vector3 direction = (target.position - transform.position).normalized;
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);

            yield return null;
        }

        // Snap to exact position
        transform.position = target.position;
    }
}
