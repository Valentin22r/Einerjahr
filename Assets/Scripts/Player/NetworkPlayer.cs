using Mirror;
using Steamworks;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSteamNameChanged))]
    public string steamName;

    [SyncVar(hook = nameof(OnSteamIdChanged))]
    public ulong steamId;

    public override void OnStartServer()
    {
        steamName = $"Player_{connectionToClient.connectionId}";
    }

    public override void OnStartLocalPlayer()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[NetworkPlayer] SteamManager not initialized in OnStartLocalPlayer");
            return;
        }

        ulong myId = SteamUser.GetSteamID().m_SteamID;
        string myName = SteamFriends.GetPersonaName();
        Debug.Log($"[NetworkPlayer] Local Steam identity → id={myId} name='{myName}' (len={myName?.Length ?? -1})");

        CmdSetSteamIdentity(myId, myName ?? "");
    }

    [Command]
    private void CmdSetSteamIdentity(ulong id, string name)
    {
        steamId = id;
        steamName = string.IsNullOrEmpty(name) ? $"Unknown_{id}" : name;
    }

    private void OnSteamNameChanged(string _, string newName)
    {
        gameObject.name = $"Player [{newName}]";
    }

    private void OnSteamIdChanged(ulong _, ulong newId)
    {
        Debug.Log($"[NetworkPlayer] {steamName} joined (SteamID: {newId})");
    }
}
