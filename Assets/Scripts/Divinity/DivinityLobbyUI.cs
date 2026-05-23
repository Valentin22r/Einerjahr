using UnityEngine;
using UnityEngine.SceneManagement;

public class DivinityLobbyUI : MonoBehaviour
{
    [Tooltip("Catalogue affichant les divinités disponibles dans le lobby.")]
    public DivinityCatalog catalog;

    [Tooltip("Nom de la scène à charger une fois la divinité validée. Laisser vide = ne rien charger (on update juste la sélection).")]
    public string sceneToLoadOnConfirm = "SampleScene";

    [Tooltip("Affiche automatiquement le HUD OnGUI. Décocher si tu utilises tes propres boutons UGUI.")]
    public bool showHud = true;

    int hoveredIndex = -1;

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

    void OnGUI()
    {
        if (!showHud || catalog == null || catalog.divinities.Count == 0) return;

        var box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        var btn = new GUIStyle(GUI.skin.button) { fontSize = 14, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6) };
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };

        const float panelW = 380f;
        float h = Screen.height - 40f;
        Rect panel = new Rect(20f, 20f, panelW, h);
        GUI.Box(panel, "── Choix de la Divinité ──", box);

        float y = panel.y + 36f;
        for (int i = 0; i < catalog.divinities.Count; i++)
        {
            var d = catalog.divinities[i];
            if (d == null) continue;
            bool isSelected = DivinitySelection.Selected == d;
            string prefix = isSelected ? "● " : "○ ";
            string label = $"{prefix}{d.displayName}";

            var r = new Rect(panel.x + 12f, y, panelW - 24f, 30f);
            if (r.Contains(Event.current.mousePosition)) hoveredIndex = i;
            if (GUI.Button(r, label, btn)) Select(d);
            y += 34f;
        }

        var current = DivinitySelection.Selected;
        if (current != null)
        {
            y += 12f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 22f), $"<b>{current.displayName}</b>",
                new GUIStyle(lbl) { richText = true, fontSize = 15 });
            y += 28f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 70f), current.description, lbl);
            y += 80f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 22f), $"+HP : {current.bonusHP:F0}    +Strength : {current.bonusStrength:F0}", lbl);
            y += 24f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 22f), $"Sang start : {current.startBlood:F0}", lbl);
            y += 30f;
            GUI.Label(new Rect(panel.x + 12f, y, panelW - 24f, 22f), $"Spells tir : {current.projectileSpells.Count}    sol : {current.groundSpells.Count}", lbl);
        }

        var confirmRect = new Rect(panel.x + 12f, panel.y + panel.height - 50f, panelW - 24f, 36f);
        GUI.enabled = current != null && !string.IsNullOrEmpty(sceneToLoadOnConfirm);
        if (GUI.Button(confirmRect, "Confirmer et lancer la partie", btn))
            Confirm();
        GUI.enabled = true;
    }
}
