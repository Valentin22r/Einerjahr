using UnityEngine;

/// <summary>
/// Panneau OnGUI unifié pour le lobby : sélection de divinité + achat de bénédictions.
/// Touche B pour ouvrir/fermer. Aucun setup UGUI nécessaire.
/// </summary>
public class BlessingPanelUI : MonoBehaviour
{
    [Header("Catalogues")]
    public DivinityCatalog divinityCatalog;
    public BlessingCatalog blessingCatalog;

    [Header("Affichage")]
    public bool showHud = true;
    public int fontSize = 14;
    public KeyCode toggleKey = KeyCode.B;

    [Tooltip("Largeur du panneau.")]
    public float panelWidth = 520f;

    [Tooltip("Couleur de fond opaque du panneau.")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);

    [Tooltip("Couleur d'un voile derrière le panneau (assombrit la scène). Alpha 0 = pas de voile.")]
    public Color dimColor = new Color(0f, 0f, 0f, 0.55f);

    Vector2 scrollPos;
    bool open;
    Texture2D backgroundTexture;
    Texture2D dimTexture;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) open = !open;
    }

    Texture2D MakeColorTexture(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    void EnsureTextures()
    {
        if (backgroundTexture == null) backgroundTexture = MakeColorTexture(backgroundColor);
        if (dimTexture == null) dimTexture = MakeColorTexture(dimColor);
    }

    void OnDisable()
    {
        if (backgroundTexture != null) { Destroy(backgroundTexture); backgroundTexture = null; }
        if (dimTexture != null) { Destroy(dimTexture); dimTexture = null; }
    }

    void OnGUI()
    {
        if (!showHud) return;
        if (!open) return;

        EnsureTextures();

        var box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = fontSize + 2, fontStyle = FontStyle.Bold };
        box.normal.background = backgroundTexture;
        box.normal.textColor = Color.white;
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
        var lblRich = new GUIStyle(lbl) { richText = true, fontSize = fontSize + 1 };
        var btn = new GUIStyle(GUI.skin.button) { fontSize = fontSize, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6) };

        // Voile sombre derrière (assombrit la scène)
        if (dimColor.a > 0f)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dimTexture, ScaleMode.StretchToFill);

        Rect panel = new Rect(Screen.width / 2f - panelWidth / 2f, 40f, panelWidth, Screen.height - 100f);

        // Fond opaque du panneau
        GUI.DrawTexture(panel, backgroundTexture, ScaleMode.StretchToFill);
        // Bord/titre
        GUI.Box(panel, "── LOBBY ──", box);

        var inner = new Rect(panel.x + 12f, panel.y + 36f, panel.width - 24f, panel.height - 80f);
        scrollPos = GUI.BeginScrollView(inner,
            scrollPos,
            new Rect(0, 0, inner.width - 20f, EstimateContentHeight()));

        float y = 0f;
        y = DrawDivinitySection(0f, y, inner.width - 20f, lblRich, lbl, btn);
        y += 12f;
        y = DrawBlessingSection(0f, y, inner.width - 20f, lblRich, lbl, btn);

        GUI.EndScrollView();

        if (GUI.Button(new Rect(panel.x + 12f, panel.y + panel.height - 36f, panel.width - 24f, 28f), $"Fermer ({toggleKey})", btn))
            open = false;
    }

    float EstimateContentHeight()
    {
        float h = 0f;
        if (divinityCatalog != null) h += 60f + divinityCatalog.divinities.Count * 36f + 160f;
        if (blessingCatalog != null) h += 60f + blessingCatalog.blessings.Count * 110f;
        return h;
    }

    float DrawDivinitySection(float x, float y, float w, GUIStyle lblRich, GUIStyle lbl, GUIStyle btn)
    {
        GUI.Label(new Rect(x, y, w, 24f), "<b>── DIVINITÉ ──</b>", lblRich);
        y += 28f;

        if (divinityCatalog == null || divinityCatalog.divinities.Count == 0)
        {
            GUI.Label(new Rect(x, y, w, 22f), "(aucun catalog assigné)", lbl);
            return y + 24f;
        }

        for (int i = 0; i < divinityCatalog.divinities.Count; i++)
        {
            var d = divinityCatalog.divinities[i];
            if (d == null) continue;
            bool selected = DivinitySelection.Selected == d;
            string label = (selected ? "● " : "○ ") + d.displayName;
            if (GUI.Button(new Rect(x, y, w, 28f), label, btn))
                DivinitySelection.Select(d);
            y += 32f;
        }

        var current = DivinitySelection.Selected;
        if (current != null)
        {
            y += 6f;
            GUI.Label(new Rect(x, y, w, 22f), $"<b>{current.displayName}</b>", lblRich);
            y += 24f;
            GUI.Label(new Rect(x + 8f, y, w - 16f, 50f), current.description, lbl);
            y += 54f;
            GUI.Label(new Rect(x + 8f, y, w - 16f, 22f),
                $"+HP {current.bonusHP:+0;-0;0}   +Strength {current.bonusStrength:+0;-0;0}   Sang start {current.startBlood:F0}",
                lbl);
            y += 24f;
            GUI.Label(new Rect(x + 8f, y, w - 16f, 22f),
                $"Spells tir : {current.projectileSpells.Count}   sol : {current.groundSpells.Count}", lbl);
            y += 26f;
        }
        return y;
    }

    float DrawBlessingSection(float x, float y, float w, GUIStyle lblRich, GUIStyle lbl, GUIStyle btn)
    {
        GUI.Label(new Rect(x, y, w, 24f), "<b>── BÉNÉDICTIONS ──</b>", lblRich);
        y += 28f;

        GUI.Label(new Rect(x, y, w, 22f),
            $"XP disponible : <b>{AccountProgress.BlessingXp}</b>   |   Niveau {AccountProgress.Level}",
            lblRich);
        y += 28f;

        if (blessingCatalog == null || blessingCatalog.blessings.Count == 0)
        {
            GUI.Label(new Rect(x, y, w, 22f), "(aucun catalog assigné)", lbl);
            return y + 24f;
        }

        foreach (var b in blessingCatalog.blessings)
        {
            if (b == null) continue;
            int lvl = BlessingProgress.GetLevel(b);
            bool prereq = BlessingProgress.ArePrerequisitesMet(b);
            bool max = lvl >= b.maxLevel;
            string status = max ? "MAX" : $"L{lvl}/{b.maxLevel}";

            GUI.Label(new Rect(x, y, w, 22f),
                $"<b>{b.displayName}</b>  {status}", lblRich);
            y += 24f;
            GUI.Label(new Rect(x + 8f, y, w - 16f, 32f), b.description, lbl);
            y += 36f;

            if (!prereq)
            {
                GUI.Label(new Rect(x + 8f, y, w - 16f, 22f),
                    "Prérequis non remplis",
                    new GUIStyle(lbl) { normal = { textColor = new Color(1f, 0.5f, 0.5f) } });
                y += 26f;
            }
            else if (!max)
            {
                GUI.enabled = AccountProgress.BlessingXp >= b.xpPerLevel;
                if (GUI.Button(new Rect(x + 8f, y, w - 16f, 26f),
                        $"Acheter L{lvl + 1}  ({b.xpPerLevel} XP)", btn))
                    BlessingProgress.TryUpgrade(b);
                GUI.enabled = true;
                y += 30f;
            }

            GUI.Box(new Rect(x, y, w, 1f), GUIContent.none);
            y += 6f;
        }
        return y;
    }
}
