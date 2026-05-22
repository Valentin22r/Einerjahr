# Mirror Cheatsheet

Référence rapide pour tout code networké. Modèle = **P2P host-authoritative**.

---

## Modèle d'autorité

- **Server (= host)** = source de vérité. Damage, XP, IA, hits de spells s'exécutent ici.
- **Client** = représentation visuelle + envoie inputs/actions au server.
- En P2P, le host fait tourner **server + client local** en même temps.

### Guards

```csharp
if (isServer)      { /* server only */ }
if (isClient)      { /* tous clients, host inclus */ }
if (isLocalPlayer) { /* mon player local */ }
if (isOwned)       { /* j'ai l'authority sur cet objet */ }

[Server] void X() { } // ignore si pas server
[Client] void Y() { } // ignore si pas client
[ServerCallback] void Update() { } // version sans warning
```

---

## Lifecycle NetworkBehaviour

| Méthode | Quand | Sur qui |
|---|---|---|
| `OnStartServer` | spawn côté server | server only |
| `OnStartClient` | spawn côté client | tous clients (host inclus) |
| `OnStartLocalPlayer` | spawn de TON player | client local only |
| `OnStopServer/Client/LocalPlayer` | despawn | symétrique |

**N'utilise pas `Start()` pour logique réseau** — SyncVars pas encore initialisées.

---

## SyncVar (server → all clients)

```csharp
[SyncVar(hook = nameof(OnHpChanged))]
public int hp = 100;

void OnHpChanged(int oldHp, int newHp)
{
    // Sur les CLIENTS quand server change hp. PAS sur le server qui a set.
    healthBar.SetValue(newHp);
}
```

- Écris uniquement depuis server. Set client = ignoré.
- Types : primitives, Vector3, struct serializable, GameObject (via NetworkIdentity)
- Collections : `SyncList<T>`, `SyncDictionary<K,V>`, `SyncSet<T>`

---

## Command (client → server)

```csharp
[Command]
void CmdCastSpell(int spellId, Vector3 target)
{
    // Sur le SERVER quand local player appelle.
    // Server valide → applique → réplique via SyncVar/ClientRpc.
}
```

- Uniquement appelable par **local player** sur son **propre NetworkBehaviour**
- `[Command(requiresAuthority = false)]` pour autoriser un appel depuis un client non-owner (rare)

---

## ClientRpc (server → tous clients)

```csharp
[ClientRpc]
void RpcPlayHitFX(Vector3 pos)
{
    Instantiate(hitFxPrefab, pos, Quaternion.identity);
}
```

Usage : effets visuels, sons, animation triggers, particle effects.

---

## TargetRpc (server → un client précis)

```csharp
[TargetRpc]
void TargetShowLevelUp(NetworkConnectionToClient target, int newLevel)
{
    levelUpPopup.Show(newLevel);
}
```

Usage : popup individuel, end-of-mission XP report.

---

## Spawn dynamique (projectiles, enemies, loot)

1. Prefab avec `NetworkIdentity`
2. **NetworkManager → Registered Spawnable Prefabs** → drag le prefab
3. Server only :

```csharp
[Server]
void SpawnProjectile(Vector3 pos, Quaternion rot, GameObject owner)
{
    GameObject p = Instantiate(projectilePrefab, pos, rot);
    NetworkServer.Spawn(p, owner); // owner = qui a tiré
}

[Server]
void Despawn(GameObject obj) => NetworkServer.Destroy(obj);
```

---

## Patterns clés Darktide-style

### Client envoie sa data au host
Déjà implémenté dans `EinerjahrNetworkManager`. Le client send un `CharacterPayloadMessage` après connexion, le host instancie le player avec ces stats.

### Enemy host-authoritative
- NetworkIdentity + NetworkTransform Reliable (Sync Direction = **Server To Client**)
- Logique uniquement côté server :
```csharp
void Update()
{
    if (!isServer) return;
    agent.SetDestination(target.position);
}
```

### Spell hitscan
```csharp
[Command]
void CmdFireHitscan(Vector3 origin, Vector3 dir)
{
    if (Physics.Raycast(origin, dir, out var hit, range))
    {
        if (hit.collider.TryGetComponent<EnemyStats>(out var e))
            e.TakeDamage(damage, gameObject);
    }
    RpcFireFX(origin, dir);
}
```

### Spell projectile
```csharp
[Command]
void CmdLaunchProjectile(Vector3 pos, Vector3 dir)
{
    var p = Instantiate(projectilePrefab, pos, Quaternion.LookRotation(dir));
    p.GetComponent<Rigidbody>().velocity = dir * speed;
    NetworkServer.Spawn(p, gameObject);
}
```

### Gains fin de partie
```csharp
[TargetRpc]
void TargetSendRewards(NetworkConnectionToClient target, int xp, string[] loot)
{
    LocalProfile.CurrentCharacter.xp += xp;
    LocalProfile.Save();
}
```

---

## Gotchas

| ❌ | ✅ |
|---|---|
| Modifier SyncVar côté client | Utilise `[Command]` qui set côté server |
| Logique gameplay dans `Update()` sans guard | `if (!isServer) return;` ou `if (!isLocalPlayer) return;` |
| Cmd sur objet non-owner | Cmd sur ton local player avec ID cible en param |
| `Start()` pour init réseau | `OnStartServer/Client/LocalPlayer` |
| `Destroy(obj)` networké | `NetworkServer.Destroy(obj)` côté server |
| Ref par GameObject sans NetworkIdentity | Ref par `NetworkIdentity` ou `connectionId` |
| Set SyncVar dans `OnStartServer` même valeur que default | Force change avec une valeur diff puis remets, OU initialise dans le constructor / field initializer |
