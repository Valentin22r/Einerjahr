# Mirror Multi — Cheatsheet Einerjahr

Référence rapide des patterns Mirror que tu vas utiliser. Modèle = **P2P host-authoritative** (le joueur qui host = serveur, les autres = clients).

---

## 1. Modèle d'autorité

- **Server (= host)** = source de vérité. Damage, XP, IA enemies, hits de spells s'exécutent ici.
- **Client** = représentation visuelle + envoie des inputs/actions au server.
- En P2P, le host fait tourner les **deux** rôles simultanément.

### Guards à connaître

```csharp
if (isServer)      { /* uniquement sur le server */ }
if (isClient)      { /* sur tous les clients, host inclus */ }
if (isLocalPlayer) { /* uniquement TON player local */ }
if (isOwned)       { /* l'objet m'appartient (j'ai l'authority) */ }

[Server] void X() { }  // attribut: ignore l'appel si pas server
[Client] void Y() { }  // attribut: ignore l'appel si pas client
```

---

## 2. Lifecycle NetworkBehaviour

| Méthode | Quand | S'exécute sur |
|---|---|---|
| `OnStartServer` | spawn côté server | server only |
| `OnStartClient` | spawn côté client | tous les clients (host inclus) |
| `OnStartLocalPlayer` | spawn de TON player object | client local only |
| `OnStopServer/Client/LocalPlayer` | despawn | symétrique |

**N'utilise pas `Start()` pour la logique réseau** — les SyncVars ne sont pas encore initialisées à ce moment-là. Utilise les hooks ci-dessus.

---

## 3. SyncVar — server pousse une valeur à tous les clients

```csharp
public class PlayerStats : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHpChanged))]
    public int hp = 100;

    void OnHpChanged(int oldHp, int newHp)
    {
        // S'exécute sur les CLIENTS quand le server change hp
        // PAS sur le server lui-même
        UpdateHealthBar(newHp);
    }
}
```

- **Écris uniquement depuis le server**. Modifier sur un client = ignoré.
- Types : primitives, Vector3, struct sérialisable, GameObject (via NetworkIdentity)
- Pour des collections : `SyncList<T>` / `SyncDictionary<K,V>` / `SyncSet<T>`

---

## 4. Command — client → server

```csharp
[Command]
void CmdCastSpell(int spellId, Vector3 target)
{
    // S'exécute sur le SERVER quand le local player appelle ça.
    // Le server valide → applique l'effet → réplique via SyncVar/ClientRpc
}
```

- Appelable **uniquement par le local player sur son propre NetworkBehaviour**.
- Si tu veux qu'un client influence un autre objet, expose une Cmd sur le PlayerObject qui prend l'identifiant cible en paramètre.

---

## 5. ClientRpc — server → tous les clients

```csharp
[ClientRpc]
void RpcPlayHitFX(Vector3 pos)
{
    Instantiate(hitFxPrefab, pos, Quaternion.identity);
}
```

Usage typique : effets visuels, sons, animations triggers, particle effects.

---

## 6. TargetRpc — server → un client précis

```csharp
[TargetRpc]
void TargetShowLevelUp(NetworkConnectionToClient target, int newLevel)
{
    // s'exécute uniquement chez ce client
    levelUpPopup.Show(newLevel);
}
```

Usage : popup individuel, end-of-mission XP report, message d'erreur dirigé.

---

## 7. Spawn dynamique (projectiles, enemies, loot)

1. Crée le prefab avec un `NetworkIdentity` dessus
2. **NetworkManager → Registered Spawnable Prefabs** → drag le prefab
3. Server only :

```csharp
[Server]
void SpawnProjectile(Vector3 pos, Quaternion rot, NetworkConnectionToClient owner)
{
    GameObject p = Instantiate(projectilePrefab, pos, rot);
    NetworkServer.Spawn(p, owner.identity.gameObject); // owner = qui a tiré (pour attribution damage)
}
```

Sans owner : `NetworkServer.Spawn(p);`

Pour despawn : `NetworkServer.Destroy(p);` (ne pas utiliser Destroy direct, ça désync).

---

## 8. Pattern Darktide : client envoie sa data de perso au host

Le client envoie son personnage sélectionné juste après la connexion, **avant** que le host ne spawn son player. Pattern déjà implémenté dans `EinerjahrNetworkManager.cs`.

**Côté client (OnClientConnect)** :
```csharp
string json = JsonUtility.ToJson(monPersoSelectionne);
NetworkClient.Send(new CharacterPayloadMessage { characterJson = json });
```

