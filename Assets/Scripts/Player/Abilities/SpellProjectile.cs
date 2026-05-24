using System.Collections.Generic;
using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    public float speed = 25f;
    public float lifeTime = 5f;
    public float damage = 25f;

    [Tooltip("Rayon utilisé pour la détection d'impact.")]
    public float hitRadius = 0.25f;

    [Tooltip("FX d'explosion instancié au point d'impact. Optionnel.")]
    public GameObject impactFxPrefab;

    [Tooltip("Durée de vie du FX d'impact avant destruction.")]
    public float impactFxLifetime = 3f;

    [Tooltip("Échelle appliquée au FX d'impact.")]
    public float impactFxScale = 1f;

    [Tooltip("Vitesse de jeu des particules de l'impact.")]
    public float impactFxSpeed = 1f;

    [Tooltip("Couches qui peuvent stopper le projectile.")]
    public LayerMask hitMask = ~0;

    [Tooltip("Rayon AoE de l'explosion. 0 = pas d'AoE.")]
    public float explosionRadius = 0f;

    [Tooltip("Dégâts AoE infligés dans le rayon.")]
    public float explosionDamage = 0f;

    [Tooltip("Couches affectées par l'explosion AoE.")]
    public LayerMask explosionMask = ~0;

    [HideInInspector] public Collider ignoreCollider;
    [HideInInspector] public string sourceSpellId;

    Vector3 direction = Vector3.forward;
    bool consumed;

    public void Launch(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        direction = dir.normalized;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (consumed) return;

        float step = speed * Time.deltaTime;
        Vector3 origin = transform.position;

        if (Physics.SphereCast(origin, hitRadius, direction, out var hit, step + 0.05f, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != ignoreCollider)
            {
                transform.position = hit.point;
                HandleHit(hit.point, hit.normal, hit.collider);
                return;
            }
        }

        transform.position = origin + direction * step;
    }

    void HandleHit(Vector3 point, Vector3 normal, Collider hitCollider)
    {
        consumed = true;

        if (impactFxPrefab != null)
        {
            var fx = Instantiate(impactFxPrefab, point, Quaternion.LookRotation(normal, Vector3.up));
            if (impactFxScale != 1f) fx.transform.localScale *= impactFxScale;
            SpellFxUtility.SetPlaybackSpeed(fx, impactFxSpeed);
            Destroy(fx, impactFxLifetime);
        }

        Damageable directDamageable = null;
        if (damage > 0f)
        {
            bool killed = ApplyDamageTo(hitCollider, damage, point, normal, out directDamageable);
            if (killed) SpellProgression.GrantKill(sourceSpellId);
        }

        if (explosionRadius > 0f && explosionDamage > 0f)
            ApplyExplosionDamage(point, directDamageable, hitCollider);

        Destroy(gameObject);
    }

    /// <summary>
    /// Inflige des dégâts au target. Essaie Damageable d'abord, puis fallback EnemyStats.
    /// Retourne true si le target a été tué.
    /// </summary>
    static bool ApplyDamageTo(Collider c, float dmg, Vector3 point, Vector3 normal, out Damageable damageableHit)
    {
        damageableHit = null;
        if (c == null) return false;

        var d = c.GetComponentInParent<Damageable>();
        if (d != null)
        {
            damageableHit = d;
            return d.ApplyDamage(dmg, point, normal);
        }

        var e = c.GetComponentInParent<EnemyStats>();
        if (e != null)
        {
            int before = e.HP;
            e.TakeDamage(Mathf.RoundToInt(dmg));
            return e.HP <= 0 && before > 0;
        }

        var pd = c.GetComponentInParent<PlayerDamage>();
        if (pd != null) pd.TakeDamage(dmg);

        return false;
    }

    void ApplyExplosionDamage(Vector3 center, Damageable skipDamageable, Collider skipCollider)
    {
        var hits = Physics.OverlapSphere(center, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);
        var seenDamageables = new HashSet<Damageable>();
        var seenEnemies = new HashSet<EnemyStats>();
        if (skipDamageable != null) seenDamageables.Add(skipDamageable);

        foreach (var h in hits)
        {
            if (h == skipCollider) continue;

            Vector3 hp = h.ClosestPoint(center);
            Vector3 nrm = h.transform.position - center;
            nrm.y = 0f;
            nrm = nrm.sqrMagnitude < 0.0001f ? Vector3.up : nrm.normalized;

            var d = h.GetComponentInParent<Damageable>();
            if (d != null)
            {
                if (d.IsDead || !seenDamageables.Add(d)) continue;
                bool killed = d.ApplyDamage(explosionDamage, hp, nrm);
                if (killed) SpellProgression.GrantKill(sourceSpellId);
                continue;
            }

            var e = h.GetComponentInParent<EnemyStats>();
            if (e != null && seenEnemies.Add(e))
            {
                int before = e.HP;
                e.TakeDamage(Mathf.RoundToInt(explosionDamage));
                if (e.HP <= 0 && before > 0) SpellProgression.GrantKill(sourceSpellId);
            }
        }
    }
}
