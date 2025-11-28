using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class VehicleController : Interactable
{
    [Header("Settings")]
    public float motorForce = 15f;
    public float turnSpeed = 100f;
    public float maxSpeed = 20f;
    public float minTurnSpeedFactor = 0.08f;

    [Header("Entry System")]
    public Transform driverSeat;

    [Header("Fork System")]
    public Transform forks;
    public float forkLiftSpeed = 2f;
    public float minForkHeight = 0f;
    public float maxForkHeight = 3f;

    [Header("Client Prediction")]
    public float reconciliationThreshold = 0.5f; // Distance before snapping to server position
    public float reconciliationSpeed = 10f; // How fast to smooth toward server position

    private Rigidbody rb;
    private VehicleControls controls;
    private Vector2 moveInput;
    private bool isBraking;
    private float forkInput;

    private float currentForkHeight = 0f;
    private Vector3 forkStartPosition;

    private NetworkVariable<ulong> driverClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GameObject currentDriver;

    // Client prediction state
    private Vector3 lastServerPosition;
    private Quaternion lastServerRotation;
    private bool isReconciling = false;

    private void Awake()
    {
        controls = new VehicleControls();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.linearDamping = 1f;
        rb.angularDamping = 1f;

        if (forks != null)
            forkStartPosition = forks.localPosition;

        lastServerPosition = transform.position;
        lastServerRotation = transform.rotation;
    }

    private void OnEnable()
    {
        controls.Vehicle.Enable();
        controls.Vehicle.Move.performed += OnMove;
        controls.Vehicle.Move.canceled += OnMove;
        controls.Vehicle.Brake.performed += OnBrake;
        controls.Vehicle.Brake.canceled += OnBrake;
        controls.Vehicle.Lift.performed += OnLift;
        controls.Vehicle.Lift.canceled += OnLift;
    }

    private void OnDisable()
    {
        controls.Vehicle.Move.performed -= OnMove;
        controls.Vehicle.Move.canceled -= OnMove;
        controls.Vehicle.Brake.performed -= OnBrake;
        controls.Vehicle.Brake.canceled -= OnBrake;
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
        NotifyEnterVehicleClientRpc(clientId);
    }

    private void ExitVehicle(GameObject player)
    {
        ulong exitingClientId = driverClientId.Value;

        driverClientId.Value = ulong.MaxValue;
        currentDriver = null;

        moveInput = Vector2.zero;
        isBraking = false;
        forkInput = 0f;

        NotifyExitVehicleClientRpc(exitingClientId);
    }

    [ClientRpc]
    private void NotifyEnterVehicleClientRpc(ulong driverId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(driverId, out var client)) return;

        GameObject player = client.PlayerObject.gameObject;
        if (player == null) return;

        currentDriver = player;

        if (driverSeat != null)
        {
            player.transform.position = driverSeat.position;
            player.transform.rotation = driverSeat.rotation;
        }

        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerRb.isKinematic = true;

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
        player.transform.position = transform.position + transform.right * 2f;

        var playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerRb.isKinematic = false;

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
        SendVehicleInputServerRpc(input, forkInput, isBraking);
    }

    private void OnBrake(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayerDriving()) return;
        bool brake = context.ReadValueAsButton();
        SendVehicleInputServerRpc(moveInput, forkInput, brake);
    }

    private void OnLift(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayerDriving()) return;
        float lift = context.ReadValue<float>();
        SendVehicleInputServerRpc(moveInput, lift, isBraking);
    }

    [Rpc(SendTo.Server)]
    private void SendVehicleInputServerRpc(Vector2 move, float fork, bool brake)
    {
        moveInput = move;
        forkInput = fork;
        isBraking = brake;
    }

    private void FixedUpdate()
    {
        if (!HasDriver()) return;

        if (IsServer)
        {
            // Server: Run authoritative physics
            RunVehiclePhysics();
        }
        else if (IsLocalPlayerDriving())
        {
            // Local client driving: Run prediction
            RunVehiclePhysics();
            CheckReconciliation();
        }
        // Other clients: NetworkTransform handles interpolation normally
    }

    private void RunVehiclePhysics()
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // Apply forward/backward force
        if (Mathf.Abs(forwardSpeed) < maxSpeed)
            rb.AddForce(transform.forward * moveInput.y * motorForce, ForceMode.Acceleration);

        // Steering proportional to speed
        float speedFactor = Mathf.Lerp(minTurnSpeedFactor, 1f, Mathf.InverseLerp(0f, maxSpeed, Mathf.Abs(forwardSpeed)));
        float proportionalTurnSpeed = turnSpeed * speedFactor;
        float targetAngularVelocity = moveInput.x * proportionalTurnSpeed * Mathf.Deg2Rad;

        if (!rb.isKinematic)
            rb.angularVelocity = new Vector3(0, targetAngularVelocity, 0);

        // Braking
        if (isBraking)
            rb.linearVelocity *= 0.95f;

        // Fork lifting
        if (forks != null)
        {
            currentForkHeight = Mathf.Clamp(
                currentForkHeight + forkInput * forkLiftSpeed * Time.fixedDeltaTime,
                minForkHeight,
                maxForkHeight
            );

            Vector3 newForkPosition = forkStartPosition;
            newForkPosition.y += currentForkHeight;
            forks.localPosition = newForkPosition;
        }
    }

    private void CheckReconciliation()
    {
        // When server update arrives, check if we need to correct
        float positionError = Vector3.Distance(transform.position, lastServerPosition);
        
        if (positionError > reconciliationThreshold)
        {
            // Large error: snap to server position
            transform.position = lastServerPosition;
            transform.rotation = lastServerRotation;
            rb.linearVelocity = Vector3.zero;
            isReconciling = false;
        }
        else if (positionError > 0.01f)
        {
            // Small error: smoothly correct
            isReconciling = true;
        }
    }

    private void Update()
    {
        // Track server position updates (NetworkTransform sets these)
        if (!IsServer && !IsLocalPlayerDriving())
        {
            lastServerPosition = transform.position;
            lastServerRotation = transform.rotation;
        }
        else if (!IsServer && IsLocalPlayerDriving() && isReconciling)
        {
            // Smoothly correct toward server position
            transform.position = Vector3.Lerp(transform.position, lastServerPosition, Time.deltaTime * reconciliationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, lastServerRotation, Time.deltaTime * reconciliationSpeed);
            
            if (Vector3.Distance(transform.position, lastServerPosition) < 0.01f)
                isReconciling = false;
        }
    }

    public void OnServerPositionUpdate(Vector3 serverPos, Quaternion serverRot)
    {
        // Call this from NetworkTransform's OnValueChanged callback
        lastServerPosition = serverPos;
        lastServerRotation = serverRot;
    }

    private void LateUpdate()
    {
        if (currentDriver != null && driverSeat != null)
        {
            currentDriver.transform.position = driverSeat.position;
            currentDriver.transform.rotation = driverSeat.rotation;
        }
    }

    private bool IsLocalPlayerDriving() => driverClientId.Value == NetworkManager.Singleton.LocalClientId;
    public bool HasDriver() => driverClientId.Value != ulong.MaxValue;
}
