using UnityEngine;

public class ProgressLights : MonoBehaviour
{
    [Header("Lights to control")]
    public Light[] lights;

    void Start()
    {
        SetProgress(0, 1);
    }

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

    public void SetProgress(int current, int max)
    {
        if (lights.Length == 0 || max <= 0) return;

        float ratio = Mathf.Clamp01((float)current / max);
        int lightsOn = Mathf.RoundToInt(ratio * lights.Length);

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = i < lightsOn;
        }
    }
}
