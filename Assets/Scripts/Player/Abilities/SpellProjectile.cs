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

        var directTarget = hitCollider != null ? hitCollider.GetComponentInParent<Damageable>() : null;
        if (directTarget != null && damage > 0f)
            directTarget.ApplyDamage(damage, point, normal);

        if (explosionRadius > 0f && explosionDamage > 0f)
            ApplyExplosionDamage(point, directTarget);

        Destroy(gameObject);
    }

    void ApplyExplosionDamage(Vector3 center, Damageable skipDirect)
    {
        var hits = Physics.OverlapSphere(center, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);
        var seen = new HashSet<Damageable>();
        if (skipDirect != null) seen.Add(skipDirect);

        foreach (var h in hits)
        {
            var d = h.GetComponentInParent<Damageable>();
            if (d == null || d.IsDead || !seen.Add(d)) continue;

            Vector3 hp = h.ClosestPoint(center);
            Vector3 nrm = h.transform.position - center;
            nrm.y = 0f;
            nrm = nrm.sqrMagnitude < 0.0001f ? Vector3.up : nrm.normalized;
            d.ApplyDamage(explosionDamage, hp, nrm);
        }
    }
}
