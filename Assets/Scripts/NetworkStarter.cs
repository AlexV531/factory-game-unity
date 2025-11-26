using Unity.Netcode;
using UnityEngine;

public class NetworkStarter : MonoBehaviour
{
    void OnGUI()
    {
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUI.Button(new Rect(10, 10, 160, 40), "Start Host"))
                NetworkManager.Singleton.StartHost();

            if (GUI.Button(new Rect(10, 60, 160, 40), "Start Client"))
                NetworkManager.Singleton.StartClient();
        }
    }
}
