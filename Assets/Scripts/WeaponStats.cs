using UnityEngine;

public class WeaponStats : MonoBehaviour
{
    [Header("Weapon Level")]
    public int DamageLevel = 1;
    public int RangeLevel = 1;
    public int SpeedLevel = 1;

    [Header("Weapon Stats")]
    public float Damage = 10;
    public float Attack_Timer;
    public float Block_Timer;
    public float Range = 10;


    [Header("Weapon base Stats")]
    public float Base_Range = 10;
    public int Base_Damage = 10;

    [Header("Weapon Multiplier")]
    public float LevelUp_Multiplicator_Damage = 1.25f;
    public float LevelUp_Multiplicator_Attack_Timer = 0.15f;
    public float LevelUp_Multiplicator_Block_Timer = 0.05f;
    public float LevelUp_Multiplicator_Range = 1.05f;


    public void UpgradeAttack()
    {
        Damage += Base_Damage * LevelUp_Multiplicator_Damage;
    }
    public void UpgradeAttackTimer()
    {
        Attack_Timer -= LevelUp_Multiplicator_Attack_Timer;
    }
    public void UpgradeBlock()
    {
        Block_Timer += LevelUp_Multiplicator_Block_Timer;
    }
    public void UpgradeRanges()
    {
        Range += Base_Range * LevelUp_Multiplicator_Range;
    }
}
