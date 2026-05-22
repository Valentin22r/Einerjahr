using Mirror;
using Steamworks;
using UnityEngine;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance { get; private set; }

    private const string HostAddressKey = "HostAddress";

    private NetworkManager networkManager;

    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> joinRequest;
    private Callback<LobbyEnter_t> lobbyEntered;

    public ulong CurrentLobbyID { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();

        if (!SteamManager.Initialized)
        {
            Debug.LogError("[SteamLobby] SteamManager not initialized. Is Steam client running?");
            return;
        }

        lobbyCreated  = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        joinRequest   = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        lobbyEntered  = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    public void LeaveLobby()
    {
        if (CurrentLobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
            CurrentLobbyID = 0;
        }

        if (NetworkServer.active && NetworkClient.isConnected) networkManager.StopHost();
        else if (NetworkClient.isConnected)                    networkManager.StopClient();
        else if (NetworkServer.active)                         networkManager.StopServer();
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[SteamLobby] Lobby creation failed: {callback.m_eResult}");
            return;
        }

        CurrentLobbyID = callback.m_ulSteamIDLobby;

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressKey,
            SteamUser.GetSteamID().ToString());

        Debug.Log($"[SteamLobby] Lobby created: {CurrentLobbyID}");
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = callback.m_ulSteamIDLobby;

        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        Debug.Log($"[SteamLobby] Joined lobby {CurrentLobbyID}, connecting to host {hostAddress}");
    }
}
