using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class WebcamStreamer : NetworkBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    
    // Target low resolution for streaming
    [SerializeField] private int targetWidth = 160;
    [SerializeField] private int targetHeight = 120;
    [SerializeField] private int fps = 10;
    [SerializeField] private int jpgQuality = 25; // Very low quality
    
    private WebCamTexture webCamTexture;
    private Texture2D downscaledTexture;
    private Texture2D displayTexture;
    private bool isStreaming = false;

    void Start()
    {
        if (IsOwner)
        {
            StartWebcam();
        }
    }

    void StartWebcam()
    {
        StartWebcam(0); // Default to first camera
    }
    
    public void StartWebcam(int deviceIndex)
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("No webcam found!");
            return;
        }
        
        // List all available cameras (helpful for debugging)
        Debug.Log($"Found {devices.Length} camera(s):");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"Camera {i}: {devices[i].name} (Front: {devices[i].isFrontFacing})");
        }
        
        // Use specified camera or default to first
        if (deviceIndex < 0 || deviceIndex >= devices.Length)
        {
            deviceIndex = 0;
        }

        // Request low resolution (webcam may ignore this and give higher res)
        webCamTexture = new WebCamTexture(devices[deviceIndex].name, targetWidth, targetHeight, fps);
        webCamTexture.Play();
        
        // Wait for webcam to start
        StartCoroutine(InitializeWebcam());
    }
    
    IEnumerator InitializeWebcam()
    {
        // Wait for webcam to actually start
        yield return new WaitUntil(() => webCamTexture.width > 16);
        
        int actualWidth = webCamTexture.width;
        int actualHeight = webCamTexture.height;
        
        Debug.Log($"Webcam actual resolution: {actualWidth}x{actualHeight}");
        Debug.Log($"Will downscale to: {targetWidth}x{targetHeight}");
        
        // Create downscaled texture at our target resolution
        downscaledTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        downscaledTexture.filterMode = FilterMode.Bilinear;
        
        // Display own webcam locally (downscaled version)
        targetRenderer.material.mainTexture = downscaledTexture;
        
        isStreaming = true;
        StartCoroutine(StreamWebcam());
    }

    IEnumerator StreamWebcam()
    {
        while (isStreaming)
        {
            // Always send at our target framerate, don't wait for didUpdateThisFrame
            // Downscale the webcam texture to target resolution
            DownscaleTexture(webCamTexture, downscaledTexture);
            
            // Encode with very low quality
            byte[] imageData = downscaledTexture.EncodeToJPG(jpgQuality);
            
            // Send to other players
            SendWebcamDataServerRpc(imageData);
            
            yield return new WaitForSeconds(1f / fps);
        }
    }
    
    void DownscaleTexture(WebCamTexture source, Texture2D destination)
    {
        // Get pixels from source
        Color[] sourcePixels = source.GetPixels();
        Color[] destPixels = new Color[destination.width * destination.height];
        
        float xRatio = (float)source.width / destination.width;
        float yRatio = (float)source.height / destination.height;
        
        // Simple nearest-neighbor downscaling (fast)
        for (int y = 0; y < destination.height; y++)
        {
            for (int x = 0; x < destination.width; x++)
            {
                int sourceX = Mathf.FloorToInt(x * xRatio);
                int sourceY = Mathf.FloorToInt(y * yRatio);
                
                int sourceIndex = sourceY * source.width + sourceX;
                int destIndex = y * destination.width + x;
                
                destPixels[destIndex] = sourcePixels[sourceIndex];
            }
        }
        
        destination.SetPixels(destPixels);
        destination.Apply();
    }

    [ServerRpc]
    void SendWebcamDataServerRpc(byte[] data)
    {
        // Broadcast to all clients except sender
        SendWebcamDataClientRpc(data);
    }

    [ClientRpc]
    void SendWebcamDataClientRpc(byte[] data)
    {
        if (!IsOwner)
        {
            // Create display texture on first frame if needed
            if (displayTexture == null)
            {
                displayTexture = new Texture2D(targetWidth, targetHeight);
                displayTexture.filterMode = FilterMode.Bilinear;
                targetRenderer.material.mainTexture = displayTexture;
            }
            
            // Decode and display
            displayTexture.LoadImage(data);
            displayTexture.Apply();
        }
    }

    public override void OnDestroy()
    {
        isStreaming = false;
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
    
    // Get list of all available cameras
    public static WebCamDevice[] GetAvailableCameras()
    {
        return WebCamTexture.devices;
    }
    
    // Switch to a different camera
    public void SwitchCamera(int deviceIndex)
    {
        if (!IsOwner) return;
        
        // Stop current camera
        isStreaming = false;
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
        
        // Start new camera
        StartWebcam(deviceIndex);
    }

    // Optional: Toggle streaming on/off
    public void ToggleStreaming(bool enabled)
    {
        if (!IsOwner) return;
        
        if (enabled && !isStreaming)
        {
            isStreaming = true;
            StartCoroutine(StreamWebcam());
        }
        else if (!enabled && isStreaming)
        {
            isStreaming = false;
        }
    }
}
