using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Machine : NetworkBehaviour
{
    public string machineName;
    public AssemblyLineManager assemblyLineManager;

    // Use a NetworkVariable to sync powered state
    public NetworkVariable<bool> PoweredOn = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    [Header("Events")]
    public UnityEvent<bool> OnPoweredChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        PowerOn();
    }

    public void TogglePowerToMachine()
    {
        if (PoweredOn.Value)
            PowerOff();
        else
            PowerOn();
    }

    public void PowerOn()
    {
        if (!IsServer) return; // Only server changes state
        PoweredOn.Value = true;
    }

    public void PowerOff()
    {
        if (!IsServer) return;
        PoweredOn.Value = false;
        assemblyLineManager.StopAllBelts();
    }

    public bool IsPoweredOn()
    {
        return PoweredOn.Value;
    }

    public bool IsOperational()
    {
        Debug.Log("PoweredOn: " + PoweredOn.Value + "assemblyLineManager.IsLineRunning(): " + assemblyLineManager.IsLineRunning());
        return PoweredOn.Value && assemblyLineManager.IsLineRunning();
    }

    private void OnEnable()
    {
        PoweredOn.OnValueChanged += HandlePoweredChanged;
    }

    private void OnDisable()
    {
        PoweredOn.OnValueChanged -= HandlePoweredChanged;
    }

    private void HandlePoweredChanged(bool oldValue, bool newValue)
    {
        OnPoweredChanged?.Invoke(newValue);
    }
}
