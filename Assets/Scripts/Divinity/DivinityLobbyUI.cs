using UnityEngine;
using UnityEngine.SceneManagement;

public class DivinityLobbyUI : MonoBehaviour
{
    [Header("Divinité")]
    [Tooltip("Catalogue affichant les divinités disponibles dans le lobby.")]
    public DivinityCatalog catalog;

    [Header("Lancement de partie")]
    [Tooltip("Nom de la scène à charger une fois la divinité validée.")]
    public string sceneToLoadOnConfirm = "SampleScene";

    [Header("Affichage")]
    [Tooltip("Affiche le HUD OnGUI. Décocher si tu utilises tes propres boutons UGUI.")]
    public bool showHud = true;

    [Header("Debug")]
    [Tooltip("Affiche un bouton qui donne 100 Mythril (pour tester les upgrades).")]
    public bool showDebugMythrilButton = true;

    void Start()
    {
        if (catalog == null) return;
        if (DivinitySelection.Selected == null)
            DivinitySelection.Select(catalog.defaultDivinity != null
                ? catalog.defaultDivinity
                : (catalog.divinities.Count > 0 ? catalog.divinities[0] : null));
    }

    public void Select(DivinityData d) => DivinitySelection.Select(d);

    public void Confirm()
    {
        if (string.IsNullOrEmpty(sceneToLoadOnConfirm)) return;
        SceneManager.LoadScene(sceneToLoadOnConfirm);
    }

    public void UpgradeWeaponDamage()
    {
        int cost = PlayerProgress.CostForLevel(PlayerProgress.WeaponDamageLevel);
        if (!PlayerProgress.TrySpendMythril(cost)) return;
        PlayerProgress.WeaponDamageLevel++;
    }

    public void UpgradeWeaponRange()
    {
        int cost = PlayerProgress.CostForLevel(PlayerProgress.WeaponRangeLevel);
        if (!PlayerProgress.TrySpendMythril(cost)) return;
        PlayerProgress.WeaponRangeLevel++;
    }

    public void UpgradeWeaponSpeed()
    {
        int cost = PlayerProgress.CostForLevel(PlayerProgress.WeaponSpeedLevel);
        if (!PlayerProgress.TrySpendMythril(cost)) return;
        PlayerProgress.WeaponSpeedLevel++;
    }

    void OnGUI()
    {
        if (!showHud) return;

        var box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        var btn = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6) };
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        var lblRich = new GUIStyle(lbl) { richText = true, fontSize = 14 };

        DrawDivinityPanel(box, btn, lbl, lblRich);
        DrawUpgradePanel(box, btn, lbl, lblRich);
        DrawPlayButton(btn);
    }

    void DrawDivinityPanel(GUIStyle box, GUIStyle btn, GUIStyle lbl, GUIStyle lblRich)
    {
        if (catalog == null || catalog.divinities.Count == 0) return;

        const float panelW = 360f;
        Rect panel = new Rect(20f, 20f, panelW, Screen.height - 100f);
        GUI.Box(panel, "── Divinités ──", box);

        float y = panel.y + 36f;
        for (int i = 0; i < catalog.divinities.Count; i++)
        {
            var d = catalog.divinities[i];
            if (d == null) continue;
            bool isSelected = DivinitySelection.Selected == d;
            string label = (isSelected ? "● " : "○ ") + d.displayName;
            if (GUI.Button(new Rect(panel.x + 12f, y, panelW - 24f, 30f), label, btn))
                Select(d);
            y += 34f;
        }

        var current = DivinitySelection.Selected;
        if (current != null)
        {
            y += 12f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 22f), $"<b>{current.displayName}</b>", lblRich);
            y += 26f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 70f), current.description, lbl);
            y += 76f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 22f), $"+HP {current.bonusHP:+0;-0;0}    +Strength {current.bonusStrength:+0;-0;0}", lbl);
            y += 22f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 22f), $"Sang start {current.startBlood:F0}    Tir x{current.projectileSpells.Count}    Sol x{current.groundSpells.Count}", lbl);
        }
    }

    void DrawUpgradePanel(GUIStyle box, GUIStyle btn, GUIStyle lbl, GUIStyle lblRich)
    {
        const float panelW = 360f;
        Rect panel = new Rect(Screen.width - panelW - 20f, 20f, panelW, Screen.height - 100f);
        GUI.Box(panel, "── Forge d'armes ──", box);

        float y = panel.y + 36f;
        GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 24f),
            $"<b>Mythril : {PlayerProgress.Mythril}</b>", lblRich);
        y += 30f;

        DrawUpgradeRow(panel.x + 12f, ref y, panelW - 24f, "Dégâts",
            PlayerProgress.WeaponDamageLevel, btn, lbl, UpgradeWeaponDamage);
        DrawUpgradeRow(panel.x + 12f, ref y, panelW - 24f, "Portée",
            PlayerProgress.WeaponRangeLevel, btn, lbl, UpgradeWeaponRange);
        DrawUpgradeRow(panel.x + 12f, ref y, panelW - 24f, "Vitesse",
            PlayerProgress.WeaponSpeedLevel, btn, lbl, UpgradeWeaponSpeed);

        if (showDebugMythrilButton)
        {
            y += 24f;
            if (GUI.Button(new Rect(panel.x + 12f, y, panelW - 24f, 28f), "[DEBUG] +100 Mythril", btn))
                PlayerProgress.GrantMythril(100);
            y += 32f;
            if (GUI.Button(new Rect(panel.x + 12f, y, panelW - 24f, 28f), "[DEBUG] Reset progression", btn))
                PlayerProgress.ResetAll();
        }
    }

    void DrawUpgradeRow(float x, ref float y, float w, string label, int level, GUIStyle btn, GUIStyle lbl, System.Action onClick)
    {
        int cost = PlayerProgress.CostForLevel(level);
        bool canAfford = PlayerProgress.Mythril >= cost;

        GUI.Label(new Rect(x, y, w * 0.45f, 28f), $"{label} L{level}", lbl);
        GUI.enabled = canAfford;
        if (GUI.Button(new Rect(x + w * 0.45f, y, w * 0.55f, 28f), $"Upgrade ({cost} Mythril)", btn))
            onClick?.Invoke();
        GUI.enabled = true;
        y += 32f;
    }

    void DrawPlayButton(GUIStyle btn)
    {
        var bigBtn = new GUIStyle(btn) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        var rect = new Rect(Screen.width / 2f - 160f, Screen.height - 64f, 320f, 48f);

        GUI.enabled = DivinitySelection.Selected != null && !string.IsNullOrEmpty(sceneToLoadOnConfirm);
        if (GUI.Button(rect, "▶ Lancer la partie", bigBtn))
            Confirm();
        GUI.enabled = true;
    }
}
