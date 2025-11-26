using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrabber : MonoBehaviour
{
    public Transform grabPoint; // Position where object is held
    public float grabDistance = 3f;
    public LayerMask grabbableLayer;

    private GrabbableObject grabbedObject;

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Interact pressed");
            if (grabbedObject == null)
                TryGrab();
            else
                Release();
        }
    }

    void TryGrab()
    {
        Debug.Log("Try grab");
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.transform.position, 
            Camera.main.transform.forward, out hit, grabDistance, grabbableLayer))
        {
            Debug.Log("Grabbable object interacted with");
            GrabbableObject grobject = hit.collider.GetComponent<GrabbableObject>();
            if (grobject != null)
            {
                Debug.Log("Grabbable object component found");
                grabbedObject = grobject;
                grobject.AddGrabber(this);
            }
        }
    }

    void Release()
    {
        Debug.Log("Release");
        if (grabbedObject == null)
            return;

        grabbedObject.RemoveGrabber(this);
        grabbedObject = null;
    }
}
