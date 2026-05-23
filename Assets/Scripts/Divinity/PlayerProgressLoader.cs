using UnityEngine;

/// <summary>
/// À placer sur le Player. Applique au Start les upgrades persistantes
/// (PlayerProgress.WeaponDamageLevel, etc.) à WeaponStats.
/// </summary>
[DisallowMultipleComponent]
public class PlayerProgressLoader : MonoBehaviour
{
    [Tooltip("Joue l'application au Start. Décocher pour appeler Apply() manuellement.")]
    public bool applyOnStart = true;

    WeaponStats weapon;

    void Awake()
    {
        weapon = GetComponent<WeaponStats>();
    }

    void Start()
    {
        if (applyOnStart) Apply();
    }

    public void Apply()
    {
        if (weapon == null) return;

        int dmgLevel = PlayerProgress.WeaponDamageLevel;
        int rangeLevel = PlayerProgress.WeaponRangeLevel;
        int speedLevel = PlayerProgress.WeaponSpeedLevel;

        weapon.DamageLevel = dmgLevel;
        weapon.RangeLevel = rangeLevel;
        weapon.SpeedLevel = speedLevel;

        weapon.Damage = weapon.Base_Damage;
        for (int i = 1; i < dmgLevel; i++) weapon.UpgradeAttack();

        weapon.Range = weapon.Base_Range;
        for (int i = 1; i < rangeLevel; i++) weapon.UpgradeRanges();

        for (int i = 1; i < speedLevel; i++) weapon.UpgradeAttackTimer();

        Debug.Log($"[PlayerProgressLoader] Loaded — Dmg L{dmgLevel} ({weapon.Damage:F0}), Range L{rangeLevel} ({weapon.Range:F0}), Speed L{speedLevel}");
    }
}
