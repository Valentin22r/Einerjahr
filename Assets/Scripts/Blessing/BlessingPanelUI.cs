using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panneau OnGUI minimal pour gérer les bénédictions dans le lobby.
/// Pas de setup UGUI requis — drag le BlessingCatalog et c'est tout.
/// </summary>
public class BlessingPanelUI : MonoBehaviour
{
    public BlessingCatalog catalog;
    public bool showHud = true;
    public int fontSize = 14;
    public KeyCode toggleKey = KeyCode.B;

    bool open;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) open = !open;
    }

    void OnGUI()
    {
        if (!showHud || catalog == null) return;
        if (!open)
        {
            var hint = new GUIStyle(GUI.skin.label) { fontSize = fontSize, normal = { textColor = new Color(1,1,1,0.7f) } };
            GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height - 24f, 200f, 22f),
                $"[{toggleKey}] Bénédictions", hint);
            return;
        }

        var box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = fontSize + 2, fontStyle = FontStyle.Bold };
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
        var btn = new GUIStyle(GUI.skin.button) { fontSize = fontSize, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6) };

        const float w = 460f;
        Rect panel = new Rect(Screen.width / 2f - w / 2f, 40f, w, Screen.height - 100f);
        GUI.Box(panel, "── BÉNÉDICTIONS ──", box);

        float y = panel.y + 36f;
        GUI.Label(new Rect(panel.x + 12f, y, w - 24f, 22f),
            $"XP disponible : <b>{AccountProgress.BlessingXp}</b>",
            new GUIStyle(lbl) { richText = true, fontSize = fontSize + 1 });
        y += 30f;

        foreach (var b in catalog.blessings)
        {
            if (b == null) continue;
            int lvl = BlessingProgress.GetLevel(b);
            bool prereq = BlessingProgress.ArePrerequisitesMet(b);
            bool max = lvl >= b.maxLevel;
            string status = max ? "MAX" : $"L{lvl}/{b.maxLevel}";

            GUI.Label(new Rect(panel.x + 12f, y, w - 24f, 22f),
                $"<b>{b.displayName}</b>  {status}",
                new GUIStyle(lbl) { richText = true, fontSize = fontSize + 1 });
            y += 24f;
            GUI.Label(new Rect(panel.x + 24f, y, w - 36f, 36f), b.description, lbl);
            y += 36f;

            if (!prereq)
            {
                GUI.Label(new Rect(panel.x + 24f, y, w - 36f, 22f),
                    "Prérequis non remplis", new GUIStyle(lbl) { normal = { textColor = new Color(1f, 0.5f, 0.5f) } });
            }
            else if (!max)
            {
                GUI.enabled = AccountProgress.BlessingXp >= b.xpPerLevel;
                if (GUI.Button(new Rect(panel.x + 24f, y, w - 36f, 26f),
                        $"Acheter L{lvl + 1}  ({b.xpPerLevel} XP)", btn))
                    BlessingProgress.TryUpgrade(b);
                GUI.enabled = true;
            }
            y += 32f;

            GUI.Box(new Rect(panel.x + 12f, y, w - 24f, 1f), GUIContent.none);
            y += 6f;
        }

        if (GUI.Button(new Rect(panel.x + 12f, panel.y + panel.height - 36f, w - 24f, 28f), "Fermer (B)", btn))
            open = false;
    }
}
