using UnityEngine;
using Dissonance;
using Dissonance.Audio.Playback;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerVoiceProximity : NetworkBehaviour
{
    [Header("Proximity Settings")]
    [SerializeField] private float maxVoiceRange = 50f;
    [SerializeField] private float minVoiceRange = 1f;
    [SerializeField] private AnimationCurve volumeFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Components")]
    [SerializeField] private VoiceBroadcastTrigger broadcastTrigger;
    [SerializeField] private VoiceReceiptTrigger receiptTrigger;
    [SerializeField] private VoicePlayback voicePlayback;
    
    private DissonanceComms dissonanceComms;
    private string roomName = "ProximityRoom";
    private Transform listenerTransform;
    private bool isMuted = false;
    
    private void Awake()
    {
        if (broadcastTrigger == null)
            broadcastTrigger = GetComponent<VoiceBroadcastTrigger>();
        if (receiptTrigger == null)
            receiptTrigger = GetComponent<VoiceReceiptTrigger>();
        if (voicePlayback == null)
            voicePlayback = GetComponent<VoicePlayback>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        dissonanceComms = FindAnyObjectByType<DissonanceComms>();
        
        if (dissonanceComms == null)
        {
            Debug.LogError("DissonanceComms not found in scene!");
            return;
        }
        
        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
    }
    
    public void OnMute(InputValue value)
    {
        if (value.isPressed)
            ToggleMute();
    }
    
    private void SetupLocalPlayer()
    {
        DissonanceNetcodeSetup.RegisterPlayer(OwnerClientId);
        
        if (broadcastTrigger != null)
        {
            broadcastTrigger.RoomName = roomName;
            broadcastTrigger.enabled = true;
        }
        
        if (voicePlayback != null)
        {
            voicePlayback.enabled = false;
        }
        
        SetupAudioListener();
        
        Debug.Log($"Local player voice setup: Player_{OwnerClientId}");
    }
    
    private void SetupRemotePlayer()
    {
        string remotePlayerId = $"Player_{OwnerClientId}";
        
        if (receiptTrigger != null)
        {
            receiptTrigger.RoomName = roomName;
            receiptTrigger.enabled = true;
        }
        
        if (voicePlayback != null)
        {
            voicePlayback.PlayerName = remotePlayerId;
            voicePlayback.enabled = true;
        }
        
        if (broadcastTrigger != null)
        {
            broadcastTrigger.enabled = false;
        }
        
        Debug.Log($"Remote player voice setup: {remotePlayerId}");
    }
    
    private void SetupAudioListener()
    {
        listenerTransform = Camera.main?.transform;
        
        if (listenerTransform == null)
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            if (listener != null)
            {
                listenerTransform = listener.transform;
            }
        }
    }
    
    private void Update()
    {
        if (!IsOwner) return;
        
        UpdateProximityVolumes();
    }
    
    private void UpdateProximityVolumes()
    {
        if (dissonanceComms == null || listenerTransform == null) return;
        
        foreach (var player in dissonanceComms.Players)
        {
            if (player.IsLocalPlayer) continue;
            
            GameObject remotePlayerObj = FindPlayerByClientId(player.Name);
            if (remotePlayerObj == null) continue;
            
            float distance = Vector3.Distance(listenerTransform.position, remotePlayerObj.transform.position);
            
            float normalizedDistance = Mathf.Clamp01(
                (distance - minVoiceRange) / (maxVoiceRange - minVoiceRange)
            );
            
            float volume = volumeFalloff.Evaluate(1f - normalizedDistance);
            
            if (distance > maxVoiceRange)
            {
                volume = 0f;
            }
            
            player.Volume = volume;
        }
    }
    
    private GameObject FindPlayerByClientId(string playerName)
    {
        if (!playerName.StartsWith("Player_")) return null;
        
        string clientIdStr = playerName.Replace("Player_", "");
        if (!ulong.TryParse(clientIdStr, out ulong clientId)) return null;
        
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (netObj.OwnerClientId == clientId && netObj.GetComponent<PlayerVoiceProximity>() != null)
            {
                return netObj.gameObject;
            }
        }
        
        return null;
    }
    
    public void ToggleMute()
    {
        if (broadcastTrigger != null)
        {
            isMuted = !isMuted;
            broadcastTrigger.enabled = !isMuted;
            Debug.Log($"Voice chat {(isMuted ? "muted" : "unmuted")}");
        }
    }
    
    public bool IsMuted()
    {
        return isMuted;
    }
    
    public void SetVoiceRange(float newMaxRange)
    {
        maxVoiceRange = newMaxRange;
    }
}
