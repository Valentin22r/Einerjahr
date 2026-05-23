using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Damageable : MonoBehaviour
{
    [Header("Santé")]
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("Intégration joueur / ennemi")]
    [Tooltip("Si vrai et qu'un PlayerDamage est trouvé en parent, les dégâts y sont forwardés (déclenche rage et Die du joueur).")]
    public bool forwardToPlayerDamage = true;
    [Tooltip("Si vrai et qu'un EnemyStats est trouvé en parent, les dégâts y sont forwardés (déclenche TakeDamage et Die de l'ennemi).")]
    public bool forwardToEnemyStats = true;

    PlayerDamage cachedPlayerDamage;
    EnemyStats cachedEnemyStats;

    [Header("Sang en cours de combat")]
    [Tooltip("Prefab de sang spawned à chaque coup reçu (KriptoFX VolumetricBloodFX).")]
    public GameObject hitBloodPrefab;
    [Tooltip("Durée de vie de chaque instance de sang d'impact (s).")]
    public float hitBloodLifetime = 12f;

    [Header("Mort — explosion de sang")]
    [Tooltip("Prefab de sang utilisé pour l'explosion mortelle. Si vide, hitBloodPrefab est réutilisé.")]
    public GameObject deathBloodPrefab;
    [Tooltip("Nombre d'instances de sang spawned à la mort.")]
    public int deathBloodCount = 6;
    [Tooltip("Rayon de dispersion autour du centre de la cible.")]
    public float deathBloodSpread = 1.4f;
    [Tooltip("Durée avant que la cible ne soit détruite (laisse le sang apparaître).")]
    public float despawnDelay = 0.05f;

    public UnityEvent OnDeath;

    public bool IsDead { get; private set; }

    void Awake()
    {
        if (forwardToPlayerDamage) cachedPlayerDamage = GetComponentInParent<PlayerDamage>();
        if (forwardToEnemyStats) cachedEnemyStats = GetComponentInParent<EnemyStats>();
    }

    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDead || amount <= 0f) return;

        BloodSpawner.Spawn(hitBloodPrefab, hitPoint, hitNormal, hitBloodLifetime);

        if (cachedPlayerDamage != null)
        {
            cachedPlayerDamage.TakeDamage(amount);
            return;
        }

        if (cachedEnemyStats != null)
        {
            cachedEnemyStats.TakeDamage(Mathf.RoundToInt(amount));
            return;
        }

        health -= amount;
        if (health <= 0f) Die();
    }

    void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();

        GameObject prefab = deathBloodPrefab != null ? deathBloodPrefab : hitBloodPrefab;
        Vector3 center = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < deathBloodCount; i++)
        {
            Vector2 disk = Random.insideUnitCircle * deathBloodSpread;
            Vector3 offset = new Vector3(disk.x, Random.Range(0f, 0.6f), disk.y);
            Vector3 normal = new Vector3(disk.x, 1f, disk.y).normalized;
            BloodSpawner.Spawn(prefab, center + offset, normal, hitBloodLifetime);
        }

        Destroy(gameObject, despawnDelay);
    }
}
