# Player Systems

Stats, input, mouvement, caméra. Tout ce qui constitue un joueur jouable et networké.

---

## PlayerStats (networked)

`Assets/Scripts/Player/PlayerStats.cs` — HP, level, XP synchronisés.

```csharp
using Mirror;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHpChanged))]
    public int currentHp;

    [SyncVar] public int maxHp;
    [SyncVar] public int level;
    [SyncVar] public int xp;
    [SyncVar] public int baseDamage;

    [SyncVar] public string classId;

    public override void OnStartServer()
    {
        // appelé après ApplyFromSave côté server
    }

    // Appelé côté server uniquement, depuis EinerjahrNetworkManager après payload reçue
    [Server]
    public void ApplyFromSave(CharacterSave save, ClassDefinition cls)
    {
        classId = save.classId;
        level = save.level;
        xp = save.xp;
        maxHp = cls.baseHp + cls.hpPerLevel * (save.level - 1);
        baseDamage = cls.baseDamage + cls.damagePerLevel * (save.level - 1);
        currentHp = maxHp;
    }

    [Server]
    public void TakeDamage(int dmg, GameObject attacker)
    {
        if (currentHp <= 0) return;
        currentHp = Mathf.Max(0, currentHp - dmg);
        if (currentHp == 0) Die(attacker);
    }

    [Server]
    void Die(GameObject killer)
    {
        RpcOnDeath();
        // TODO: gestion respawn / spectator
    }

    [ClientRpc]
    void RpcOnDeath()
    {
        // anim mort, désactiver collider, etc.
    }

    void OnHpChanged(int oldHp, int newHp)
    {
        // sur les clients (incluant host) — update UI
        if (isLocalPlayer) HUD.Instance.SetHp(newHp, maxHp);
    }
}
```

---

## Input (New Input System)

Le package `com.unity.inputsystem` est déjà installé. Crée un asset d'actions :
- **Project → Create → Input Actions** → nomme-le `PlayerInputActions`
- Actions standard : `Move` (Vector2), `Look` (Vector2), `Jump`, `Fire`, `Cast1`, `Cast2`, `Cast3`, `Cast4`

Génère la classe C# (cocher "Generate C# Class" dans l'inspector de l'asset).

### Lecture dans un script
```csharp
using UnityEngine.InputSystem;

public class PlayerInput : NetworkBehaviour
{
    PlayerInputActions actions;

    void Awake() => actions = new PlayerInputActions();

    public override void OnStartLocalPlayer()
    {
        actions.Enable();
        actions.Player.Fire.performed += _ => OnFire();
        actions.Player.Cast1.performed += _ => OnCast(0);
    }

    void OnDisable() => actions?.Disable();

    void Update()
    {
        if (!isLocalPlayer) return;
        Vector2 move = actions.Player.Move.ReadValue<Vector2>();
        // → passer à PlayerMovement
    }
}
```

---

## Mouvement

Deux approches selon ton autorité préférée :

### A) Client-authoritative (simple, recommandé pour coop)
Le client local bouge librement, sa position est répliquée via `NetworkTransform`.

**Setup** :
- Sur le Player prefab : `NetworkTransform Reliable` (ou `Unreliable` pour smoother) → **Sync Direction = Client To Server**
- Code dans le `Update()` du Player local :

```csharp
void Update()
{
    if (!isLocalPlayer) return;

    Vector2 input = actions.Player.Move.ReadValue<Vector2>();
    Vector3 dir = transform.right * input.x + transform.forward * input.y;
    controller.Move(dir * moveSpeed * Time.deltaTime);
}
```

### B) Server-authoritative (anti-cheat strict)
Le client envoie son input, le server applique le mouvement.

```csharp
void Update()
{
    if (!isLocalPlayer) return;
    Vector2 input = actions.Player.Move.ReadValue<Vector2>();
    CmdMove(input);
}

[Command]
void CmdMove(Vector2 input)
{
    Vector3 dir = transform.right * input.x + transform.forward * input.y;
    controller.Move(dir * moveSpeed * Time.deltaTime);
    // NetworkTransform Server→Client réplique la nouvelle position
}
```

⚠ Server-authoritative pur sans prédiction = mouvement laggy. Pour un coop entre potes, **client-authoritative suffit largement** (Darktide fait pareil pour le mouvement).

---

## CharacterController vs Rigidbody

| | CharacterController | Rigidbody |
|---|---|---|
| Mouvement précis | ✅ | ⚠ |
| Réagit aux forces externes | ❌ | ✅ |
| Glisser / pousser | ❌ | ✅ |
| Performance | Meilleure | Bonne |
| Réplication Mirror | ✅ via NetworkTransform | ✅ via NetworkRigidbody |

Pour un Darktide-like : **CharacterController** est suffisant pour les joueurs, garde Rigidbody pour les projectiles.

---

## Caméra

Première personne :
```csharp
// Camera en enfant du Player prefab, position ~ (0, 1.7, 0)
void Update()
{
    if (!isLocalPlayer) return;
    Vector2 look = actions.Player.Look.ReadValue<Vector2>();
    transform.Rotate(0, look.x * sensitivity, 0);          // yaw (player tourne)
    camTransform.Rotate(-look.y * sensitivity, 0, 0);      // pitch (camera tourne)
}
```

Désactive la caméra et le AudioListener sur les players non-locaux :
```csharp
public override void OnStartLocalPlayer()
{
    cam.enabled = true;
    audioListener.enabled = true;
}
// pour les non-local : cam et audioListener restent désactivés (par défaut)
```

---

## Gérer la mort + respawn

Mort = `currentHp == 0` côté server. Plusieurs options :

### Hard respawn (Mirror)
```csharp
[Server]
void Die(GameObject killer)
{
    var conn = connectionToClient;
    NetworkServer.Destroy(gameObject);
    Invoke(nameof(SpawnReplacement), 5f); // 5s de timer
}

[Server]
void SpawnReplacement()
{
    // recrée le player et appelle AddPlayerForConnection
}
```

### Soft respawn (recommandé Darktide-style)
Player object reste, juste désactivé visuellement + collider off. Re-active à la fin du timer.

```csharp
[Server]
void Die(GameObject killer)
{
    isDowned = true;             // SyncVar
    RpcSetDownedVisuals(true);
    // Allié peut reviver via Cmd : currentHp = 25%, isDowned = false
}
```

---

## Liste de joueurs

Côté server, garde un dictionnaire actif :
```csharp
public class PlayerRegistry : NetworkBehaviour
{
    public static readonly Dictionary<int, PlayerStats> Players = new();

    public override void OnStartServer()
    {
        NetworkServer.OnConnectedEvent += OnConn;
        NetworkServer.OnDisconnectedEvent += OnDisconn;
    }

    [Server]
    public static void Register(int connId, PlayerStats p) => Players[connId] = p;

    [Server]
    void OnDisconn(NetworkConnectionToClient conn) => Players.Remove(conn.connectionId);
}
```

Utile pour : envoyer rewards de fin, broadcast un message à tous, find closest player pour une IA d'enemy.
