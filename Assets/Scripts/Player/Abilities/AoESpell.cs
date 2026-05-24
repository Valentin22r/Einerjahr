using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AoESpell : MonoBehaviour
{
    [Tooltip("Durée totale d'application de l'effet (s).")]
    public float duration = 4f;

    [Tooltip("Intervalle entre deux ticks de dégâts (s).")]
    public float tickInterval = 0.5f;

    [Tooltip("Rayon de la zone (m).")]
    public float radius = 4f;

    [Tooltip("Dégâts par tick infligés à chaque Damageable dans la zone.")]
    public float damagePerTick = 15f;

    [Tooltip("LayerMask des cibles potentielles. Utilise les couches des cibles, pas le sol.")]
    public LayerMask targetMask = ~0;

    [HideInInspector] public string sourceSpellId;

    public void Configure(float duration, float tickInterval, float radius, float damagePerTick, LayerMask targetMask)
    {
        this.duration = duration;
        this.tickInterval = tickInterval;
        this.radius = radius;
        this.damagePerTick = damagePerTick;
        this.targetMask = targetMask;
    }

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float elapsed = 0f;
        var hits = new Collider[32];
        var damagedDamageables = new HashSet<Damageable>();
        var damagedEnemies = new HashSet<EnemyStats>();

        while (elapsed < duration)
        {
            damagedDamageables.Clear();
            damagedEnemies.Clear();
            int n = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, targetMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Vector3 hitPoint = hits[i].ClosestPoint(transform.position + Vector3.up * 1f);
                Vector3 normal = (hits[i].transform.position - transform.position);
                normal.y = 0f;
                if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
                else normal.Normalize();

                var d = hits[i].GetComponentInParent<Damageable>();
                if (d != null)
                {
                    if (d.IsDead || !damagedDamageables.Add(d)) continue;
                    bool killed = d.ApplyDamage(damagePerTick, hitPoint, normal);
                    if (killed) SpellProgression.GrantKill(sourceSpellId);
                    continue;
                }

                var e = hits[i].GetComponentInParent<EnemyStats>();
                if (e != null && damagedEnemies.Add(e))
                {
                    int before = e.HP;
                    e.TakeDamage(Mathf.RoundToInt(damagePerTick));
                    if (e.HP <= 0 && before > 0) SpellProgression.GrantKill(sourceSpellId);
                }
            }

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
