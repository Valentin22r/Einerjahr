# Spell System

Architecture pour les spells lancés (projectiles) et tirés directs (hitscan, AOE instantané, buff sur soi).

---

## Modèle de données

### `SpellDefinition` (ScriptableObject)
```csharp
using UnityEngine;

public enum SpellType
{
    Hitscan,     // raycast instantané
    Projectile,  // objet physique lancé
    Self,        // buff/heal sur soi
    AreaInstant  // AOE autour de la cible (explosion immédiate)
}

[CreateAssetMenu(menuName = "Einerjahr/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    public string spellId;
    public string displayName;
    public Sprite icon;
    public SpellType type;

    [Header("Stats")]
    public int baseDamage = 20;
    public float cooldown = 2f;
    public float range = 30f;
    public float manaCost = 10f;

    [Header("Type-specific")]
    public GameObject projectilePrefab; // pour Projectile
    public GameObject hitFxPrefab;      // VFX au point d'impact
    public GameObject castFxPrefab;     // VFX sur le caster pendant le cast
    public float aoeRadius;             // pour AreaInstant
    public float projectileSpeed = 20f;

    [Header("Buff (si Self)")]
    public float buffDuration;
    public float buffMultiplier;
}
```

### `SpellRegistry` (résoudre spellId → définition)
Pareil que `ClassRegistry` (cf. doc 03).

---

## Caster côté joueur

```csharp
using Mirror;
using UnityEngine;

public class SpellCaster : NetworkBehaviour
{
    [SerializeField] SpellRegistry registry;
    [SerializeField] Transform castOrigin; // souvent la main ou la caméra

    // Cooldowns suivis côté server, exposés via SyncDictionary
    readonly SyncDictionary<string, double> cooldowns = new(); // spellId → time ready (NetworkTime.time)

    public void TryCast(string spellId)
    {
        if (!isLocalPlayer) return;
        CmdCast(spellId, castOrigin.position, castOrigin.forward);
    }

    [Command]
    void CmdCast(string spellId, Vector3 origin, Vector3 dir)
    {
        var spell = registry.Get(spellId);
        if (spell == null) return;

        // Cooldown check
        if (cooldowns.TryGetValue(spellId, out double ready) && NetworkTime.time < ready)
            return;

        // TODO: mana check, animation lock, etc.

        cooldowns[spellId] = NetworkTime.time + spell.cooldown;

        switch (spell.type)
        {
            case SpellType.Hitscan:     DoHitscan(spell, origin, dir); break;
            case SpellType.Projectile:  DoProjectile(spell, origin, dir); break;
            case SpellType.Self:        DoSelf(spell); break;
            case SpellType.AreaInstant: DoArea(spell, origin + dir * spell.range); break;
        }

        RpcOnCast(spellId, origin, dir);
    }

    [ClientRpc]
    void RpcOnCast(string spellId, Vector3 origin, Vector3 dir)
    {
        var spell = registry.Get(spellId);
        if (spell.castFxPrefab != null)
            Instantiate(spell.castFxPrefab, origin, Quaternion.LookRotation(dir));
    }
}
```

---

## Hitscan

```csharp
[Server]
void DoHitscan(SpellDefinition spell, Vector3 origin, Vector3 dir)
{
    if (Physics.Raycast(origin, dir, out RaycastHit hit, spell.range))
    {
        if (hit.collider.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(spell.baseDamage, gameObject);

        if (spell.hitFxPrefab != null)
            RpcHitFx(spell.spellId, hit.point, hit.normal);
    }
}

[ClientRpc]
void RpcHitFx(string spellId, Vector3 pos, Vector3 normal)
{
    var spell = registry.Get(spellId);
    Instantiate(spell.hitFxPrefab, pos, Quaternion.LookRotation(normal));
}
```

---

## Projectile

### Le caster spawne
```csharp
[Server]
void DoProjectile(SpellDefinition spell, Vector3 origin, Vector3 dir)
{
    var p = Instantiate(spell.projectilePrefab, origin, Quaternion.LookRotation(dir));
    var proj = p.GetComponent<Projectile>();
    proj.spellId = spell.spellId;
    proj.damage = spell.baseDamage;
    proj.owner = gameObject;
    proj.GetComponent<Rigidbody>().linearVelocity = dir * spell.projectileSpeed;
    NetworkServer.Spawn(p, gameObject);
}
```

### Le projectile lui-même
```csharp
using Mirror;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SyncVar] public string spellId;
    [SyncVar] public int damage;
    public GameObject owner;

    [SerializeField] float lifetime = 5f;
    float spawnedAt;

    public override void OnStartServer() => spawnedAt = (float)NetworkTime.time;

    void Update()
    {
        if (!isServer) return;
        if (NetworkTime.time - spawnedAt > lifetime)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage, owner);
            RpcImpact(transform.position);
            NetworkServer.Destroy(gameObject);
        }
    }

    [ClientRpc]
    void RpcImpact(Vector3 pos) { /* VFX/sound */ }
}
```

**Important** : le prefab `projectilePrefab` doit être :
- Dans **Registered Spawnable Prefabs** du NetworkManager
- Avoir `NetworkIdentity` + `NetworkTransform Reliable` (Server→Client) + `Rigidbody` (kinematic = false) + Collider (Is Trigger = true)

---

## Area instant (explosion AOE)

```csharp
[Server]
void DoArea(SpellDefinition spell, Vector3 center)
{
    Collider[] hits = Physics.OverlapSphere(center, spell.aoeRadius);
    foreach (var c in hits)
    {
        if (c.TryGetComponent<IDamageable>(out var d) && c.gameObject != gameObject)
            d.TakeDamage(spell.baseDamage, gameObject);
    }
    RpcExplosionFx(spell.spellId, center);
}

[ClientRpc]
void RpcExplosionFx(string spellId, Vector3 pos)
{
    var spell = registry.Get(spellId);
    Instantiate(spell.hitFxPrefab, pos, Quaternion.identity);
}
```

---

## Self (buff/heal sur soi)

```csharp
[Server]
void DoSelf(SpellDefinition spell)
{
    var stats = GetComponent<PlayerStats>();
    if (spell.spellId == "heal")
    {
        stats.Heal(spell.baseDamage); // au passage `baseDamage` peut servir de "amount"
    }
    else if (spell.spellId == "haste")
    {
        StartCoroutine(BuffMoveSpeed(spell.buffDuration, spell.buffMultiplier));
    }
}
```

---

## Cooldowns UI

Côté client local, ton HUD lit `SyncDictionary<string, double> cooldowns` du SpellCaster (de son own player) :

```csharp
void Update()
{
    if (!localCaster) return;
    foreach (var pair in localCaster.cooldowns)
    {
        double remaining = pair.Value - NetworkTime.time;
        UpdateSpellIcon(pair.Key, Mathf.Max(0, (float)remaining));
    }
}
```

---

## Interface IDamageable

Standardise comment les choses prennent des dégâts :
```csharp
public interface IDamageable
{
    void TakeDamage(int amount, GameObject attacker);
}
```

Implémente sur : `PlayerStats`, `EnemyStats`, props destructibles, etc.

Cf. [07-combat-damage.md](07-combat-damage.md) pour le pipeline complet (résistances, crits, attribution kill).
