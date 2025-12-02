using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class VehicleController : Interactable
{
    [Header("Settings")]
    public float motorForce = 18f;
    public float turnSpeed = 260f;
    public float maxSpeed = 24f;
    public float minTurnSpeedFactor = 0.3f;

    [Header("Entry System")]
    public Transform driverSeat;

    [Header("Fork System")]
    public Transform forks;
    public ConfigurableJoint forksJoint;
    public float liftSpeed = 2f;
    public float minForkHeight = 0f;
    public float maxForkHeight = 3f;

    private Rigidbody rb;
    private Rigidbody forksRb;
    private VehicleControls controls;
    private Vector2 moveInput;
    private float forkInput;
    private Vector2 localMoveInput;
    private float localForkInput;
    private float currentForksTarget;

    // private float currentForkHeight = 0f;
    // private NetworkVariable<float> ForkHeight = new NetworkVariable<float>(
    //     0f,
    //     NetworkVariableReadPermission.Everyone,
    //     NetworkVariableWritePermission.Server
    // );
    private Vector3 forkStartPosition;

    private NetworkVariable<ulong> driverClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GameObject currentDriver;

    private void Awake()
    {
        controls = new VehicleControls();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // Forklift must always be dynamic
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.linearDamping = 1f;
        rb.angularDamping = 1f;

        forksRb = forks.GetComponent<Rigidbody>();
        forksRb.isKinematic = false;

        if (forks != null)
            forkStartPosition = forks.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void OnEnable()
    {
        controls.Vehicle.Enable();
        controls.Vehicle.Move.performed += OnMove;
        controls.Vehicle.Move.canceled += OnMove;
        controls.Vehicle.Lift.performed += OnLift;
        controls.Vehicle.Lift.canceled += OnLift;
    }

    private void OnDisable()
    {
        controls.Vehicle.Move.performed -= OnMove;
        controls.Vehicle.Move.canceled -= OnMove;
        controls.Vehicle.Lift.performed -= OnLift;
        controls.Vehicle.Lift.canceled -= OnLift;
        controls.Vehicle.Disable();
    }

    public override void Interact(ulong clientId)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        GameObject player = client.PlayerObject.gameObject;

        if (driverClientId.Value == clientId)
            ExitVehicle(player);
        else if (driverClientId.Value == ulong.MaxValue)
            EnterVehicle(player, clientId);
    }

    private void EnterVehicle(GameObject player, ulong clientId)
    {
        driverClientId.Value = clientId;
        currentDriver = player;

        // NetworkObject.ChangeOwnership(clientId);

        NotifyEnterVehicleClientRpc(clientId);
    }

    private void ExitVehicle(GameObject player)
    {
        ulong exitingClientId = driverClientId.Value;

        driverClientId.Value = ulong.MaxValue;
        currentDriver = null;

        moveInput = Vector2.zero;
        forkInput = 0f;

        // NetworkObject.RemoveOwnership();

        NotifyExitVehicleClientRpc(exitingClientId);
    }

    [ClientRpc]
    private void NotifyEnterVehicleClientRpc(ulong driverId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(driverId, out var client)) return;

        GameObject player = client.PlayerObject.gameObject;
        if (player == null) return;

        currentDriver = player;

        // Move player to driver seat initially
        if (driverSeat != null)
        {
            player.transform.position = driverSeat.position;
            player.transform.rotation = driverSeat.rotation;
        }

        // Make player kinematic
        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerRb.isKinematic = true;

        // Set isDriving on the player script
        var playerScript = player.GetComponent<PhysicsPlayerController>();
        if (playerScript != null)
            playerScript.SetVehicle(this);

        if (NetworkManager.Singleton.LocalClientId == driverId)
            Debug.Log("You are now driving the vehicle");
    }

    [ClientRpc]
    private void NotifyExitVehicleClientRpc(ulong driverId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(driverId, out var client)) return;

        GameObject player = client.PlayerObject.gameObject;
        if (player == null) return;

        currentDriver = null;

        // Move player slightly beside vehicle
        player.transform.position = transform.position + transform.right * 2f;

        // Make player non-kinematic
        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerRb.isKinematic = false;

        // Set isDriving on the player script
        var playerScript = player.GetComponent<PhysicsPlayerController>();
        if (playerScript != null)
            playerScript.SetVehicle(null);

        if (NetworkManager.Singleton.LocalClientId == driverId)
            Debug.Log("You exited the vehicle");
    }

    // Input callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayerDriving()) return;
        Vector2 input = context.ReadValue<Vector2>();
        localMoveInput = input;
        SendVehicleInputServerRpc(input, localForkInput);
    }

    private void OnLift(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayerDriving()) return;
        float lift = context.ReadValue<float>();
        localForkInput = lift;
        SendVehicleInputServerRpc(localMoveInput, lift);
    }

    // Server authoritative input
    [Rpc(SendTo.Server)]
    private void SendVehicleInputServerRpc(Vector2 move, float fork)
    {
        moveInput = move;
        forkInput = fork;
        // SendVehicleInputClientRpc(move, fork);
    }

    // [ClientRpc]
    // private void SendVehicleInputClientRpc(Vector2 move, float fork)
    // {
    //     moveInput = move;
    //     forkInput = fork;
    // }

    private void FixedUpdate()
    {
        if (!HasDriver()) return;

        if (IsServer)
        {
            // Update fork height
            if (forks != null)
            {
                // Compute new height
                currentForksTarget += forkInput * liftSpeed * Time.deltaTime;

                // Clamp to min/max
                currentForksTarget = Mathf.Clamp(currentForksTarget, minForkHeight, maxForkHeight);

                // Update the joint's target
                Vector3 tp = forksJoint.targetPosition;
                tp.y = -currentForksTarget;  // negative because Unity joint axis is reversed
                forksJoint.targetPosition = tp;
            }

            // Apply forward/backward force
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (Mathf.Abs(forwardSpeed) < maxSpeed)
                rb.AddForce(transform.forward * moveInput.y * motorForce, ForceMode.Acceleration);

            // Apply turning torque
            float speedFactor = Mathf.Lerp(minTurnSpeedFactor, 1f, Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(forwardSpeed)));
            float turnTorque = moveInput.x * turnSpeed;

            // Apply a minimum torque so you can always turn when stationary
            float minTorque = 0.5f; // tune as needed
            turnTorque = Mathf.Sign(turnTorque) * Mathf.Max(Mathf.Abs(turnTorque * speedFactor), minTorque);

            rb.AddTorque(Vector3.up * turnTorque * rb.mass * 0.1f, ForceMode.Force);

            // Clamp angular velocity to prevent runaway spin
            Vector3 angVel = rb.angularVelocity;
            angVel.y = Mathf.Clamp(angVel.y, -1f, 1f); 
            rb.angularVelocity = angVel;
        }

        // --- Client-side fork smoothing ---
        // if (!IsServer && forks != null)
        // {
        //     Vector3 targetPos = forkStartPosition;
        //     targetPos.y += ForkHeight.Value;
        //     forks.localPosition = Vector3.Lerp(forks.localPosition, targetPos, 10f * Time.deltaTime);
        // }

        // --- Smoothly move driver ---
        if (currentDriver != null && driverSeat != null)
        {
            currentDriver.transform.position = Vector3.Lerp(currentDriver.transform.position, driverSeat.position, 15f * Time.deltaTime);
            currentDriver.transform.rotation = Quaternion.Slerp(currentDriver.transform.rotation, driverSeat.rotation, 1f * Time.deltaTime);
        }
    }

    private bool IsLocalPlayerDriving() => driverClientId.Value == NetworkManager.Singleton.LocalClientId;
    public bool HasDriver() => driverClientId.Value != ulong.MaxValue;
}
