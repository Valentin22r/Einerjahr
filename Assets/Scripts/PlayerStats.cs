using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Stats")]
    public float HP = 100;
    public float Blood = 0;
    public float Strength = 10;

    public int Mythril = 0;

    [Header("Player Multiplicators")]
    public int HP_multiplier = 100;
    public int Strength_multiplier = 100;

    [Header("Player Combo")]
    public float Combo_multiplier = 1;
    public float Combo_kill = 0;
    public int Combo_kill_requirements = 5;
    public float Combo_multiplier_max = 5;

    [Header("Death / Revive")]
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPlayerRevived;

    public bool IsDead { get; private set; }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log("Player Died");
        OnPlayerDeath?.Invoke();
    }

    public void Revive(float reviveHP = 50f)
    {
        if (!IsDead) return;
        IsDead = false;
        HP = Mathf.Max(HP, reviveHP);
        Debug.Log($"Player Revived (HP={HP:F0})");
        OnPlayerRevived?.Invoke();
    }
}
