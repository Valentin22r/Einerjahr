# Combat & Damage

Pipeline de dégâts unifié pour spells, melee, projectiles, dot, etc.

---

## Interface `IDamageable`

```csharp
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
    bool IsAlive { get; }
}
```

Tout ce qui peut être blessé l'implémente : `PlayerStats`, `EnemyStats`, `DestructibleProp`.

---

## Struct `DamageInfo`

Plutôt que `TakeDamage(int amount, GameObject attacker)`, encapsule dans une struct pour pouvoir étendre sans changer toutes les signatures.

```csharp
public enum DamageType
{
    Physical,
    Fire,
    Frost,
    Lightning,
    Poison,
    Holy,
    True // ignore toutes résistances
}

public struct DamageInfo
{
    public int amount;
    public DamageType type;
    public GameObject attacker;      // qui inflige
    public GameObject source;        // arme/spell/projectile origine
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public bool isCrit;
    public bool isKnockback;
}
```

---

## Pipeline serveur

```csharp
[Server]
public void TakeDamage(DamageInfo info)
{
    if (currentHp <= 0) return;

    // 1. Application des résistances
    int mitigated = ApplyResistances(info);

    // 2. Crit (decidé soit par le caster en amont, soit ici)
    if (info.isCrit) mitigated = Mathf.RoundToInt(mitigated * 1.5f);

    // 3. Apply
    currentHp = Mathf.Max(0, currentHp - mitigated);

    // 4. Feedback visuel (damage number, hit sound, blood)
    RpcShowDamage(mitigated, info.hitPoint, info.isCrit);

    // 5. Mort
    if (currentHp == 0) Die(info);
}

int ApplyResistances(DamageInfo info)
{
    if (info.type == DamageType.True) return info.amount;
    float resist = GetResistance(info.type); // 0..1, ex 0.3 = -30% damage
    return Mathf.RoundToInt(info.amount * (1f - resist));
}
```

---

## Résistances (depuis ClassDefinition ou Equipment)

```csharp
[SyncVar] public float resistFire;
[SyncVar] public float resistFrost;
// etc.

float GetResistance(DamageType t) => t switch
{
    DamageType.Fire => resistFire,
    DamageType.Frost => resistFrost,
    _ => 0f
};
```

Tu peux aussi avoir un `SyncDictionary<DamageType, float>` si beaucoup de types.

---

## Crits

Décide où le crit est rollé : **au moment du cast/hit côté server**.

```csharp
[Server]
void DoHitscan(SpellDefinition spell, Vector3 origin, Vector3 dir)
{
    if (Physics.Raycast(origin, dir, out RaycastHit hit, spell.range))
    {
        float critRoll = Random.value;
        bool crit = critRoll < critChance;

        var dmg = new DamageInfo
        {
            amount = spell.baseDamage,
            type = spell.damageType,
            attacker = gameObject,
            source = spell,  // si tu peux ref le SO
            hitPoint = hit.point,
            hitNormal = hit.normal,
            isCrit = crit
        };

        if (hit.collider.TryGetComponent<IDamageable>(out var t))
            t.TakeDamage(dmg);
    }
}
```

---

## Damage over time (DoT)

Pour poison / burning / bleed :

```csharp
[Server]
public void ApplyDot(DamageType type, int tickDamage, float interval, float duration)
{
    StartCoroutine(DotCoroutine(type, tickDamage, interval, duration));
}

System.Collections.IEnumerator DotCoroutine(DamageType type, int dmg, float interval, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration && currentHp > 0)
    {
        yield return new WaitForSeconds(interval);
        elapsed += interval;
        TakeDamage(new DamageInfo { amount = dmg, type = type, attacker = null });
    }
}
```

---

## Knockback / impulse

Pour faire reculer la cible au hit (utile sur les spells lourds) :

```csharp
[Server]
void ApplyKnockback(Vector3 dir, float force)
{
    // Si tu utilises CharacterController :
    // accumule un vector velocity côté server, applique dans Update
    // OU si Rigidbody:
    rb.AddForce(dir.normalized * force, ForceMode.Impulse);
}

[ClientRpc]
void RpcSyncKnockback(Vector3 dir, float force)
{
    // sur le client local pour reactivité immédiate (anti-lag)
}
```

