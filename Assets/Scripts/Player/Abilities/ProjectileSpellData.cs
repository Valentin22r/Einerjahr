using UnityEngine;

[CreateAssetMenu(menuName = "Einerjahr/Spells/Projectile Spell", fileName = "ProjectileSpell_New")]
public class ProjectileSpellData : ScriptableObject
{
    [Header("Identité")]
    public string displayName = "Fireball";

    [Header("Progression")]
    [Tooltip("Si vrai, le spell démarre verrouillé. Appeler SpellCaster.UnlockProjectile pour le débloquer.")]
    public bool startsLocked = false;

    [Header("─── 1. Casting (joué au muzzle) ───")]
    public GameObject castFxPrefab;
    [Tooltip("Échelle visuelle (1 = taille du prefab).")]
    [Min(0.01f)] public float castFxScale = 1f;
    [Tooltip("Vitesse de jeu des particules (1 = normal, 2 = deux fois plus rapide).")]
    [Min(0.05f)] public float castFxSpeed = 1f;
    [Tooltip("Durée avant destruction du FX cast (s).")]
    [Min(0.1f)] public float castFxLifetime = 2f;

    [Header("─── 2. Projectile (en vol) ───")]
    public GameObject projectilePrefab;
    [Min(0.01f)] public float projectileScale = 1f;
    [Tooltip("Vitesse de déplacement (m/s).")]
    [Min(0f)] public float projectileSpeed = 25f;
    [Tooltip("Dégâts directs infligés au premier Damageable touché.")]
    [Min(0f)] public float projectileDamage = 25f;
    [Tooltip("Rayon de détection d'impact.")]
    [Min(0.01f)] public float hitRadius = 0.25f;
    [Tooltip("Durée max en vol avant auto-destruction.")]
    [Min(0.1f)] public float lifeTime = 5f;
    [Tooltip("Cooldown entre deux tirs (s).")]
    [Min(0f)] public float cooldown = 0.35f;
    public LayerMask hitMask = ~0;

    [Header("─── 3. Explosion (à l'impact) ───")]
    public GameObject impactFxPrefab;
    [Min(0.01f)] public float impactFxScale = 1f;
    [Tooltip("Vitesse de jeu des particules d'impact.")]
    [Min(0.05f)] public float impactFxSpeed = 1f;
    [Min(0.1f)] public float impactFxLifetime = 3f;
    [Tooltip("Rayon AoE de l'explosion. 0 = pas d'AoE, seul l'impact direct fait des dégâts.")]
    [Min(0f)] public float explosionRadius = 0f;
    [Tooltip("Dégâts AoE infligés à chaque Damageable dans le rayon d'explosion.")]
    [Min(0f)] public float explosionDamage = 0f;
    public LayerMask explosionMask = ~0;

    [Header("─── Crit (chance par tir) ───")]
    [Range(0f, 1f)] public float critChance = 0.01f;
    [Tooltip("Multiplicateur appliqué à projectileDamage et explosionDamage.")]
    [Min(1f)] public float critDamageMultiplier = 5f;
    [Tooltip("Multiplie la taille du casting, du projectile et de l'explosion.")]
    [Min(1f)] public float critScaleMultiplier = 10f;
    [Tooltip("Multiplie le rayon d'explosion en crit.")]
    [Min(1f)] public float critRadiusMultiplier = 10f;
    [Tooltip("Rayon d'explosion minimum garanti en crit (m). Permet à un spell sans AoE de base d'exploser quand même en crit. 0 = pas d'AoE forcée.")]
    [Min(0f)] public float critMinExplosionRadius = 8f;
}
