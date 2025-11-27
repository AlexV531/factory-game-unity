using UnityEngine;

public class FloatProgressLights : MonoBehaviour
{
    [Header("Lights to control")]
    public Light[] lights;

    // This function can be called via the UnityEvent
    public void SetProgress(float current, float max)
    {
        if (lights.Length == 0 || max <= 0f) return;

        float ratio = Mathf.Clamp01(current / max);
        int lightsOn = Mathf.RoundToInt(ratio * lights.Length);

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = i < lightsOn;
        }
    }
}