⚠ Knockback sur des **players** synchronisés via NetworkTransform = peut être saccadé. Le mieux est que le knockback soit calculé côté server **et** côté client local (en parallèle), puis le NetworkTransform reconcile.

---

## Damage numbers (UI feedback)

```csharp
[ClientRpc]
void RpcShowDamage(int amount, Vector3 worldPos, bool crit)
{
    var go = Instantiate(damageNumberPrefab, worldPos, Quaternion.identity);
    go.GetComponent<DamageNumber>().Setup(amount, crit);
}
```

Le prefab `damageNumberPrefab` = un text world-space qui s'élève + fade out en 1s.

---

## Friendly fire

Décide au démarrage : peut-on blesser ses alliés ? Pour un coop PvE Darktide-style, généralement **partial friendly fire** (les hitscan/AOE blessent un peu, le melee non).

```csharp
[Server]
public void TakeDamage(DamageInfo info)
{
    if (IsAlly(info.attacker))
    {
        if (info.type == DamageType.Physical && info.source.IsMelee) return; // pas de FF melee
        info.amount = Mathf.RoundToInt(info.amount * 0.3f); // 30% FF pour le reste
    }
    // ... reste du pipeline
}
```

---

## Threat / aggro (optionnel, type tank)

Si tu veux que certains spells génèrent plus d'aggro (ce qui fait qu'un enemy cible préférentiellement un joueur) :

```csharp
public class EnemyAI : NetworkBehaviour
{
    Dictionary<GameObject, float> threatTable = new();

    [Server]
    public void AddThreat(GameObject player, float amount)
    {
        if (!threatTable.ContainsKey(player)) threatTable[player] = 0;
        threatTable[player] += amount;
    }

    [Server]
    Transform FindHighestThreat()
    {
        GameObject best = null; float max = 0;
        foreach (var pair in threatTable)
            if (pair.Value > max) { max = pair.Value; best = pair.Key; }
        return best?.transform;
    }
}
```

Chaque hit ajoute du threat = damage. Compétences "taunt" du tank = ajoutent gros threat.

---

## Attribution de kill (pour XP/loot)

Le `lastAttacker` simple suffit pour 90% des cas. Pour gérer l'assist (aider quelqu'un à kill) :

```csharp
public class EnemyStats
{
    Dictionary<GameObject, int> damageContributors = new();

    [Server]
    public void TakeDamage(DamageInfo info)
    {
        if (info.attacker != null)
        {
            if (!damageContributors.ContainsKey(info.attacker))
                damageContributors[info.attacker] = 0;
            damageContributors[info.attacker] += info.amount;
        }
        // ... reste
    }

    [Server]
    void Die(DamageInfo killingBlow)
    {
        int total = damageContributors.Values.Sum();
        foreach (var pair in damageContributors)
        {
            float share = (float)pair.Value / total;
            int xp = Mathf.RoundToInt(xpReward * share);
            if (pair.Key.TryGetComponent<PlayerStats>(out var ps))
                ps.AddPendingXp(xp);
        }
    }
}
```

---

## Gotchas

- **`TakeDamage` côté client** : si tu l'appelles depuis un client (collision détectée localement), tu vas désync. Toujours `[Server]` + appel via Cmd.
- **Floating damage numbers en double** : si le caster est aussi le host, le ClientRpc s'exécute aussi chez lui → pas de double. Mais si tu spawn un VFX dans le Cmd ET le Rpc → double sur le host. Garde un seul des deux.
- **Race condition `currentHp <= 0`** : un enemy peut prendre 2 hits dans la même frame avant de mourir. Le check `if (currentHp <= 0) return;` évite que 2 Die() se lancent.
- **Sync de coup vs sync de HP** : le client local peut voir le coup ARRIVER avant que la SyncVar HP change → désync visuel. Pour de la réactivité, déclenche les VFX dans le Rpc, pas dans le hook SyncVar.
