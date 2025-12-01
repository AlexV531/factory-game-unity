using UnityEngine;
using Unity.Netcode;

public class SnappingJoinedObject : NetworkBehaviour
{
    [Header("Edge Snapping Settings")]
    public ConfigurableJoint configurableJoint;
    public Transform[] edgePoints;
    [Tooltip("Distance from edge to trigger snapping")]
    public float snapDistance = 0.5f;
    [Tooltip("Distance from edge where joint target starts")]
    public float jointStartDistance = 0f;

    private Rigidbody rb;
    private Vector3[] edgeTargets;
    private JointDrive originalXDrive;
    private JointDrive originalYDrive;
    private JointDrive originalZDrive;
    private bool drivesDisabled = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Convert all edge points to local space
        if (edgePoints != null && edgePoints.Length > 0)
        {
            edgeTargets = new Vector3[edgePoints.Length];
            for (int i = 0; i < edgePoints.Length; i++)
            {
                if (edgePoints[i] != null)
                {
                    edgeTargets[i] = rb.transform.InverseTransformPoint(edgePoints[i].transform.position);
                }
            }
        }

        // Store original drive settings
        if (configurableJoint != null)
        {
            originalXDrive = configurableJoint.xDrive;
            originalYDrive = configurableJoint.yDrive;
            originalZDrive = configurableJoint.zDrive;

            // Start with drives disabled
            DisableJointDrives();
        }
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        UpdateEdgeSnapping();
    }

    void UpdateEdgeSnapping()
    {
        // Skip edge snapping if no joint is configured
        if (configurableJoint == null)
        {
            return;
        }

        // Skip if no edge points are configured
        if (edgePoints == null || edgePoints.Length == 0)
        {
            return;
        }

        Vector3 closestEdgeTarget = Vector3.zero;
        float closestDistance = float.MaxValue;
        bool foundValidEdge = false;

        // Check distance to all edge points
        for (int i = 0; i < edgePoints.Length; i++)
        {
            if (edgePoints[i] != null)
            {
                float distance = Vector3.Distance(transform.position, edgePoints[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEdgeTarget = edgeTargets[i];
                    foundValidEdge = true;
                }
            }
        }

        // If close enough to an edge, set joint target and enable drives
        if (foundValidEdge && closestDistance <= snapDistance)
        {
            Debug.Log("within snapping distance");

            // Enable drives if they're disabled
            if (drivesDisabled)
            {
                EnableJointDrives();
            }

            Vector3 jointTargetPosition = closestEdgeTarget;
            configurableJoint.targetPosition = -jointTargetPosition; // Invert due to joint being inverted
        }
        else
        {
            // Not close enough to any edge, disable drives
            DisableJointDrives();
        }
    }

    void EnableJointDrives()
    {
        if (configurableJoint != null && drivesDisabled)
        {
            configurableJoint.xDrive = originalXDrive;
            configurableJoint.yDrive = originalYDrive;
            configurableJoint.zDrive = originalZDrive;
            drivesDisabled = false;
        }
    }

    void DisableJointDrives()
    {
        if (configurableJoint != null && !drivesDisabled)
        {
            JointDrive disabledDrive = new JointDrive
            {
                positionSpring = 0,
                positionDamper = 0,
                maximumForce = 0
            };

            configurableJoint.xDrive = disabledDrive;
            configurableJoint.yDrive = disabledDrive;
            configurableJoint.zDrive = disabledDrive;
            drivesDisabled = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (edgePoints == null || edgePoints.Length == 0)
            return;

        Color[] colors = new Color[] { Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, Color.red };

        for (int i = 0; i < edgePoints.Length; i++)
        {
            if (edgePoints[i] != null)
            {
                Gizmos.color = colors[i % colors.Length];
                Gizmos.DrawWireSphere(edgePoints[i].position, snapDistance);
                Gizmos.DrawSphere(edgePoints[i].position, 0.05f);
            }
        }
    }
}
