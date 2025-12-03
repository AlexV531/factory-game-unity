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

    private List<string> outputLines = new List<string>();
    private List<string> commandHistory = new List<string>();
    private int historyIndex = -1;
    private bool isActive = false;
    private ulong activeClientId;
    private Transform activePlayerTransform;
    private InputActionMap previousActionMap;

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

        AddSystemMessage("Terminal v1.0 - Type 'help' for commands");
        AddOutputLine("");
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
        // Check if player is too far away
        if (isActive && activePlayerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, activePlayerTransform.position);
            if (distance > maxInteractionDistance)
            {
                DeactivateTerminal();
                return;
            }
        }

        if (isActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            DeactivateTerminal();
        }

        // Keep input field focused
        if (isActive && inputField != null && !inputField.isFocused)
        {
            inputField.ActivateInputField();
        }

        if (isActive && inputField != null && inputField.isFocused && Keyboard.current != null)
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

        if (!IsOwner && !IsServer)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId)
                return;
        }

        if (!isActive)
        {
            ActivateTerminal(clientId);
        }
        else
        {
            DeactivateTerminal();
        }
    }

    void ActivateTerminal(ulong clientId)
    {
        isActive = true;
        activeClientId = clientId;

        // Get the player transform from the clientId
        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == clientId && client.PlayerObject != null)
                {
                    activePlayerTransform = client.PlayerObject.transform;

                    // Switch to Look action map
                    var playerInput = client.PlayerObject.GetComponent<PlayerInput>();
                    if (playerInput != null)
                    {
                        previousActionMap = playerInput.currentActionMap;
                        playerInput.SwitchCurrentActionMap(terminalActionMapName);
                    }
                    break;
                }
            }
        }

        if (terminalCanvas != null)
        {
            terminalCanvas.gameObject.SetActive(true);
        }

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }

        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
    }

    void DeactivateTerminal()
    {
        isActive = false;

        // Switch back to Player action map
        if (NetworkManager.Singleton != null && activePlayerTransform != null)
        {
            var playerInput = activePlayerTransform.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap(playerActionMapName);
            }
        }

        activePlayerTransform = null;

        if (terminalCanvas != null)
        {
            terminalCanvas.gameObject.SetActive(false);
        }

        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void OnSubmit(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            if (inputField != null)
                inputField.ActivateInputField();
            return;
        }

        AddOutputLine($"> {command}");
        commandHistory.Add(command);
        historyIndex = commandHistory.Count;
        ProcessCommand(command.Trim());

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }

        ScrollToBottom();
    }

    void ProcessCommand(string command)
    {
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
                ClearOutput();
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
                DeactivateTerminal();
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

    public void AddOutputLine(string line)
    {
        outputLines.Add(line);

        if (outputLines.Count > maxOutputLines)
        {
            outputLines.RemoveAt(0);
        }

        UpdateOutputText();
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

    public void ClearOutput()
    {
        outputLines.Clear();
        UpdateOutputText();
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
        return isActive;
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction distance sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxInteractionDistance);
    }
}
