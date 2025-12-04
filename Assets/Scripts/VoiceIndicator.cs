using UnityEngine;

public class VoiceIndicator : MonoBehaviour
{
    [SerializeField] private GameObject muteIcon;
    [SerializeField] private float updateInterval = 0.2f;
    
    private PlayerVoiceProximity localPlayerVoiceProximity;
    private float nextUpdateTime;
    
    private void Start()
    {
        if (muteIcon != null)
        {
            muteIcon.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (muteIcon == null) return;
        
        // Try to find the local player's voice component if we don't have it yet
        if (localPlayerVoiceProximity == null)
        {
            if (PhysicsPlayerController.LocalPlayer != null)
            {
                localPlayerVoiceProximity = PhysicsPlayerController.LocalPlayer.GetComponent<PlayerVoiceProximity>();
            }
            return; // Wait until next frame to start updating
        }
        
        // Update the mute icon state
        if (Time.time >= nextUpdateTime)
        {
            muteIcon.SetActive(localPlayerVoiceProximity.IsMuted());
            nextUpdateTime = Time.time + updateInterval;
        }
    }
}
