using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class WebcamStreamer : NetworkBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    
    // Very low settings for minimal bandwidth
    [SerializeField] private int textureWidth = 160;
    [SerializeField] private int textureHeight = 120;
    [SerializeField] private int fps = 10;
    [SerializeField] private int jpgQuality = 25; // Very low quality
    
    private WebCamTexture webCamTexture;
    private Texture2D tempTexture;
    private Texture2D displayTexture;
    private bool isStreaming = false;

    void Start()
    {
        if (IsOwner)
        {
            StartWebcam();
        }
        else
        {
            // Display texture will be created when we receive first frame
            // This allows us to match the sender's actual resolution
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

        // Request the lowest resolution from webcam
        webCamTexture = new WebCamTexture(devices[deviceIndex].name, textureWidth, textureHeight, fps);
        webCamTexture.Play();
        
        // Wait for webcam to start and get actual resolution
        StartCoroutine(InitializeWebcam());
    }
    
    IEnumerator InitializeWebcam()
    {
        // Wait for webcam to actually start
        yield return new WaitUntil(() => webCamTexture.width > 16);
        
        // Use the actual webcam resolution (it might differ from what we requested)
        int actualWidth = webCamTexture.width;
        int actualHeight = webCamTexture.height;
        
        Debug.Log($"Requested: {textureWidth}x{textureHeight}, Actual: {actualWidth}x{actualHeight}");
        
        // Create texture with actual resolution
        tempTexture = new Texture2D(actualWidth, actualHeight, TextureFormat.RGB24, false);
        
        // Display own webcam locally
        targetRenderer.material.mainTexture = webCamTexture;
        
        isStreaming = true;
        StartCoroutine(StreamWebcam());
    }

    IEnumerator StreamWebcam()
    {
        while (isStreaming)
        {
            if (webCamTexture.didUpdateThisFrame)
            {
                // Copy webcam to Texture2D
                tempTexture.SetPixels(webCamTexture.GetPixels());
                tempTexture.Apply();
                
                // Encode with very low quality - results in ~5-15 KB per frame
                byte[] imageData = tempTexture.EncodeToJPG(jpgQuality);
                
                // Send to other players
                SendWebcamDataServerRpc(imageData);
            }
            
            yield return new WaitForSeconds(1f / fps);
        }
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
                displayTexture = new Texture2D(2, 2); // Will auto-resize on LoadImage
                displayTexture.filterMode = FilterMode.Bilinear;
                targetRenderer.material.mainTexture = displayTexture;
            }
            
            // Decode and display - LoadImage automatically resizes the texture
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
        isStreaming = true;
        StartCoroutine(StreamWebcam());
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
