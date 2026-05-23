using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackCooldown = 1f;

    [Tooltip("Si vrai, ignore les joueurs morts (ne les attaque pas).")]
    public bool ignoreDeadPlayers = true;

    [Tooltip("Détecte automatiquement le player même sans tag. Si décoché, utilise FindGameObjectWithTag.")]
    public bool robustPlayerFind = true;

    float nextAttackTime;
    EnemyStats stats;
    Transform playerTransform;
    PlayerDamage playerDamage;
    PlayerStats playerStats;

    void Start()
    {
        stats = GetComponent<EnemyStats>();
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject p = null;
        if (robustPlayerFind)
        {
            // Cherche par PlayerStats (plus fiable que le tag).
            var ps = Object.FindFirstObjectByType<PlayerStats>();
            if (ps != null) p = ps.gameObject;
        }
        if (p == null) p = GameObject.FindGameObjectWithTag("Player");

        if (p == null)
        {
            Debug.LogWarning($"[EnemyCombat] {name} can't find a player (no PlayerStats and no GameObject tagged 'Player').");
            return;
        }

        playerTransform = p.transform;
        playerDamage = p.GetComponentInChildren<PlayerDamage>();
        if (playerDamage == null) playerDamage = p.GetComponentInParent<PlayerDamage>();
        playerStats = p.GetComponentInChildren<PlayerStats>();
        if (playerStats == null) playerStats = p.GetComponentInParent<PlayerStats>();

        if (playerDamage == null)
            Debug.LogWarning($"[EnemyCombat] Player found ({p.name}) but no PlayerDamage component — enemy can't damage it.");
    }

    void Update()
    {
        if (playerTransform == null) { FindPlayer(); return; }
        if (ignoreDeadPlayers && playerStats != null && playerStats.IsDead) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack()
    {
        if (playerDamage == null || stats == null) return;
        playerDamage.TakeDamage(stats.damage);
    }
}
