using UnityEngine;

public class TrainController : MonoBehaviour
{
    public Transform[] points;
    public float speed = 5f;
    public float stopDuration = 2f;

    private int currentPointIndex = 0;
    private bool isStopping = false;

    void Update()
    {
        if (points.Length == 0 || isStopping) return;

        Transform target = points[currentPointIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // Optional: rotate to face movement direction
        Vector3 direction = (target.position - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        // Check if we've reached the current point
        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            StartCoroutine(HandleStopAtPoint());
        }
    }

    System.Collections.IEnumerator HandleStopAtPoint()
    {
        isStopping = true;

        if (currentPointIndex == 0)
            yield return new WaitForSeconds(stopDuration);

        currentPointIndex++;

        // Clamp to avoid going out of bounds
        if (currentPointIndex >= points.Length)
            currentPointIndex = points.Length - 1; // Stop at the last point

        isStopping = false;
    }
}
