using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlashingLight : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashSpeed = 2f;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 1f;

    private Light lightComponent;
    private bool isFlashing = false;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        lightComponent.intensity = minIntensity;
    }

    private void Update()
    {
        if (isFlashing)
        {
            float intensity = Mathf.Lerp(minIntensity, maxIntensity,
                (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f);
            lightComponent.intensity = intensity;
        }
    }

    public void StartFlashing()
    {
        isFlashing = true;
    }

    public void StopFlashing()
    {
        isFlashing = false;
        lightComponent.intensity = minIntensity;
    }
}
