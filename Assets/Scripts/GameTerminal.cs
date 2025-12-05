using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GameTerminal : Interactable
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public TMP_Text outputText;
    public ScrollRect scrollRect;
    public Canvas terminalCanvas;

    [Header("Display Settings")]
    public int maxOutputLines = 100;
    public Color systemColor = Color.cyan;
    public Color errorColor = Color.red;
    public Color successColor = Color.green;

    [Header("Interaction Settings")]
    public float maxInteractionDistance = 5f;
    public string playerActionMapName = "Player";
    public string terminalActionMapName = "Look";

    private NetworkVariable<bool> isTerminalActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> activeClientId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<string> outputLines = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;
    private Transform activePlayerTransform;
    private InputActionMap previousActionMap;
    private bool hasControl = false;

    void Start()
    {
        requiresSustainedInteraction = false;

        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmit);
        }

        if (terminalCanvas != null)
        {
            terminalCanvas.gameObject.SetActive(false);
        }

        // Subscribe to network variable changes
        isTerminalActive.OnValueChanged += OnTerminalActiveChanged;
        activeClientId.OnValueChanged += OnActiveClientChanged;

        AddSystemMessage("Terminal v1.0 - Type 'help' for commands");
        AddOutputLine("");
    }

    void OnTerminalActiveChanged(bool previousValue, bool newValue)
    {
        // Update canvas visibility for all clients
        if (terminalCanvas != null)
        {
            terminalCanvas.gameObject.SetActive(newValue);
        }

        // Handle control setup for the active client
        if (newValue && NetworkManager.Singleton.LocalClientId == activeClientId.Value)
        {
            SetupLocalControl();
        }
        else if (!newValue && hasControl)
        {
            ReleaseLocalControl();
        }
    }

    void OnActiveClientChanged(ulong previousValue, ulong newValue)
    {
        // When the active client changes, check if we should have control
        if (isTerminalActive.Value && NetworkManager.Singleton.LocalClientId == newValue)
        {
            SetupLocalControl();
        }
        else if (hasControl && NetworkManager.Singleton.LocalClientId != newValue)
        {
            ReleaseLocalControl();
        }
    }

    void SetupLocalControl()
    {
        hasControl = true;

        // Get local player transform
        if (NetworkManager.Singleton != null)
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient != null && localClient.PlayerObject != null)
            {
                activePlayerTransform = localClient.PlayerObject.transform;

                // Switch to Look action map
                var playerInput = localClient.PlayerObject.GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    previousActionMap = playerInput.currentActionMap;
                    playerInput.SwitchCurrentActionMap(terminalActionMapName);
                }
            }
        }

        if (inputField != null)
        {
            inputField.interactable = true;
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    void ReleaseLocalControl()
    {
        hasControl = false;

        // Switch back to Player action map
        if (activePlayerTransform != null)
        {
            var playerInput = activePlayerTransform.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap(playerActionMapName);
            }
        }

        activePlayerTransform = null;

        if (inputField != null)
        {
            inputField.interactable = false;
        }
    }

    void DiagnoseMachine(string machineId)
    {
        // Find all machines
        Machine[] machines = FindObjectsByType<Machine>(FindObjectsSortMode.None);
        Machine targetMachine = null;

        // Search for machine by name (case-insensitive)
        foreach (Machine machine in machines)
        {
            if (machine != null && machine.machineId.Equals(machineId, System.StringComparison.OrdinalIgnoreCase))
            {
                targetMachine = machine;
                break;
            }
        }

        if (targetMachine == null)
        {
            AddErrorMessage($"Machine '{machineId}' not found");
            AddOutputLine("Use 'status' command to see all machines");
            return;
        }

        // Display machine diagnostics
        AddSystemMessage($"=== Diagnostics: {targetMachine.machineId} ===");

        // Power status
        bool isPowered = targetMachine.IsPoweredOn();
        if (isPowered)
        {
            AddSuccessMessage("Power: ONLINE");
        }
        else
        {
            AddErrorMessage("Power: OFFLINE");
        }

        // Get all Task components on the machine
        Task[] tasks = targetMachine.GetComponents<Task>();

        if (tasks.Length == 0)
        {
            AddOutputLine("No tasks configured");
        }
        else
        {
            AddOutputLine("");

            // Count active tasks
            int activeTaskCount = 0;
            foreach (Task task in tasks)
            {
                if (task != null && task.IsActive)
                {
                    activeTaskCount++;
                }
            }

            if (activeTaskCount == 0)
            {
                AddOutputLine("No active tasks");
            }
            else
            {
                AddSystemMessage($"Active Tasks ({activeTaskCount} total):");

                foreach (Task task in tasks)
                {
                    if (task != null && task.IsActive)
                    {
                        string taskDisplayName = !string.IsNullOrEmpty(task.TaskName) ? task.TaskName : task.GetType().Name;

                        AddOutputLine($"  [{taskDisplayName}] IN PROGRESS");
                        if (task.TimeLimit > 0)
                        {
                            AddOutputLine($"    Time Remaining: {task.TimeRemaining:F1}s");
                        }

                        // Show description if available
                        // if (!string.IsNullOrEmpty(task.TaskDescription))
                        // {
                        //     AddOutputLine($"    {task.TaskDescription}");
                        // }
                    }
                }
            }
        }

        AddOutputLine("");
    }

    void Update()
    {
        // Only run control logic if this client has control
        if (!hasControl)
        {
            return;
        }

        // Check if player is too far away
        if (activePlayerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, activePlayerTransform.position);
            if (distance > maxInteractionDistance)
            {
                RequestDeactivateTerminalServerRpc();
                return;
            }
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            RequestDeactivateTerminalServerRpc();
        }

        // Keep input field focused
        if (inputField != null && !inputField.isFocused)
        {
            inputField.ActivateInputField();
        }

        if (inputField != null && inputField.isFocused && Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                NavigateHistory(-1);
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                NavigateHistory(1);
            }
        }
    }

    public override void Interact(ulong clientId)
    {
        base.Interact(clientId);

        // This is called via ServerRpc, so we're already on the server
        if (!IsServer)
        {
            return;
        }

        if (!isTerminalActive.Value)
        {
            // Activate terminal for this client
            activeClientId.Value = clientId;
            isTerminalActive.Value = true;
        }
        else if (activeClientId.Value == clientId)
        {
            // Same client interacting again, deactivate
            isTerminalActive.Value = false;
        }
        // If a different client tries to interact, do nothing (or you could show a message)
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestDeactivateTerminalServerRpc()
    {
        isTerminalActive.Value = false;
    }

    void OnSubmit(string command)
    {
        if (!hasControl)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            if (inputField != null)
                inputField.ActivateInputField();
            return;
        }

        // Send command to server to process and broadcast
        SubmitCommandServerRpc(command);

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void SubmitCommandServerRpc(string command)
    {
        // Add to output and process on server
        AddOutputLineClientRpc($"> {command}");

        // Process command on server
        ProcessCommand(command.Trim());

        // Scroll to bottom for all clients
        ScrollToBottomClientRpc();
    }

    void ProcessCommand(string command)
    {
        if (!IsServer)
        {
            return;
        }

        string[] parts = command.ToLower().Split(' ');
        string cmd = parts[0];

        switch (cmd)
        {
            case "help":
                AddSystemMessage("Available Commands:");
                AddOutputLine("  help - Show this help message");
                AddOutputLine("  clear - Clear the terminal");
                AddOutputLine("  echo [text] - Echo back text");
                AddOutputLine("  time - Display current time");
                AddOutputLine("  status - Check assembly line status");
                AddOutputLine("  diagnose [machine] - Detailed machine diagnostics");
                AddOutputLine("  exit - Close terminal");
                break;

            case "clear":
                ClearOutputClientRpc();
                break;

            case "echo":
                if (parts.Length > 1)
                {
                    string echoText = command.Substring(5);
                    AddOutputLine(echoText);
                }
                else
                {
                    AddOutputLine("");
                }
                break;

            case "time":
                AddOutputLine(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                break;

            case "status":
                CheckAssemblyLineStatus();
                break;

            case "diagnose":
                if (parts.Length > 1)
                {
                    string machineName = command.Substring(9).Trim();
                    DiagnoseMachine(machineName);
                }
                else
                {
                    AddErrorMessage("Usage: diagnose [machine-name]");
                    AddOutputLine("Example: diagnose press-a");
                }
                break;

            case "exit":
            case "quit":
                isTerminalActive.Value = false;
                break;

            default:
                AddErrorMessage($"Unknown command: {cmd}");
                AddOutputLine("Type 'help' for available commands");
                break;
        }
    }

    void CheckAssemblyLineStatus()
    {
        if (AssemblyLineManager.Instance == null)
        {
            AddErrorMessage("Assembly line manager not found");
            return;
        }

        AddSystemMessage("=== Assembly Line Status ===");

        // Check if line is running
        bool isRunning = AssemblyLineManager.Instance.IsLineRunning();
        if (isRunning)
        {
            AddSuccessMessage($"Line Status: RUNNING");
        }
        else
        {
            AddErrorMessage($"Line Status: STOPPED");
        }

        AddOutputLine("");

        // List all machines individually
        Machine[] machines = FindObjectsByType<Machine>(FindObjectsSortMode.None);
        int poweredOffCount = 0;

        AddSystemMessage($"Machines ({machines.Length} total):");

        foreach (Machine machine in machines)
        {
            if (machine != null)
            {
                string machineId = machine.machineId;
                bool isPowered = machine.IsPoweredOn();

                if (isPowered)
                {
                    AddSuccessMessage($"  [{machineId}] ONLINE");
                }
                else
                {
                    AddErrorMessage($"  [{machineId}] OFFLINE");
                    poweredOffCount++;
                }
            }
        }

        AddOutputLine("");

        if (poweredOffCount > 0)
        {
            AddErrorMessage($"Warning: {poweredOffCount} machine(s) offline");
        }
        else
        {
            AddSuccessMessage("All systems operational");
        }

        AddOutputLine("");
    }

    [ClientRpc]
    void AddOutputLineClientRpc(string line)
    {
        AddOutputLineLocal(line);
    }

    void AddOutputLineLocal(string line)
    {
        outputLines.Add(line);

        if (outputLines.Count > maxOutputLines)
        {
            outputLines.RemoveAt(0);
        }

        UpdateOutputText();
    }

    public void AddOutputLine(string line)
    {
        if (IsServer)
        {
            AddOutputLineClientRpc(line);
        }
        else
        {
            AddOutputLineLocal(line);
        }
    }

    public void AddSystemMessage(string message)
    {
        AddOutputLine($"<color=#{ColorUtility.ToHtmlStringRGB(systemColor)}>[SYSTEM] {message}</color>");
    }

    public void AddErrorMessage(string message)
    {
        AddOutputLine($"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>[ERROR] {message}</color>");
    }

    public void AddSuccessMessage(string message)
    {
        AddOutputLine($"<color=#{ColorUtility.ToHtmlStringRGB(successColor)}>[SUCCESS] {message}</color>");
    }

    void UpdateOutputText()
    {
        if (outputText != null)
        {
            outputText.text = string.Join("\n", outputLines);
        }
    }

    [ClientRpc]
    void ClearOutputClientRpc()
    {
        outputLines.Clear();
        UpdateOutputText();
    }

    public void ClearOutput()
    {
        if (IsServer)
        {
            ClearOutputClientRpc();
        }
    }

    void NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0) return;

        historyIndex += direction;
        historyIndex = Mathf.Clamp(historyIndex, 0, commandHistory.Count);

        if (historyIndex < commandHistory.Count)
        {
            inputField.text = commandHistory[historyIndex];
            inputField.caretPosition = inputField.text.Length;
        }
        else
        {
            inputField.text = "";
        }
    }

    [ClientRpc]
    void ScrollToBottomClientRpc()
    {
        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public bool IsActive()
    {
        return isTerminalActive.Value;
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction distance sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxInteractionDistance);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (isTerminalActive != null)
        {
            isTerminalActive.OnValueChanged -= OnTerminalActiveChanged;
        }

        if (activeClientId != null)
        {
            activeClientId.OnValueChanged -= OnActiveClientChanged;
        }
    }
}
