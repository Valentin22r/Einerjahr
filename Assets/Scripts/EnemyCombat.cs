using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    private float nextAttackTime;
    private EnemyStats stats;
    private Transform player;

    void Start()
    {
        stats = GetComponent<EnemyStats>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            Debug.Log("Player Found");
        }
        else
        {
            Debug.Log("Player NOT Found");
        }
    }
    void Update()
    {
        if (player == null)
            return;
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void Attack()
    {
        PlayerDamage playerStats = player.GetComponent<PlayerDamage>();
        if (playerStats != null)
        {
            Debug.Log("Damage: " + stats.damage);
            playerStats.TakeDamage(stats.damage);
            Debug.Log("Enemy attacked player");
        }
    }
}