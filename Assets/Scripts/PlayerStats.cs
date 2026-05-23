using UnityEngine;

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
    public void Die()
    {
        Debug.Log("Player Died");
    }
}
