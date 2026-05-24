using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerAim))]
public class SpellCaster : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Point d'émission (ex. main du player). Si vide, on tire depuis la caméra.")]
    public Transform muzzle;
    [Tooltip("Collider du player à ignorer pour le projectile (laisse vide = auto).")]
    public Collider playerCollider;
    [Tooltip("Stats du joueur, source du sang dépensé pour caster. Si vide, on cherche sur le GameObject.")]
    public PlayerStats playerStats;

    [Header("Slot Projectile (clic gauche / molette pour changer)")]
    public List<ProjectileSpellData> projectileSpells = new List<ProjectileSpellData>();
    public int defaultProjectileIndex = 0;

    [Header("Slot AoE Sol (clic droit pour viser / molette pendant la visée)")]
    public List<GroundSpellData> groundSpells = new List<GroundSpellData>();
    public int defaultGroundIndex = 0;
    [Tooltip("Indicateur par défaut si le spell n'en définit pas un.")]
    public GameObject defaultGroundIndicatorPrefab;

    [Header("Bonus de spell en faible vie")]
    [Tooltip("À 0 HP, les dégâts des spells sont multipliés par (1 + lowHpDamageBonus). À full HP, multiplicateur = 1. Linéaire.")]
    [Min(0f)] public float lowHpDamageBonus = 1f;

    [Header("Touches")]
    public Key cancelAoeKey = Key.Escape;

    [Header("HUD")]
    public bool showHud = true;
    public Color hudColor = new Color(1f, 1f, 1f, 0.9f);
    public int hudFontSize = 16;

    [Header("Debug")]
    [Tooltip("Log dans la console à chaque crit.")]
    public bool logCrits = true;

    public int CurrentProjectileIndex { get; private set; }
    public int CurrentGroundIndex { get; private set; }

    public ProjectileSpellData CurrentProjectile =>
        projectileSpells.Count > 0 ? projectileSpells[Mathf.Clamp(CurrentProjectileIndex, 0, projectileSpells.Count - 1)] : null;
    public GroundSpellData CurrentGround =>
        groundSpells.Count > 0 ? groundSpells[Mathf.Clamp(CurrentGroundIndex, 0, groundSpells.Count - 1)] : null;

    PlayerAim aim;
    GroundTargeter targeter;
    float projectileNextCastTime;
    float aoeNextCastTime;
    readonly HashSet<int> unlockedProjectiles = new HashSet<int>();
    readonly HashSet<int> unlockedGrounds = new HashSet<int>();

    void Awake()
    {
        aim = GetComponent<PlayerAim>();
        targeter = GetComponent<GroundTargeter>();
        if (targeter == null) targeter = gameObject.AddComponent<GroundTargeter>();
        if (playerCollider == null) playerCollider = GetComponentInChildren<Collider>();
        if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();

        for (int i = 0; i < projectileSpells.Count; i++)
            if (projectileSpells[i] != null && !projectileSpells[i].startsLocked) unlockedProjectiles.Add(i);
        for (int i = 0; i < groundSpells.Count; i++)
            if (groundSpells[i] != null && !groundSpells[i].startsLocked) unlockedGrounds.Add(i);

        CurrentProjectileIndex = projectileSpells.Count > 0 ? Mathf.Clamp(defaultProjectileIndex, 0, projectileSpells.Count - 1) : 0;
        CurrentGroundIndex = groundSpells.Count > 0 ? Mathf.Clamp(defaultGroundIndex, 0, groundSpells.Count - 1) : 0;
        if (!IsProjectileUnlocked(CurrentProjectileIndex)) CycleProjectile(1);
        if (!IsGroundUnlocked(CurrentGroundIndex)) CycleGround(1);
    }

    public bool IsProjectileUnlocked(int index) => unlockedProjectiles.Contains(index);
    public bool IsGroundUnlocked(int index) => unlockedGrounds.Contains(index);

    public void UnlockProjectile(int index)
    {
        if (index < 0 || index >= projectileSpells.Count) return;
        unlockedProjectiles.Add(index);
    }

    public void UnlockGround(int index)
    {
        if (index < 0 || index >= groundSpells.Count) return;
        unlockedGrounds.Add(index);
    }

    public bool UnlockProjectile(ProjectileSpellData spell)
    {
        int i = projectileSpells.IndexOf(spell);
        if (i < 0) return false;
        unlockedProjectiles.Add(i);
        return true;
    }

    public bool UnlockGround(GroundSpellData spell)
    {
        int i = groundSpells.IndexOf(spell);
        if (i < 0) return false;
        unlockedGrounds.Add(i);
        return true;
    }

    public void LockProjectile(int index) { unlockedProjectiles.Remove(index); }
    public void LockGround(int index) { unlockedGrounds.Remove(index); }

    [ContextMenu("Unlock All")]
    public void UnlockAll()
    {
        for (int i = 0; i < projectileSpells.Count; i++) unlockedProjectiles.Add(i);
        for (int i = 0; i < groundSpells.Count; i++) unlockedGrounds.Add(i);
    }

    /// <summary>
    /// Rebuild the unlock sets from the current spell lists. Appelé après une injection
    /// dynamique (DivinityLoader, save load, etc.).
    /// </summary>
    public void RebuildUnlockState(bool unlockAll = false)
    {
        unlockedProjectiles.Clear();
        unlockedGrounds.Clear();

        for (int i = 0; i < projectileSpells.Count; i++)
        {
            if (projectileSpells[i] == null) continue;
            if (unlockAll || !projectileSpells[i].startsLocked) unlockedProjectiles.Add(i);
        }
        for (int i = 0; i < groundSpells.Count; i++)
        {
            if (groundSpells[i] == null) continue;
            if (unlockAll || !groundSpells[i].startsLocked) unlockedGrounds.Add(i);
        }

        CurrentProjectileIndex = projectileSpells.Count > 0 ? Mathf.Clamp(defaultProjectileIndex, 0, projectileSpells.Count - 1) : 0;
        CurrentGroundIndex = groundSpells.Count > 0 ? Mathf.Clamp(defaultGroundIndex, 0, groundSpells.Count - 1) : 0;
        if (!IsProjectileUnlocked(CurrentProjectileIndex)) CycleProjectile(1);
        if (!IsGroundUnlocked(CurrentGroundIndex)) CycleGround(1);
    }

    [ContextMenu("Lock All (sauf défaut)")]
    public void LockAll()
    {
        unlockedProjectiles.Clear();
        unlockedGrounds.Clear();
        if (projectileSpells.Count > 0) unlockedProjectiles.Add(Mathf.Clamp(defaultProjectileIndex, 0, projectileSpells.Count - 1));
        if (groundSpells.Count > 0) unlockedGrounds.Add(Mathf.Clamp(defaultGroundIndex, 0, groundSpells.Count - 1));
    }

    void Update()
    {
        if (Mouse.current == null) return;

        float wheel = Mouse.current.scroll.ReadValue().y;

        if (targeter.IsActive)
        {
            UpdateAoeTargeting(wheel);
            return;
        }

        if (wheel != 0f && projectileSpells.Count > 1)
            CycleProjectile(wheel > 0 ? 1 : -1);

        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= projectileNextCastTime)
            CastProjectile();

        if (Mouse.current.rightButton.wasPressedThisFrame && Time.time >= aoeNextCastTime)
            BeginAoeTargeting();
    }

    void CycleProjectile(int dir)
    {
        int next = FindNextUnlocked(projectileSpells.Count, CurrentProjectileIndex, dir, unlockedProjectiles);
        if (next >= 0) CurrentProjectileIndex = next;
    }

    void CycleGround(int dir)
    {
        int next = FindNextUnlocked(groundSpells.Count, CurrentGroundIndex, dir, unlockedGrounds);
        if (next >= 0)
        {
            CurrentGroundIndex = next;
            ApplyIndicatorForCurrentGround();
        }
    }

    static int FindNextUnlocked(int count, int from, int dir, HashSet<int> unlocked)
    {
        if (count == 0 || unlocked.Count == 0) return -1;
        int cur = from;
        for (int step = 0; step < count; step++)
        {
            cur = (cur + dir + count) % count;
            if (unlocked.Contains(cur)) return cur;
        }
        return -1;
    }

    float LowHpDamageMultiplier()
    {
        if (playerStats == null || playerStats.MaxHP <= 0f) return 1f;
        float missing = Mathf.Clamp01(1f - playerStats.HP / playerStats.MaxHP);
        return 1f + missing * lowHpDamageBonus;
    }

    void CastProjectile()
    {
        var s = CurrentProjectile;
        if (s == null || s.projectilePrefab == null) return;
        if (!IsProjectileUnlocked(CurrentProjectileIndex)) return;

        int progLevel = SpellProgression.GetLevel(s.name);
        float progDmg = SpellProgression.DamageMultiplier(progLevel);
        float progRadius = SpellProgression.RadiusMultiplier(progLevel);
        float progCdMul = SpellProgression.CooldownMultiplier(progLevel);
        float lowHpMul = LowHpDamageMultiplier();

        projectileNextCastTime = Time.time + s.cooldown * progCdMul;

        bool crit = Random.value < s.critChance;
        float scaleMul = crit ? s.critScaleMultiplier : 1f;
        float damageMul = (crit ? s.critDamageMultiplier : 1f) * progDmg * lowHpMul;
        float radiusMul = (crit ? s.critRadiusMultiplier : 1f) * progRadius;

        float explosionRadius = s.explosionRadius * radiusMul;
        if (crit && explosionRadius < s.critMinExplosionRadius)
            explosionRadius = s.critMinExplosionRadius;

        float explosionDamage = s.explosionDamage * damageMul;
        if (crit && explosionRadius > 0f && explosionDamage <= 0f)
            explosionDamage = s.projectileDamage * damageMul;

        if (crit && logCrits)
            Debug.Log($"[SpellCaster] CRIT {s.displayName}: dmg ×{s.critDamageMultiplier}, size ×{s.critScaleMultiplier}, explosion {explosionRadius:F1}m / {explosionDamage:F0} dmg");

        Vector3 direction = aim.AimDirection;
        Vector3 origin = muzzle != null ? muzzle.position : aim.AimOrigin + direction * 0.5f;

        if (s.castFxPrefab != null)
        {
            var castFx = Instantiate(s.castFxPrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
            castFx.transform.localScale *= s.castFxScale * scaleMul;
            SpellFxUtility.SetPlaybackSpeed(castFx, s.castFxSpeed);
            Destroy(castFx, s.castFxLifetime);
        }

        var go = Instantiate(s.projectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
        go.transform.localScale *= s.projectileScale * scaleMul;
        SpellFxUtility.DisableObstructingComponents(go);

        var projectile = go.GetComponent<SpellProjectile>();
        if (projectile == null) projectile = go.AddComponent<SpellProjectile>();
        projectile.speed = s.projectileSpeed;
        projectile.damage = s.projectileDamage * damageMul;
        projectile.hitRadius = s.hitRadius * scaleMul;
        projectile.lifeTime = s.lifeTime;
        projectile.impactFxPrefab = s.impactFxPrefab;
        projectile.impactFxScale = s.impactFxScale * scaleMul;
        projectile.impactFxSpeed = s.impactFxSpeed;
        projectile.impactFxLifetime = s.impactFxLifetime;
        projectile.hitMask = s.hitMask;
        projectile.explosionRadius = explosionRadius;
        projectile.explosionDamage = explosionDamage;
        projectile.explosionMask = s.explosionMask;
        projectile.ignoreCollider = playerCollider;
        projectile.sourceSpellId = s.name;
        projectile.Launch(direction);
    }

    void BeginAoeTargeting()
    {
        if (groundSpells.Count == 0 || unlockedGrounds.Count == 0) return;
        if (!IsGroundUnlocked(CurrentGroundIndex)) CycleGround(1);
        if (!IsGroundUnlocked(CurrentGroundIndex)) return;
        ApplyIndicatorForCurrentGround();
        targeter.Begin();
    }

    void ApplyIndicatorForCurrentGround()
    {
        var g = CurrentGround;
        GameObject prefab = (g != null && g.indicatorPrefab != null) ? g.indicatorPrefab : defaultGroundIndicatorPrefab;
        float scale = (g != null)
            ? (g.indicatorScale > 0f ? g.indicatorScale : g.radius)
            : 1f;
        targeter.SetIndicator(prefab, scale);
    }

    void UpdateAoeTargeting(float wheel)
    {
        if (wheel != 0f && groundSpells.Count > 1)
            CycleGround(wheel > 0 ? 1 : -1);

        if (aim.TryGetGroundPointUnderCursor(out var groundPoint))
            targeter.UpdatePoint(groundPoint);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ConfirmAoe();
            return;
        }

        bool cancel = Mouse.current.rightButton.wasPressedThisFrame
            || (Keyboard.current != null && Keyboard.current[cancelAoeKey].wasPressedThisFrame);
        if (cancel) targeter.Cancel();
    }

    void OnGUI()
    {
        if (!showHud) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = hudFontSize,
            normal = { textColor = hudColor },
            alignment = TextAnchor.LowerLeft
        };

        string projLevel = CurrentProjectile != null ? $" L{SpellProgression.GetLevel(CurrentProjectile.name)}" : "";
        string groundLevel = CurrentGround != null ? $" L{SpellProgression.GetLevel(CurrentGround.name)}" : "";
        string proj = CurrentProjectile != null
            ? (IsProjectileUnlocked(CurrentProjectileIndex) ? $"{CurrentProjectile.displayName}{projLevel}" : $"🔒 {CurrentProjectile.displayName}")
            : "—";
        string ground = CurrentGround != null
            ? (IsGroundUnlocked(CurrentGroundIndex) ? $"{CurrentGround.displayName}{groundLevel}" : $"🔒 {CurrentGround.displayName}")
            : "—";

        float h = hudFontSize * 1.6f;
        float w = 360f;
        float x = 12f;
        float y = Screen.height - 12f - h * 2f;

        GUI.Label(new Rect(x, y, w, h), $"[Wheel] Tir : {proj}  ({CurrentProjectileIndex + 1}/{projectileSpells.Count})", style);
        GUI.Label(new Rect(x, y + h, w, h), $"[RMB+Wheel] Sol : {ground}  ({CurrentGroundIndex + 1}/{groundSpells.Count})", style);

        if (targeter.IsActive)
        {
            var s2 = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = hudFontSize + 4 };
            GUI.Label(new Rect(0, Screen.height / 2f - 60f, Screen.width, h), "Visée AoE — clic gauche pour valider, clic droit pour annuler", s2);
        }
    }

    void ConfirmAoe()
    {
        var s = CurrentGround;
        Vector3 point = targeter.CurrentPoint;
        targeter.Cancel();

        if (s == null || s.aoeEffectPrefab == null) return;
        if (!IsGroundUnlocked(CurrentGroundIndex)) return;

        int progLevel = SpellProgression.GetLevel(s.name);
        float progDmg = SpellProgression.DamageMultiplier(progLevel);
        float progRadius = SpellProgression.RadiusMultiplier(progLevel);
        float progCdMul = SpellProgression.CooldownMultiplier(progLevel);
        float lowHpMul = LowHpDamageMultiplier();

        aoeNextCastTime = Time.time + s.cooldown * progCdMul;

        bool crit = Random.value < s.critChance;
        float radius = s.radius * (crit ? s.critRadiusMultiplier : 1f) * progRadius;
        float damage = s.damagePerTick * (crit ? s.critDamageMultiplier : 1f) * progDmg * lowHpMul;
        float scale = s.effectScale * (crit ? s.critScaleMultiplier : 1f);

        if (crit && logCrits) Debug.Log($"[SpellCaster] CRIT AoE! {s.displayName} ×{s.critDamageMultiplier} damage, ×{s.critRadiusMultiplier} radius ({radius:F1} m)");

        var go = Instantiate(s.aoeEffectPrefab, point, Quaternion.identity);
        if (scale != 1f) go.transform.localScale *= scale;

        var aoe = go.GetComponent<AoESpell>();
        if (aoe == null) aoe = go.AddComponent<AoESpell>();
        aoe.Configure(s.duration, s.tickInterval, radius, damage, s.targetMask);
        aoe.sourceSpellId = s.name;

        Destroy(go, s.duration + 2f);
    }
}
