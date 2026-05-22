using Mirror;
using UnityEngine;

public class SteamLobbyTestHUD : MonoBehaviour
{
    private SteamLobby steamLobby;

    private void Awake()
    {
        steamLobby = GetComponent<SteamLobby>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 220, 200));
        GUILayout.Label("Steam Lobby Test");

        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (GUILayout.Button("Host Steam Lobby"))
                steamLobby.HostLobby();

            GUILayout.Label("Join: ask friend to invite\nvia Steam overlay (Shift+Tab)");
        }
        else
        {
            GUILayout.Label(NetworkServer.active ? "Hosting" : "Connected as client");
            GUILayout.Label($"Lobby ID: {steamLobby.CurrentLobbyID}");

            if (GUILayout.Button("Leave"))
                steamLobby.LeaveLobby();
        }

        GUILayout.EndArea();
    }
}
