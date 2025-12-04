using UnityEngine;
using TMPro;
using Dissonance;
using System.Collections.Generic;

public class MicrophoneSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown microphoneDropdown;
    [SerializeField] private DissonanceComms dissonanceComms;

    private void Start()
    {
        if (dissonanceComms == null)
        {
            dissonanceComms = FindAnyObjectByType<DissonanceComms>();
        }

        if (dissonanceComms == null)
        {
            Debug.LogError("DissonanceComms not found! Add it to the scene.");
            return;
        }

        PopulateMicrophoneList();
    }

    private void PopulateMicrophoneList()
    {
        microphoneDropdown.ClearOptions();

        string[] microphones = Microphone.devices;

        if (microphones.Length == 0)
        {
            microphoneDropdown.AddOptions(new List<string> { "No microphones found" });
            return;
        }

        var options = new List<string>();
        foreach (var mic in microphones)
        {
            options.Add(mic);
        }

        microphoneDropdown.AddOptions(options);

        SetCurrentMicrophone();

        microphoneDropdown.onValueChanged.AddListener(OnMicrophoneSelected);
    }

    private void SetCurrentMicrophone()
    {
        if (dissonanceComms == null) return;

        string currentMic = dissonanceComms.MicrophoneName;

        if (string.IsNullOrEmpty(currentMic))
        {
            microphoneDropdown.value = 0;
            return;
        }

        for (int i = 0; i < microphoneDropdown.options.Count; i++)
        {
            if (microphoneDropdown.options[i].text == currentMic)
            {
                microphoneDropdown.value = i;
                break;
            }
        }
    }

    private void OnMicrophoneSelected(int index)
    {
        if (dissonanceComms == null) return;

        if (index < 0 || index >= Microphone.devices.Length)
        {
            Debug.LogWarning("Invalid microphone index selected");
            return;
        }

        string selectedMic = Microphone.devices[index];

        dissonanceComms.MicrophoneName = selectedMic;

        Debug.Log($"Microphone changed to: {selectedMic}");
    }

    public void RefreshMicrophoneList()
    {
        PopulateMicrophoneList();
    }
}