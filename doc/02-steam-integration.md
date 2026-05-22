# Steam Integration

Comment fonctionne l'intégration Steam dans le projet et comment l'étendre.

---

## Architecture actuelle

```
SteamManager (Steamworks.NET)
    ↓ initialise SteamAPI au démarrage
SteamLobby.cs
    ↓ crée/rejoint des lobbies Steam
NetworkManager (EinerjahrNetworkManager)
    ↓ démarre Mirror host/client
FizzySteamworks Transport
    ↓ utilise SteamID comme adresse réseau
Mirror gameplay
```

`steam_appid.txt` à la racine = AppID utilisé en dev (480 = Spacewar pour tester sans avoir payé Steam Direct).

---

## SteamLobby — flow

### Côté host
```csharp
SteamLobby.Instance.HostLobby();
// → SteamMatchmaking.CreateLobby(FriendsOnly, maxConn)
// → callback OnLobbyCreated
//     → NetworkManager.StartHost()
//     → SetLobbyData("HostAddress", monSteamID)
```

### Côté client (invité)
```
Friend click "Join Game" dans l'overlay Steam
→ callback GameLobbyJoinRequested
→ SteamMatchmaking.JoinLobby(lobbyId)
→ callback LobbyEntered
→ Lis "HostAddress" du lobby
→ NetworkManager.networkAddress = hostSteamID
→ NetworkManager.StartClient()
```

---

## Steam Lobby Data — étendre

Le lobby Steam peut stocker des **clés/valeurs** que tous les joueurs voient. Utile pour : nom de mission, difficulté, slots libres, état (in-lobby vs in-game).

```csharp
// Host set
SteamMatchmaking.SetLobbyData(lobbyId, "mission", "swamp_01");
SteamMatchmaking.SetLobbyData(lobbyId, "difficulty", "hard");

// Client read (n'importe quand)
string mission = SteamMatchmaking.GetLobbyData(lobbyId, "mission");
```

Limites : 255 caractères par valeur, ~250 clés max.

---

## Inviter un ami

Plusieurs méthodes :

### Via overlay Steam (le plus simple)
Tu as déjà ça : ton ami clique "Join Game" sur ton profil Steam, le callback `GameLobbyJoinRequested` se déclenche.

### Via bouton in-game
```csharp
SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(SteamLobby.Instance.CurrentLobbyID));
// ouvre la dialog Steam d'invite, l'ami clique "Join"
```

### Rich Presence (status visible chez les amis)
```csharp
SteamFriends.SetRichPresence("status", "En mission - Swamp");
SteamFriends.SetRichPresence("connect", "+connect_lobby " + lobbyId);
```

---

## Steam Friends — récupérer ses amis

```csharp
int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
for (int i = 0; i < count; i++)
{
    CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
    string name = SteamFriends.GetFriendPersonaName(friendId);
    EPersonaState state = SteamFriends.GetFriendPersonaState(friendId);
    // state : Online, Offline, Busy, Away, Snooze, LookingToTrade, LookingToPlay
}
```

Pour afficher l'avatar : `SteamFriends.GetSmallFriendAvatar(friendId)` → renvoie un handle qu'il faut convertir en `Texture2D` (cf. `SteamUtils.GetImageRGBA`).

---

## Achievements (plus tard, quand AppID réel)

```csharp
// Débloquer
SteamUserStats.SetAchievement("ACH_FIRST_KILL");
SteamUserStats.StoreStats(); // important: persiste

// Vérifier
SteamUserStats.GetAchievement("ACH_FIRST_KILL", out bool unlocked);
```

Les achievements doivent être déclarés côté Steamworks backend (interface web).

---

## Steam Cloud — sauvegarder en cross-machine

Une fois activé dans le Steamworks backend :

```csharp
// Écrire
byte[] data = Encoding.UTF8.GetBytes(jsonProfile);
SteamRemoteStorage.FileWrite("profile.json", data, data.Length);

// Lire
int size = SteamRemoteStorage.GetFileSize("profile.json");
byte[] buffer = new byte[size];
SteamRemoteStorage.FileRead("profile.json", buffer, size);
string json = Encoding.UTF8.GetString(buffer);
```

Quota par défaut : 100 Mo, configurable dans le backend.

---

## Build & Deploy Steam (quand tu auras un vrai AppID)

### Étapes
1. Payer Steam Direct (100€) → t'obtiens un AppID
2. Configurer ton AppID dans le backend (nom, dépôts, plateformes)
3. **Remplace `480` par ton AppID** dans `steam_appid.txt`
4. **Modifie `SteamManager.cs`** : remplace `k_uAppIdInvalid` par ton AppID (en dur) → ça empêche le piratage basique car le jeu refusera de lancer sans le bon AppID
5. Build Unity → upload via Steamworks SDK steamcmd (depot upload)

### Tester en build avant de release
Steam te permet de mettre ton jeu en **alpha branch** accessible uniquement avec un code → invite tes testeurs sans rendre public.

---

## Différences AppID 480 vs vrai AppID

| | 480 (Spacewar) | Vrai AppID |
|---|---|---|
| Lobby Steam | ✅ | ✅ |
| P2P transport | ✅ | ✅ |
| Pseudo/avatar | ✅ | ✅ |
| Achievements | ❌ (partagés/cassés) | ✅ |
| Steam Cloud | ❌ | ✅ |
| Rich presence | partiel | ✅ |
| Launch via Steam library | ❌ | ✅ |

---

## Tester en multi sans 2 machines

Avec AppID 480, Steam **bloque** le lancement de 2 instances du même AppID sur le même compte. Solutions :

1. **Build standalone + Editor** : lance un build, puis dans l'Editor click Play → ça marche car Steam considère que c'est deux process différents (parfois)
2. **Un pote** : la vraie solution, build et envoie-lui
3. **2 comptes Steam** : login sur Steam → Switch User
4. **ParrelSync** (Unity package) : permet d'avoir 2 Editor Unity ouverts sur le même projet (mais même AppID = problème Steam)

Plus en détail dans [09-debugging-multi.md](09-debugging-multi.md).

---

## Bugs Steam connus

| Symptôme | Cause | Solution |
|---|---|---|
| `SteamAPI_Init failed` | Steam pas lancé | Lance Steam + login |
| `Cannot find steam_api64.dll` | Plugin pas trouvé | Vérifie `Assets/Plugins/Steamworks.NET/redistributable_bin/win64/` |
| Lobby créé mais aucun ami ne le voit | Lobby type = Private au lieu de FriendsOnly | Check `ELobbyType` dans `HostLobby()` |
| Friend click "Join" → rien | Pas de gestion du `GameLobbyJoinRequested` | Vérifie que SteamLobby est dans la scène au démarrage |
