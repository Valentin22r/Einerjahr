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
    [Tooltip("XP de compte donnée au kill.")]
    public int accountXpDrop = 5;
    [Tooltip("HP rendus au joueur le plus proche au kill (heal). Le HP est capé à PlayerStats.MaxHP.")]
    public float healOnKill = 5f;

    [Header("Objectifs")]
    [Tooltip("Si non vide, ce kill ne compte que pour les objectifs avec ce filterTag (ex. 'Boss').")]
    public string objectiveFilterTag = "";

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
        if (accountXpDrop > 0) AccountProgress.GrantXp(accountXpDrop);
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportProgress(ObjectiveType.KillEnemies, 1, objectiveFilterTag);
        if (healOnKill > 0f) HealNearestPlayer();
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    void HealNearestPlayer()
    {
        var ps = Object.FindFirstObjectByType<PlayerStats>();
        if (ps == null || ps.IsDead) return;
        ps.HP = Mathf.Min(ps.MaxHP, ps.HP + healOnKill);
    }
}
