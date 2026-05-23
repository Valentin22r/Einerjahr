using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    private PlayerStats playerStats;
    private WeaponStats weaponStats;
    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        weaponStats = GetComponent<WeaponStats>();
    }

    public void ResetCombo()
    {
        playerStats.Combo_multiplier = 1;
        playerStats.Combo_kill = 0;
    }
    private void addBlood(int bloodCount)
    {
        playerStats.Blood += bloodCount * playerStats.Combo_multiplier;
    }
    private void ElevateCombo()
    {
        playerStats.Combo_kill = 0;
        if (playerStats.Combo_multiplier < playerStats.Combo_multiplier_max)
            playerStats.Combo_multiplier += 0.25f;
    }
    public void Kill(int bloodCount)
    {
        playerStats.Combo_kill++;
        if (playerStats.Combo_kill == playerStats.Combo_kill_requirements)
            ElevateCombo();
        addBlood(bloodCount);
    }
    public void TakeDamage(float damage)
    {
        playerStats.HP -= damage;
        playerStats.Strength += damage;
        if (playerStats.HP <= 0)
            playerStats.Die();
    }
    public void HealDamage(float heal)
    {
        playerStats.HP += heal;
        playerStats.Strength -= heal;
    }
    public float DealDamage()
    {
        return weaponStats.Damage + (playerStats.Strength * playerStats.Strength_multiplier / 100);
    }
}