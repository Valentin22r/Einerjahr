using Mirror;
using UnityEngine;

// Message envoyé par le client au host juste après la connexion.
// Contient le perso (classe + stats sauvegardées) avec lequel le joueur veut entrer en partie.
public struct CharacterPayloadMessage : NetworkMessage
{
    public string characterJson;
}

[AddComponentMenu("Network/Einerjahr Network Manager")]
public class EinerjahrNetworkManager : NetworkManager
{
    // -------- SERVER --------

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<CharacterPayloadMessage>(OnReceiveCharacterPayload, false);
        Debug.Log("[EinerjahrNM] Server started, waiting for client character payloads.");
    }

    // On laisse Auto Create Player désactivé dans l'inspector.
    // Le spawn se fait dans OnReceiveCharacterPayload une fois la payload reçue.
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.LogWarning("[EinerjahrNM] OnServerAddPlayer ignoré (en attente du CharacterPayloadMessage).");
    }

    private void OnReceiveCharacterPayload(NetworkConnectionToClient conn, CharacterPayloadMessage msg)
    {
        if (conn.identity != null)
        {
            Debug.LogWarning($"[EinerjahrNM] Connection {conn.connectionId} a déjà un player object, payload ignorée.");
            return;
        }

        // TODO: désérialise le JSON en CharacterSave (à créer côté gameplay)
        // CharacterSave save = JsonUtility.FromJson<CharacterSave>(msg.characterJson);

        GameObject player = Instantiate(playerPrefab);

        // TODO: applique les stats du perso au PlayerStats (à créer côté gameplay)
        // player.GetComponent<PlayerStats>().ApplyFromSave(save);

        NetworkServer.AddPlayerForConnection(conn, player);

        Debug.Log($"[EinerjahrNM] Player spawné pour conn {conn.connectionId} (payload bytes={msg.characterJson?.Length ?? 0}).");
    }

    // -------- CLIENT --------

    public override void OnClientConnect()
    {
        base.OnClientConnect(); // marque le client comme "ready" sans envoyer l'AddPlayer auto

        // TODO: charge le perso sélectionné dans le menu (depuis Application.persistentDataPath)
        // string json = JsonUtility.ToJson(LocalProfile.SelectedCharacter);
        string json = "{}"; // placeholder tant que la sauvegarde n'existe pas

        NetworkClient.Send(new CharacterPayloadMessage { characterJson = json });
        Debug.Log("[EinerjahrNM] CharacterPayload envoyé au host.");
    }
}
