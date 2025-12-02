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
    [Tooltip("If true, repels from edge points instead of snapping to them")]
    public bool repelFromEdges = false;

    [Header("Rotation Snapping Settings")]
    public bool useRotationSnapping = false;
    public Transform[] rotationTargets;
    [Tooltip("Angle difference to trigger rotation snapping")]
    public float rotationSnapAngle = 45f;

    private Rigidbody rb;
    private Vector3[] edgeTargets;
    private Quaternion[] rotationTargetQuaternions;
    private JointDrive originalXDrive;
    private JointDrive originalYDrive;
    private JointDrive originalZDrive;
    private JointDrive originalAngularXDrive;
    private JointDrive originalAngularYZDrive;
    private bool drivesDisabled = false;
    private bool angularDrivesDisabled = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Store original drive settings
        if (configurableJoint != null)
        {
            originalXDrive = configurableJoint.xDrive;
            originalYDrive = configurableJoint.yDrive;
            originalZDrive = configurableJoint.zDrive;
            originalAngularXDrive = configurableJoint.angularXDrive;
            originalAngularYZDrive = configurableJoint.angularYZDrive;

            // Start with drives disabled
            DisableJointDrives();
            if (useRotationSnapping)
            {
                DisableAngularDrives();
            }
        }
    }

    void Start()
    {
        // Convert all edge points to local space without scale affecting the coordinates
        if (edgePoints != null && edgePoints.Length > 0)
        {
            edgeTargets = new Vector3[edgePoints.Length];
            for (int i = 0; i < edgePoints.Length; i++)
            {
                if (edgePoints[i] != null)
                {
                    // Get world offset
                    Vector3 worldOffset = edgePoints[i].transform.position - rb.transform.position;
                    // Convert to local direction (rotation only, no scale)
                    Vector3 localDirection = rb.transform.InverseTransformDirection(worldOffset);
                    // Keep the world distance
                    edgeTargets[i] = localDirection.normalized * worldOffset.magnitude;
                }
            }
        }

        // Store rotation targets as local quaternions
        if (useRotationSnapping && rotationTargets != null && rotationTargets.Length > 0)
        {
            rotationTargetQuaternions = new Quaternion[rotationTargets.Length];
            for (int i = 0; i < rotationTargets.Length; i++)
            {
                if (rotationTargets[i] != null)
                {
                    // Store as local rotation relative to rigidbody
                    rotationTargetQuaternions[i] = Quaternion.Inverse(rb.transform.rotation) * rotationTargets[i].rotation;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        UpdateEdgeSnapping();
        if (useRotationSnapping)
        {
            UpdateRotationSnapping();
        }
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

            if (repelFromEdges)
            {
                // Invert the target to push away from the edge
                jointTargetPosition = -closestEdgeTarget;
            }

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

    void UpdateRotationSnapping()
    {
        // Skip rotation snapping if no joint is configured
        if (configurableJoint == null)
        {
            return;
        }

        // Skip if no rotation targets are configured
        if (rotationTargets == null || rotationTargets.Length == 0)
        {
            return;
        }

        Quaternion closestRotation = Quaternion.identity;
        float smallestAngle = float.MaxValue;
        bool foundValidRotation = false;

        // Check angle difference to all rotation targets
        for (int i = 0; i < rotationTargets.Length; i++)
        {
            if (rotationTargets[i] != null)
            {
                float angle = Quaternion.Angle(transform.rotation, rotationTargets[i].rotation);
                if (angle < smallestAngle)
                {
                    smallestAngle = angle;
                    closestRotation = rotationTargetQuaternions[i];
                    foundValidRotation = true;
                }
            }
        }

        // If close enough to a rotation target, set joint target and enable angular drives
        if (foundValidRotation && smallestAngle <= rotationSnapAngle)
        {
            Debug.Log("within rotation snapping angle");

            // Enable angular drives if they're disabled
            if (angularDrivesDisabled)
            {
                EnableAngularDrives();
            }

            // Invert the rotation for the joint (ConfigurableJoint uses inverted target rotation)
            configurableJoint.targetRotation = Quaternion.Inverse(closestRotation);
        }
        else
        {
            // Not close enough to any rotation target, disable angular drives
            DisableAngularDrives();
        }
    }

    void EnableAngularDrives()
    {
        if (configurableJoint != null && angularDrivesDisabled)
        {
            configurableJoint.angularXDrive = originalAngularXDrive;
            configurableJoint.angularYZDrive = originalAngularYZDrive;
            angularDrivesDisabled = false;
        }
    }

    void DisableAngularDrives()
    {
        if (configurableJoint != null && !angularDrivesDisabled)
        {
            JointDrive disabledDrive = new JointDrive
            {
                positionSpring = 0,
                positionDamper = 0,
                maximumForce = 0
            };

            configurableJoint.angularXDrive = disabledDrive;
            configurableJoint.angularYZDrive = disabledDrive;
            angularDrivesDisabled = true;
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

        // Draw rotation targets
        if (useRotationSnapping && rotationTargets != null)
        {
            for (int i = 0; i < rotationTargets.Length; i++)
            {
                if (rotationTargets[i] != null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.matrix = Matrix4x4.TRS(rotationTargets[i].position, rotationTargets[i].rotation, Vector3.one * 0.3f);
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    Gizmos.DrawLine(Vector3.zero, Vector3.forward);
                    Gizmos.matrix = Matrix4x4.identity;
                }
            }
        }
    }
}
