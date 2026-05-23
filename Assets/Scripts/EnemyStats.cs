using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int HP = 100;
    public int damage = 10;
    public float blood = 30;
    public void TakeDamage(int amount)
    {
        HP -= amount;

        Debug.Log("Enemy HP: " + HP);

        if (HP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
