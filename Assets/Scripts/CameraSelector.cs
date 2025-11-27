using UnityEngine;
using TMPro;

public class CameraSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown cameraDropdown;
    
    void Start()
    {
        PopulateCameraList();
    }
    
    void PopulateCameraList()
    {
        cameraDropdown.ClearOptions();
        
        WebCamDevice[] cameras = WebcamStreamer.GetAvailableCameras();
        
        if (cameras.Length == 0)
        {
            cameraDropdown.AddOptions(new System.Collections.Generic.List<string> { "No cameras found" });
            return;
        }
        
        var options = new System.Collections.Generic.List<string>();
        foreach (var cam in cameras)
        {
            string label = cam.name;
            if (cam.isFrontFacing)
                label += " (Front)";
            options.Add(label);
        }
        
        cameraDropdown.AddOptions(options);
        cameraDropdown.onValueChanged.AddListener(OnCameraSelected);
    }
    
    void OnCameraSelected(int index)
    {
        PhysicsPlayerController.LocalPlayer.webcamStreamer.SwitchCamera(index);
    }
}
