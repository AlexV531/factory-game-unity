using UnityEngine;
using Dissonance;

public class DissonanceNetcodeSetup : MonoBehaviour
{
    [SerializeField] private DissonanceComms dissonanceComms;

    private static DissonanceNetcodeSetup instance;

    private void Awake()
    {
        instance = this;

        if (dissonanceComms == null)
        {
            dissonanceComms = FindAnyObjectByType<DissonanceComms>();
        }

        if (dissonanceComms == null)
        {
            Debug.LogError("DissonanceComms not found! Add it to the scene.");
            return;
        }
    }

    private void Start()
    {
        if (dissonanceComms != null)
        {
            dissonanceComms.OnPlayerJoinedSession += OnPlayerJoined;
            dissonanceComms.OnPlayerLeftSession += OnPlayerLeft;
        }
    }

    public static void RegisterPlayer(ulong clientId)
    {
        if (instance == null || instance.dissonanceComms == null) return;

        string uniquePlayerId = $"Player_{clientId}";

        try
        {
            instance.dissonanceComms.LocalPlayerName = uniquePlayerId;
            Debug.Log($"Set player name: {uniquePlayerId}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not set player name (Dissonance may have already started): {e.Message}");
        }
    }

    private void OnPlayerJoined(VoicePlayerState player)
    {
        Debug.Log($"Player joined voice: {player.Name}");
    }

    private void OnPlayerLeft(VoicePlayerState player)
    {
        Debug.Log($"Player left voice: {player.Name}");
    }

    private void OnDestroy()
    {
        if (dissonanceComms != null)
        {
            dissonanceComms.OnPlayerJoinedSession -= OnPlayerJoined;
            dissonanceComms.OnPlayerLeftSession -= OnPlayerLeft;
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}
