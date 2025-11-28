using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MaterialRequiredMachine : Machine
{
    public ObjectSpawner outputSpawner;

    public float processingTime = 6f;
    public float materialRequired = 5f;
    public float maxMaterial = 120f;
    public float startingMaterial = 100f;

    [Header("Material Quantity Event")]
    public UnityEvent<float, float> OnMaterialChanged;

    // private float currentMaterial;
    public NetworkVariable<float> CurrentMaterial = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private bool isProcessing;

    // void Start() 
    // {
    //     if (!IsServer) return;

    //     AddMaterial(startingMaterial);
    // }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        AddMaterial(startingMaterial);
    }

    void Update()
    {
        if (!IsServer) return;
        if (!IsOperational()) return;
        if (isProcessing) return;

        TryProcessNextItem();
    }

    private void TryProcessNextItem()
    {
        // Debug.Log("Trying");
        if (CurrentMaterial.Value >= materialRequired)
        {
            // Debug.Log("Made it");
            RemoveMaterial(materialRequired);
            StartCoroutine(ProcessProduct());
            return;
        }
    }

    private IEnumerator ProcessProduct()
    {
        isProcessing = true;

        float timer = 0f;
        while (timer < processingTime)
        {
            if (IsOperational())
            {
                timer += Time.deltaTime; // only count time when powered on
            }
            yield return null; // wait for next frame
        }

        // Produce the output
        SpawnOutputProduct();

        isProcessing = false;
    }

    private void SpawnOutputProduct()
    {
        outputSpawner.SpawnObject();
    }

    private void UpdateMaterial(float value)
    {
        CurrentMaterial.Value = Mathf.Clamp(value, 0, maxMaterial);
    }

    public void AddMaterial(float amount)
    {
        UpdateMaterial(CurrentMaterial.Value + amount);
    }

    public void RemoveMaterial(float amount)
    {
        UpdateMaterial(CurrentMaterial.Value - amount);
    }

    private void OnEnable()
    {
        CurrentMaterial.OnValueChanged += HandleCurrentMaterialChanged;
    }

    private void OnDisable()
    {
        CurrentMaterial.OnValueChanged -= HandleCurrentMaterialChanged;
    }

    private void HandleCurrentMaterialChanged(float oldValue, float newValue)
    {
        OnMaterialChanged?.Invoke(CurrentMaterial.Value, maxMaterial);
    }
}