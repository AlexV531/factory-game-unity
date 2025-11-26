using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class SimpleNetworkHUD : MonoBehaviour
{
    private string ipAddress = "127.0.0.1";
    private string port = "7777";
    private string playerName = "Player";

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 300));
        GUILayout.BeginVertical("box");

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // Name input
            GUILayout.Label("Player Name:");
            playerName = GUILayout.TextField(playerName, 20);

            GUILayout.Label("IP Address:");
            ipAddress = GUILayout.TextField(ipAddress, 25);

            GUILayout.Label("Port:");
            port = GUILayout.TextField(port, 5);

            if (GUILayout.Button("Host"))
            {
                SavePlayerName();
                SetConnectionData();
                NetworkManager.Singleton.StartHost();
            }
            if (GUILayout.Button("Server"))
            {
                SavePlayerName();
                SetConnectionData();
                NetworkManager.Singleton.StartServer();
            }
            if (GUILayout.Button("Client"))
            {
                SavePlayerName();
                SetConnectionData();
                NetworkManager.Singleton.StartClient();
            }
        }
        else
        {
            if (GUILayout.Button("Shutdown"))
            {
                NetworkManager.Singleton.Shutdown();
            }

            GUILayout.Label($"Transport: {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name}");
            GUILayout.Label($"Mode: {(NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client")}");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void SavePlayerName()
    {
        // Store the chosen name before connecting
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
    }

    private void SetConnectionData()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            ushort parsedPort = 7777;
            ushort.TryParse(port, out parsedPort);
            transport.SetConnectionData(ipAddress, parsedPort);
        }
    }
}