using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class BlowoffPanel : NetworkBehaviour
{
    [Header("Blowoff Settings")]
    [SerializeField] private Vector3 forceDirection = Vector3.forward;
    [SerializeField] private float forceStrength = 10f;
    [SerializeField] private float torqueStrength = 5f;
    [SerializeField] private bool randomizeTorque = true;
    [SerializeField] private bool useLocalDirection = true;

    [Header("Audio")]
    [SerializeField] private AudioClip blowoffSound;
    [SerializeField] private float volume = 1f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float effectDuration = 2f;

    private Rigidbody rb;
    private AudioSource audioSource;
    private bool hasBlownOff = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Start with rigidbody kinematic
        rb.isKinematic = true;
    }

    public void BlowOff()
    {
        if (hasBlownOff)
            return;

        if (IsServer)
        {
            BlowOffServerRpc();
        }
        else
        {
            BlowOffServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void BlowOffServerRpc()
    {
        if (hasBlownOff)
            return;

        PerformBlowoff();
        BlowOffClientRpc();
    }

    [ClientRpc]
    private void BlowOffClientRpc()
    {
        if (IsServer)
            return;

        PlayAudioAndEffects();
    }

    private void PerformBlowoff()
    {
        hasBlownOff = true;

        // Enable physics
        rb.isKinematic = false;

        // Calculate force direction
        Vector3 finalForceDirection = useLocalDirection ?
            transform.TransformDirection(forceDirection) :
            forceDirection;

        // Apply force
        rb.AddForce(finalForceDirection.normalized * forceStrength, ForceMode.Impulse);

        // Apply torque
        if (randomizeTorque)
        {
            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * torqueStrength;
            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }
        else
        {
            rb.AddTorque(finalForceDirection.normalized * torqueStrength, ForceMode.Impulse);
        }

        PlayAudioAndEffects();
    }

    private void PlayAudioAndEffects()
    {
        // Play sound
        if (blowoffSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(blowoffSound, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(blowoffSound, transform.position, volume);
            }
        }

        // Spawn explosion effect
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, transform.rotation);
            Destroy(effect, effectDuration);
        }
    }

    public void Reset()
    {
        hasBlownOff = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Visualize the force direction in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 direction = useLocalDirection ?
            transform.TransformDirection(forceDirection) :
            forceDirection;
        Gizmos.DrawRay(transform.position, direction.normalized * 2f);
    }
}
