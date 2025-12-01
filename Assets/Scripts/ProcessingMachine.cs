using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class ProcessingMachine : Machine
{
    // References
    public ObjectSpawner outputSpawner; // Where output products are spawned

    // Settings
    public float processingTime = 2f; // Time to process one output
    public int numInputsToProcess = 1; // Number of inputs needed to process one output

    public UnityEvent<int, int> OnNumInputsChanged;

    // Internal state
    protected bool isProcessing;
    protected List<int> inputtedProductCodes = new List<int>();
    public NetworkVariable<int> CurrentNumInputs = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentNumInputs.OnValueChanged += HandleNumInputsChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        CurrentNumInputs.OnValueChanged -= HandleNumInputsChanged;
    }

    public virtual void InputItem(int productCode)
    {
        CurrentNumInputs.Value++;
        inputtedProductCodes.Add(productCode);

        if (CurrentNumInputs.Value >= numInputsToProcess)
        {
            CurrentNumInputs.Value -= numInputsToProcess;
            StartCoroutine(ProcessProduct());
        }
    }

    protected virtual IEnumerator ProcessProduct()
    {
        isProcessing = true;
        float timer = 0f;

        // Wait for processing time to complete
        while (timer < processingTime)
        {
            if (IsOperational())
            {
                timer += Time.deltaTime;
            }
            yield return null;
        }

        // Wait until output spawner is ready
        while (!OutputReady())
        {
            yield return null;
        }

        // Now we can produce the output
        OutputProduct();
        isProcessing = false;
    }

    protected virtual bool OutputReady()
    {
        return outputSpawner.ReadyToSpawn();
    }

    protected virtual void OutputProduct()
    {
        outputSpawner.SpawnObject();
    }

    private void OnEnable()
    {
        CurrentNumInputs.OnValueChanged += HandleNumInputsChanged;
    }

    private void OnDisable()
    {
        CurrentNumInputs.OnValueChanged -= HandleNumInputsChanged;
    }

    private void HandleNumInputsChanged(int oldValue, int newValue)
    {
        OnNumInputsChanged?.Invoke(CurrentNumInputs.Value, numInputsToProcess);
    }

    public bool IsProcessing() => isProcessing;
}
