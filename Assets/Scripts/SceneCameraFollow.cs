using UnityEngine;

public class SceneCameraFollow : MonoBehaviour
{
    public static SceneCameraFollow Instance;

    private Transform followTarget;   
    public Vector3 offset = Vector3.zero;
    public float followSpeed = 20f;

    void Awake()
    {
        Instance = this;
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    void LateUpdate()
    {
        if (followTarget == null) return;

        Vector3 targetPos = followTarget.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // exact rotation copy
        transform.rotation = followTarget.rotation;
    }
}