**Côté server (handler)** :
```csharp
void OnReceiveCharacterPayload(NetworkConnectionToClient conn, CharacterPayloadMessage msg)
{
    var save = JsonUtility.FromJson<CharacterSave>(msg.characterJson);
    GameObject player = Instantiate(playerPrefab);
    player.GetComponent<PlayerStats>().ApplyFromSave(save);
    NetworkServer.AddPlayerForConnection(conn, player);
}
```

**Important** : dans l'inspector du NetworkManager, **décoche Auto Create Player**.

---

## 9. Pattern Enemy host-authoritative

- Prefab Enemy avec `NetworkIdentity` + `NetworkTransform Reliable` → **Sync Direction = Server To Client**
- Logique (mouvement, IA, attaques) tourne **uniquement** sur le server :

```csharp
void Update()
{
    if (!isServer) return; // les clients ne font rien, ils reçoivent juste la position
    agent.SetDestination(currentTarget.position);
}
```

- Damage / mort : `[Server]` only. Animations via ClientRpc pour que tous voient le même effet.

---

## 10. Patterns Spell

### Hitscan (raycast instantané — type sniper, scan, etc.)

```csharp
[Command]
void CmdFireHitscan(Vector3 origin, Vector3 dir)
{
    if (Physics.Raycast(origin, dir, out var hit, range))
    {
        if (hit.collider.TryGetComponent<EnemyStats>(out var e))
            e.TakeDamage(damage, connectionToClient);
    }
    RpcFireFX(origin, dir); // effet visuel pour tous
}

[ClientRpc]
void RpcFireFX(Vector3 origin, Vector3 dir) { /* tracer, muzzle flash */ }
```

### Projectile (boule de feu, flèche, etc.)

```csharp
[Command]
void CmdLaunchProjectile(Vector3 pos, Vector3 dir)
{
    GameObject p = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(dir));
    p.GetComponent<Rigidbody>().velocity = dir * speed;
    NetworkServer.Spawn(p, gameObject); // owner = ce player
}
```

Le projectile a son propre script avec `OnTriggerEnter` côté server qui applique damage + se détruit via `NetworkServer.Destroy`.

---

## 11. Persistance fin de partie (Darktide-style)

À la fin de la mission, le host renvoie à chaque joueur ses gains :

```csharp
[TargetRpc]
void TargetSendSessionRewards(NetworkConnectionToClient target, int xpGained, string[] loot)
{
    // côté client : applique au CharacterSave local + sauvegarde JSON
    LocalProfile.CurrentCharacter.xp += xpGained;
    LocalProfile.Save(); // écrit dans Application.persistentDataPath
}
```

---

## 12. Gotchas

| ❌ Erreur | ✅ Solution |
|---|---|
| Modifier une SyncVar côté client | Utilise une `[Command]` qui demande au server de modifier |
| Logique gameplay dans `Update()` sans guard | Ajoute `if (!isServer) return;` ou `if (!isLocalPlayer) return;` |
| Cmd appelée sur un objet qu'on n'owne pas | Expose la Cmd sur le local player et passe l'identité cible en paramètre |
| `Start()` pour init réseau | Utilise `OnStartServer`/`OnStartClient`/`OnStartLocalPlayer` |
| `Destroy()` un objet networké | `NetworkServer.Destroy(obj)` (uniquement côté server) |
| Référencer un autre joueur par GameObject | Référence par `NetworkIdentity` ou `connectionId` |

---

## 13. Mapping vers ton modèle Darktide

| Élément gameplay | Où ça vit | Pattern Mirror |
|---|---|---|
| `CharacterSave` (level, XP, classe) | Client (JSON local) | envoyé au host via `CharacterPayloadMessage` |
| `PlayerStats` (HP courant, XP de la run) | Server | SyncVar avec hook côté client pour UI |
| Input mouvement | Client local | CmdMove ou NetworkTransform en Client-to-Server |
| Cast de spell | Client local → server | `[Command]` puis `[ClientRpc]` pour FX |
| Enemy IA | Server only | `if (!isServer) return;` + NetworkTransform Server-to-Client |
| Damage / mort enemy | Server | SyncVar `hp`, ClientRpc death anim |
| Loot drop | Server | `NetworkServer.Spawn` du prefab loot |
| Gains de fin de mission | Server → chaque client | `[TargetRpc]` puis sauvegarde JSON locale |
