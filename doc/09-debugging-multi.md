# Debugging Multi

Tester un jeu networké quand t'es tout seul, c'est galère. Voici les techniques.

---

## Tester avec 2 instances locales

### Avec AppID 480 (Spacewar — situation actuelle)

Steam refuse 2 instances du même AppID sur le même compte → contournements :

**Méthode 1 : Editor + Build (ça marche ~80% du temps)**
1. Build standalone : `File → Build Settings → Build` dans un dossier
2. Lance le `.exe` du build → ça démarre Steam pour AppID 480 (ou si Steam déjà ouvert, ça l'utilise)
3. Dans Unity, click Play → instance 2

L'Editor et le build sont 2 process différents, Steam les considère comme tels. Parfois ça râle, restart Steam.

**Méthode 2 : Switch User**
- Sur Steam, déconnecte-toi → reconnecte avec un compte secondaire
- Lance ton build avec ce compte, garde l'Editor avec ton compte principal

**Méthode 3 : Famille Steam / un pote**
- Steam Family Sharing partage ta library → un compte ami sur ta machine ou la sienne
- Build → envoie-le → testez en live (le plus réaliste)

---

## ParrelSync (multi-Editor)

Permet d'ouvrir 2 instances Unity Editor sur le **même projet** sans dupliquer les fichiers. Très pratique pour itérer rapidement.

### Install
```
manifest.json :
"com.veriorpies.parrelsync": "https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync"
```

### Usage
1. `Window → ParrelSync → Clones Manager`
2. **Add new clone** → ParrelSync crée un mirroring du projet dans un dossier voisin
3. Open the clone → 2è Editor s'ouvre, partage les Assets/ via symlinks

⚠ **ParrelSync ne contourne pas la limitation Steam AppID 480** → mais utile pour tester du code Mirror **non-Steam** (ex: si tu rajoutes un transport de fallback KCP pour les tests).

---

## Switch transport pour les tests locaux

Tu peux ajouter un transport KCP en parallèle de FizzySteamworks, et basculer dessus pour les tests rapides avec ParrelSync ou Editor+Build.

```csharp
// Sur le NetworkManager GameObject, ajoute aussi un KcpTransport (Mirror inclut ça par défaut).
// Au démarrage, choisis lequel utiliser :

[SerializeField] FizzySteamworks steamTransport;
[SerializeField] KcpTransport kcpTransport;
[SerializeField] bool useSteam = true;

void Awake()
{
    Transport.active = useSteam ? (Transport)steamTransport : kcpTransport;
}
```

Désactive aussi `SteamLobby` quand `useSteam = false`, et utilise le HUD Mirror par défaut pour entrer une IP.

---

## Logs filtering

Avec beaucoup de SyncVar et de spawn, la console se remplit vite.

### Préfixes stricts
Toujours préfixer tes logs : `[EinerjahrNM]`, `[Spell]`, `[Enemy]`, `[Player]`. Tu peux filtrer dans la barre de la Console.

### LogLevel custom
```csharp
public static class DLog
{
    public const bool VERBOSE_NET = false;
    public const bool VERBOSE_AI = false;

    public static void Net(string msg) { if (VERBOSE_NET) Debug.Log("[Net] " + msg); }
    public static void AI(string msg) { if (VERBOSE_AI) Debug.Log("[AI] " + msg); }
}
```

Tu peux toggler par catégorie sans recompiler tout.

---

## Inspecter le réseau

### Mirror NetworkManager HUD
Pas utilisable directement avec Steam (champ IP inutile), mais regarde les `Stats` (Window → Mirror) :
- Bandwidth utilisée
- Packets par seconde
- Nombre de SyncVar dirty par frame

### Wireshark
Si t'as un vrai souci de perf réseau ou tu soupçonnes un mauvais packet, capture le trafic. Steam P2P utilise UDP sur des ports dynamiques, mais Mirror te logge l'IP virtuelle.

---

## Profiler Mirror

```csharp
// Active les stats du NetworkServer
NetworkServer.dontListen = false;  // doit être false
// En play, dans la Console :
Debug.Log($"Connections: {NetworkServer.connections.Count}");
Debug.Log($"Spawned objects: {NetworkIdentity.spawned.Count}");
```

Le Unity Profiler montre aussi un panneau **Network** (Window → Analysis → Profiler → ajoute le module Network).

---

## Bugs Mirror courants

| Symptôme | Cause | Solution |
|---|---|---|
| Player spawné mais invisible chez les autres | NetworkTransform pas sur le prefab | Ajoute le composant + reset position |
| SyncVar pas synced | Pas `[SyncVar]` ou typo dans le hook | Vérifie l'attribut et le nom de la méthode hook |
| Cmd ignorée | Pas appelée par le local player, ou pas l'owner | `[Command(requiresAuthority = false)]` ou appelle depuis local player |
| ClientRpc ne s'exécute pas chez certains | Le NetworkBehaviour n'est pas spawn chez eux | Vérifie que le prefab est dans Registered Spawnable Prefabs |
| "Failed to spawn server object" | Prefab pas enregistré | Idem ci-dessus |
| Désync de position | NetworkTransform Sync Direction mauvais | Player = Client→Server, Enemy = Server→Client |
| Player double sur l'host | Auto Create Player + custom OnServerAddPlayer | Décoche Auto Create Player |

---

## Logging un état réseau

À mettre dans `OnGUI` d'un script de debug pour voir l'état :

```csharp
void OnGUI()
{
    GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 200));
    GUILayout.Label($"Server: {NetworkServer.active}");
    GUILayout.Label($"Client: {NetworkClient.isConnected}");
    GUILayout.Label($"Connections: {NetworkServer.connections.Count}");
    GUILayout.Label($"Spawned: {NetworkIdentity.spawned.Count}");
    GUILayout.Label($"RTT: {NetworkTime.rtt * 1000:F0} ms");
    GUILayout.EndArea();
}
```

---

## Tester la latence simulée

Mirror inclut un transport "Latency Simulator" :
- Add Component → `Latency Simulation` sur le NetworkManager GameObject
- Wraps ton transport actuel → ajoute lag/jitter/loss artificiels
- Indispensable pour vérifier que ton gameplay tient avec 150ms ping

---

## Réseau lent / disconnect inattendu

| Cause | Diag |
|---|---|
| Trop de SyncVar | Profiler → packets par sec très haut |
| ClientRpc spam | Idem |
| Steam pas synchronisé entre les 2 joueurs | Vérifie que les 2 sont sur le même AppID |
| Firewall bloque P2P | Steam gère normalement le NAT punching, mais bloque possible si firewall agressif |

---

## Tester le crash recovery

Quand tu auras la sauvegarde end-of-mission : tue volontairement le process pendant une session, vérifie que :
- Le client crashe ne perd pas ses XP gagnées **avant** le crash (= save fréquente vs save end-of-mission seul)
- Si le **host** crashe, les clients reçoivent un disconnect propre — pas de freeze
- Le lobby Steam ferme proprement (`SteamMatchmaking.LeaveLobby` doit être appelé dans `OnApplicationQuit`)
