using UnityEngine;
using UnityEngine.Events;

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int HP = 100;
    public int damage = 10;
    public float blood = 30;

    [Header("Drops")]
    [Tooltip("Mythril donné au joueur en kill (persistent via PlayerProgress / PlayerPrefs).")]
    public int mythrilDrop = 5;
    [Tooltip("Range minimale aléatoire pour le drop. Si > 0, drop final = Random.Range(mythrilDropMin, mythrilDrop + 1).")]
    public int mythrilDropMin = 0;

    [Header("Events")]
    public UnityEvent OnDeath;

    bool dead;

    public void TakeDamage(int amount)
    {
        if (dead) return;
        HP -= amount;
        if (HP <= 0) Die();
    }

    void Die()
    {
        dead = true;
        int drop = mythrilDropMin > 0 ? Random.Range(mythrilDropMin, mythrilDrop + 1) : mythrilDrop;
        PlayerProgress.GrantMythril(drop);
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
