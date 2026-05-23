using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Einerjahr/Divinity/Divinity", fileName = "Divinity_New")]
public class DivinityData : ScriptableObject
{
    [Header("Identité")]
    public string displayName = "Berserker";
    [TextArea] public string description = "Augmente la force au prix de la santé.";
    public Sprite icon;

    [Header("Bonus de stats appliqués au PlayerStats")]
    [Tooltip("Bonus additif à HP au démarrage.")]
    public float bonusHP = 0f;
    [Tooltip("Bonus additif à Strength au démarrage.")]
    public float bonusStrength = 0f;
    [Tooltip("Sang max (PlayerStats.Blood plafond souple — appliqué juste après l'application des bonus).")]
    public float startBlood = 0f;
    [Tooltip("Multiplicateur HP supplémentaire (HP_multiplier).")]
    public int bonusHPMultiplier = 0;
    [Tooltip("Multiplicateur de Strength supplémentaire (Strength_multiplier).")]
    public int bonusStrengthMultiplier = 0;

    [Header("Loadout de spells — tir (clic gauche, molette)")]
    public List<ProjectileSpellData> projectileSpells = new List<ProjectileSpellData>();
    public int defaultProjectileIndex = 0;

    [Header("Loadout de spells — sol (clic droit, molette)")]
    public List<GroundSpellData> groundSpells = new List<GroundSpellData>();
    public int defaultGroundIndex = 0;

    [Header("Progression")]
    [Tooltip("Tous les spells du loadout sont débloqués au start (sinon respecte startsLocked sur chaque spell).")]
    public bool unlockAllSpells = false;
}
