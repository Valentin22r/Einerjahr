# Enemy AI

Enemies = entièrement host-authoritative. Le server fait l'IA, les clients reçoivent juste les positions/animations.

---

## Setup d'un enemy prefab

1. **GameObject** avec :
   - `NetworkIdentity`
   - `NetworkTransform Reliable` → Sync Direction = **Server To Client**
   - `NavMeshAgent` (Unity AI)
   - `Animator` (optionnel mais probable)
   - `Collider` (pour hitbox des spells/coups)
   - `Rigidbody` (kinematic = true, sinon NavMeshAgent et physics se battent)
   - `EnemyStats` (notre script — HP, dégâts)
   - `EnemyAI` (notre script — comportement)
2. **Ajouter le prefab à Registered Spawnable Prefabs** du NetworkManager
3. Le **NavMesh doit être bake** sur la scène : Window → AI → Navigation → Bake

---

## EnemyStats (HP, mort, attribution)

```csharp
using Mirror;
using UnityEngine;

public class EnemyStats : NetworkBehaviour, IDamageable
{
    [SerializeField] int maxHpBase = 50;
    [SyncVar(hook = nameof(OnHpChanged))] public int currentHp;
    [SyncVar] public int maxHp;

    public int xpReward = 25;

    GameObject lastAttacker;

    public override void OnStartServer()
    {
        maxHp = maxHpBase;
        currentHp = maxHp;
    }

    [Server]
    public void TakeDamage(int amount, GameObject attacker)
    {
        if (currentHp <= 0) return;
        currentHp = Mathf.Max(0, currentHp - amount);
        lastAttacker = attacker;
        if (currentHp == 0) Die();
    }

    [Server]
    void Die()
    {
        RpcOnDeath();
        if (lastAttacker != null && lastAttacker.TryGetComponent<PlayerStats>(out var p))
            p.AddPendingXp(xpReward); // accumulé pour la fin de mission
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcOnDeath() { /* animation, ragdoll, son */ }

    void OnHpChanged(int oldHp, int newHp) { /* update health bar above head */ }
}
```

---

## EnemyAI (state machine basique)

```csharp
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : NetworkBehaviour
{
    enum State { Idle, Chase, Attack, Dead }

    [SerializeField] float aggroRange = 15f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float attackCooldown = 1.5f;
    [SerializeField] int meleeDamage = 8;

    NavMeshAgent agent;
    State state = State.Idle;
    Transform target;
    double nextAttackTime;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    [ServerCallback]
    void Update()
    {
        if (state == State.Dead) return;

        FindClosestPlayer();

        switch (state)
        {
            case State.Idle:   TickIdle();   break;
            case State.Chase:  TickChase();  break;
            case State.Attack: TickAttack(); break;
        }
    }

    [Server]
    void FindClosestPlayer()
    {
        Transform closest = null;
        float bestDist = aggroRange;
        foreach (var p in PlayerRegistry.Players.Values)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < bestDist) { closest = p.transform; bestDist = d; }
        }
        target = closest;
    }

    [Server]
    void TickIdle()
    {
        if (target != null) state = State.Chase;
    }

    [Server]
    void TickChase()
    {
        if (target == null) { state = State.Idle; agent.ResetPath(); return; }
        agent.SetDestination(target.position);
        if (Vector3.Distance(transform.position, target.position) <= attackRange)
            state = State.Attack;
    }

    [Server]
    void TickAttack()
    {
        if (target == null) { state = State.Idle; return; }
        if (Vector3.Distance(transform.position, target.position) > attackRange)
        {
            state = State.Chase;
            return;
        }
        agent.ResetPath();
        transform.LookAt(target);

        if (NetworkTime.time >= nextAttackTime)
        {
            nextAttackTime = NetworkTime.time + attackCooldown;
            if (target.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(meleeDamage, gameObject);
            RpcAttackAnim();
        }
    }

    [ClientRpc]
    void RpcAttackAnim()
    {
        // GetComponent<Animator>().SetTrigger("Attack");
    }
}
```

---

## Spawn manager (vagues d'enemies)

```csharp
using Mirror;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] int enemiesPerWave = 5;
    [SerializeField] float waveDelay = 30f;

    int currentWave;

    public override void OnStartServer()
    {
        InvokeRepeating(nameof(SpawnWave), 5f, waveDelay);
    }

    [Server]
    void SpawnWave()
    {
        currentWave++;
        for (int i = 0; i < enemiesPerWave; i++)
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var enemy = Instantiate(prefab, point.position, Quaternion.identity);

            // Scale stats avec la wave
            if (enemy.TryGetComponent<EnemyStats>(out var stats))
            {
                stats.maxHp = stats.maxHpBase * (1 + currentWave / 3);
            }

            NetworkServer.Spawn(enemy);
        }
    }
}
```

---

## Pooling (perf si beaucoup d'enemies)

Spawn/Destroy à répétition est coûteux. Pour > 20 enemies actifs en permanence, utilise un pool :

```csharp
// Crée des enemies à l'avance, désactivés
// Recycle via NetworkServer.UnSpawn (au lieu de Destroy) puis SetActive(true) + NetworkServer.Spawn quand besoin
```

Mirror n'a pas de pool intégré, tu peux écrire une `EnemyPool` simple à base de `Queue<GameObject>`.

---

## Sensoriel : vision, audition

Si tu veux des enemies qui détectent par la vue ou le bruit :

### Vision (cone of sight)
```csharp
bool CanSee(Transform t)
{
    Vector3 dir = (t.position - transform.position).normalized;
    if (Vector3.Angle(transform.forward, dir) > visionAngle / 2) return false;

    if (Physics.Raycast(transform.position, dir, out RaycastHit hit, visionRange))
        return hit.transform == t;
    return false;
}
```

### Audition (alerte à un événement de son)
```csharp
// PlayerMovement émet un event quand sprint/saute
public static event System.Action<Vector3, float> OnNoiseEmitted;

// Dans EnemyAI, abonne-toi (uniquement côté server)
void OnEnable() { if (isServer) EnemyAudition.OnNoiseEmitted += React; }
void React(Vector3 pos, float intensity)
{
    if (Vector3.Distance(pos, transform.position) <= intensity)
        target = ...; // investigate
}
```

---

## Gotchas

- **NavMeshAgent et NetworkTransform** : le NavMesh bouge le transform, NetworkTransform le réplique. Si tu mets sync direction sur "Client To Server" → le client va écraser la position décidée par le NavMesh. **Toujours Server → Client**.
- **Rigidbody pas en kinematic** : NavMeshAgent et Rigidbody non-kinematic se battent → enemy qui tremble. Met `isKinematic = true`.
- **Plusieurs enemies overlappant** : NavMeshAgent gère mal les collisions entre agents → utilise l'**Avoidance Quality** + **Stopping Distance** pour atténuer.
- **OnTriggerEnter sur les clients aussi** : `OnTriggerEnter` se déclenche sur tous les clients + server. Si tu veux la logique server-only, utilise `[ServerCallback]` ou guarde avec `if (!isServer) return;`.
- **Enemy mort qui apparaît vivant chez un joueur en lag** : SyncVar `currentHp` arrive avant le RpcOnDeath. Mets la logique de mort dans le hook `OnHpChanged` côté client.
