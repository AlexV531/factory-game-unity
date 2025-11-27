using System.Collections;
using Unity.VisualScripting;
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

    private float currentMaterial;
    private bool isProcessing;

    void Start()
    {
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
        if (currentMaterial >= materialRequired)
        {
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
        currentMaterial = Mathf.Clamp(value, 0, maxMaterial);

        // Broadcast the new progress to listeners
        OnMaterialChanged?.Invoke(currentMaterial, maxMaterial);
    }

    public void AddMaterial(float amount)
    {
        UpdateMaterial(currentMaterial + amount);
    }

    public void RemoveMaterial(float amount)
    {
        UpdateMaterial(currentMaterial - amount);
    }
}