using System.Collections;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Attaque")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;

    [Tooltip("Si vrai, l'ennemi exécute une attaque 'chute cartoon' (tombe en avant et se relève) au lieu de juste taper sur place.")]
    public bool useLungeAttack = true;

    [Header("Chute cartoon (fall forward)")]
    [Tooltip("Angle de chute en avant (degrés). 75-85° = bien planté face contre terre.")]
    [Range(30f, 90f)] public float fallAngle = 80f;
    [Tooltip("Durée pour tomber (s).")]
    public float fallDuration = 0.25f;
    [Tooltip("Durée pendant laquelle l'ennemi reste planté au sol (s).")]
    public float groundedHold = 0.15f;
    [Tooltip("Durée pour se relever (s).")]
    public float riseDuration = 0.35f;
    [Tooltip("Moment (0..1) de la chute où les dégâts sont infligés (1 = pile à l'impact au sol).")]
    [Range(0f, 1f)] public float damageMoment = 0.9f;

    [Header("Cible")]
    [Tooltip("Si vrai, ignore les joueurs morts (ne les attaque pas).")]
    public bool ignoreDeadPlayers = true;

    [Tooltip("Détecte automatiquement le player même sans tag. Si décoché, utilise FindGameObjectWithTag.")]
    public bool robustPlayerFind = true;

    float nextAttackTime;
    EnemyStats stats;
    EnemyController controller;
    Transform playerTransform;
    PlayerDamage playerDamage;
    PlayerStats playerStats;
    bool lunging;

    void Start()
    {
        stats = GetComponent<EnemyStats>();
        controller = GetComponent<EnemyController>();
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject p = null;
        if (robustPlayerFind)
        {
            var ps = Object.FindFirstObjectByType<PlayerStats>();
            if (ps != null) p = ps.gameObject;
        }
        if (p == null) p = GameObject.FindGameObjectWithTag("Player");

        if (p == null) return;

        playerTransform = p.transform;
        playerDamage = p.GetComponentInChildren<PlayerDamage>();
        if (playerDamage == null) playerDamage = p.GetComponentInParent<PlayerDamage>();
        playerStats = p.GetComponentInChildren<PlayerStats>();
        if (playerStats == null) playerStats = p.GetComponentInParent<PlayerStats>();
    }

    void Update()
    {
        if (lunging) return;
        if (playerTransform == null) { FindPlayer(); return; }
        if (ignoreDeadPlayers && playerStats != null && playerStats.IsDead) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            if (useLungeAttack)
                StartCoroutine(FallForwardAttack());
            else
                Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack()
    {
        if (playerDamage == null || stats == null) return;
        if (controller != null) controller.TriggerAttackAnimation();
        playerDamage.TakeDamage(stats.damage);
    }

    IEnumerator FallForwardAttack()
    {
        if (stats == null) yield break;
        lunging = true;
        if (controller != null) controller.enabled = false;

        Quaternion startRot = transform.rotation;
        Quaternion downRot = startRot * Quaternion.Euler(fallAngle, 0f, 0f);

        // Phase 1 : chute en avant
        float t = 0f;
        bool damageDealt = false;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fallDuration);
            float ease = k * k; // accélération (chute libre)
            transform.rotation = Quaternion.Slerp(startRot, downRot, ease);

            if (!damageDealt && k >= damageMoment)
            {
                ApplyHit();
                damageDealt = true;
            }
            yield return null;
        }
        transform.rotation = downRot;
        if (!damageDealt) { ApplyHit(); damageDealt = true; }

        // Phase 2 : reste planté au sol
        if (groundedHold > 0f) yield return new WaitForSeconds(groundedHold);

        // Phase 3 : se relève
        t = 0f;
        while (t < riseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / riseDuration);
            // décélération (mou de relèvement)
            float ease = 1f - (1f - k) * (1f - k);
            transform.rotation = Quaternion.Slerp(downRot, startRot, ease);
            yield return null;
        }
        transform.rotation = startRot;

        if (controller != null) controller.enabled = true;
        lunging = false;
    }

    void ApplyHit()
    {
        if (playerDamage == null || stats == null) return;
        if (playerTransform == null) return;
        // Hit valide si le joueur est encore à portée raisonnable (un peu de marge pour le "face plant")
        if (Vector3.Distance(transform.position, playerTransform.position) > attackRange + 1f) return;
        playerDamage.TakeDamage(stats.damage);
    }
}
