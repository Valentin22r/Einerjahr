using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Contrôleur central du lobby (WeaponUpgrade). Branche tes boutons UGUI sur
/// ses méthodes publiques. Aucune dépendance forte : tous les TMP_Text /
/// Button refs sont optionnels.
/// </summary>
[DisallowMultipleComponent]
public class LobbyController : MonoBehaviour
{
    [Header("Divinité")]
    public DivinityCatalog catalog;
    [Tooltip("Parent UGUI où les boutons de divinité seront auto-générés. Laisse vide pour gérer toi-même les boutons (et appeler SelectDivinity).")]
    public RectTransform divinityButtonsContainer;
    [Tooltip("Prefab de bouton (Button + TMP_Text en enfant). Instancié pour chaque divinité du catalog.")]
    public Button divinityButtonPrefab;

    [Header("Affichage divinité sélectionnée")]
    public TMP_Text selectedDivinityNameText;
    public TMP_Text selectedDivinityDescriptionText;
    public TMP_Text selectedDivinityStatsText;
    public Image selectedDivinityIcon;

    [Header("Affichage compte (XP / niveau)")]
    public TMP_Text accountLevelText;
    public TMP_Text accountXpText;
    public Image accountXpBar; // Image en mode Filled (Horizontal)

    [Header("Affichage économie / upgrades")]
    public TMP_Text mythrilText;
    public TMP_Text weaponDamageLevelText;
    public TMP_Text weaponRangeLevelText;
    public TMP_Text weaponSpeedLevelText;
    public Button damageUpgradeButton;
    public Button rangeUpgradeButton;
    public Button speedUpgradeButton;
    public TMP_Text damageUpgradeCostText;
    public TMP_Text rangeUpgradeCostText;
    public TMP_Text speedUpgradeCostText;

    [Header("Difficulté")]
    public TMP_Text difficultyText;
    public Button difficultyPrevButton;
    public Button difficultyNextButton;

    [Header("Play")]
    public string gameSceneName = "SampleScene";
    public Button playButton;

    [Header("Debug")]
    public bool showDebugButtons = false;

    readonly List<Button> spawnedDivinityButtons = new List<Button>();

    void Start()
    {
        EnsureDefaultDivinity();
        BuildDivinityButtons();

        if (difficultyPrevButton != null) difficultyPrevButton.onClick.AddListener(PrevDifficulty);
        if (difficultyNextButton != null) difficultyNextButton.onClick.AddListener(NextDifficulty);

        RefreshAll();
    }

    void Update()
    {
        RefreshAll();
    }

    void EnsureDefaultDivinity()
    {
        if (catalog == null) return;
        if (DivinitySelection.Selected == null)
            DivinitySelection.Select(catalog.defaultDivinity != null
                ? catalog.defaultDivinity
                : (catalog.divinities.Count > 0 ? catalog.divinities[0] : null));
    }

    void BuildDivinityButtons()
    {
        if (catalog == null || divinityButtonsContainer == null || divinityButtonPrefab == null) return;

        foreach (var b in spawnedDivinityButtons) if (b != null) Destroy(b.gameObject);
        spawnedDivinityButtons.Clear();

        for (int i = 0; i < catalog.divinities.Count; i++)
        {
            var d = catalog.divinities[i];
            if (d == null) continue;
            var btn = Instantiate(divinityButtonPrefab, divinityButtonsContainer);
            btn.gameObject.SetActive(true);
            btn.name = $"DivinityButton_{d.displayName}";

            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = d.displayName;

            var captured = d;
            btn.onClick.AddListener(() => SelectDivinity(captured));
            spawnedDivinityButtons.Add(btn);
        }
    }

    void RefreshAll()
    {
        RefreshDivinityDisplay();
        RefreshUpgradeDisplay();
        RefreshDifficultyDisplay();
        RefreshPlayButton();
    }

    void RefreshDifficultyDisplay()
    {
        if (difficultyText != null) difficultyText.text = $"Difficulté : {DifficultySelection.Current}";
    }

    public void NextDifficulty()
    {
        int n = System.Enum.GetValues(typeof(Difficulty)).Length;
        DifficultySelection.Current = (Difficulty)(((int)DifficultySelection.Current + 1) % n);
    }

    public void PrevDifficulty()
    {
        int n = System.Enum.GetValues(typeof(Difficulty)).Length;
        DifficultySelection.Current = (Difficulty)(((int)DifficultySelection.Current - 1 + n) % n);
    }

    void RefreshDivinityDisplay()
    {
        var d = DivinitySelection.Selected;
        if (selectedDivinityNameText != null)
            selectedDivinityNameText.text = d != null ? d.displayName : "—";
        if (selectedDivinityDescriptionText != null)
            selectedDivinityDescriptionText.text = d != null ? d.description : "";
        if (selectedDivinityStatsText != null && d != null)
            selectedDivinityStatsText.text =
                $"+HP {d.bonusHP:+0;-0;0}   +Strength {d.bonusStrength:+0;-0;0}\n" +
                $"Sang start : {d.startBlood:F0}\n" +
                $"Spells tir : {d.projectileSpells.Count}   sol : {d.groundSpells.Count}";
        if (selectedDivinityIcon != null)
        {
            selectedDivinityIcon.sprite = d != null ? d.icon : null;
            selectedDivinityIcon.enabled = d != null && d.icon != null;
        }

        for (int i = 0; i < spawnedDivinityButtons.Count && i < catalog.divinities.Count; i++)
        {
            var btn = spawnedDivinityButtons[i];
            if (btn == null) continue;
            var div = catalog.divinities[i];
            bool selected = div == DivinitySelection.Selected;
            var colors = btn.colors;
            colors.normalColor = selected ? new Color(0.9f, 0.7f, 0.3f, 1f) : Color.white;
            btn.colors = colors;
        }
    }

    void RefreshUpgradeDisplay()
    {
        int mythril = PlayerProgress.Mythril;
        if (mythrilText != null) mythrilText.text = $"Mythril : {mythril}";

        int lvl = AccountProgress.Level;
        if (accountLevelText != null) accountLevelText.text = $"Niveau {lvl}";
        int into = AccountProgress.XpIntoCurrentLevel;
        int need = AccountProgress.XpNeededForNextLevel;
        if (accountXpText != null) accountXpText.text = need > 0 ? $"{into} / {need} XP" : "MAX";
        if (accountXpBar != null) accountXpBar.fillAmount = need > 0 ? (float)into / need : 1f;

        SetLevelText(weaponDamageLevelText, "Dégâts", PlayerProgress.WeaponDamageLevel);
        SetLevelText(weaponRangeLevelText, "Portée", PlayerProgress.WeaponRangeLevel);
        SetLevelText(weaponSpeedLevelText, "Vitesse", PlayerProgress.WeaponSpeedLevel);

        SetUpgradeButton(damageUpgradeButton, damageUpgradeCostText, PlayerProgress.WeaponDamageLevel, mythril);
        SetUpgradeButton(rangeUpgradeButton, rangeUpgradeCostText, PlayerProgress.WeaponRangeLevel, mythril);
        SetUpgradeButton(speedUpgradeButton, speedUpgradeCostText, PlayerProgress.WeaponSpeedLevel, mythril);
    }

    void SetLevelText(TMP_Text text, string label, int level)
    {
        if (text != null) text.text = $"{label} L{level}";
    }

    void SetUpgradeButton(Button btn, TMP_Text costText, int currentLevel, int mythril)
    {
        int cost = PlayerProgress.CostForLevel(currentLevel);
        if (costText != null) costText.text = $"{cost} Mythril";
        if (btn != null) btn.interactable = mythril >= cost;
    }

    void RefreshPlayButton()
    {
        if (playButton == null) return;
        playButton.interactable = DivinitySelection.Selected != null && !string.IsNullOrEmpty(gameSceneName);
    }

    // ─── API publique pour brancher sur les OnClick UGUI ──────────────────

    public void SelectDivinity(DivinityData d)
    {
        DivinitySelection.Select(d);
    }

    public void SelectDivinityByIndex(int index)
    {
        if (catalog == null || index < 0 || index >= catalog.divinities.Count) return;
        SelectDivinity(catalog.divinities[index]);
    }

    public void UpgradeDamage()
    {
        int cost = PlayerProgress.CostForLevel(PlayerProgress.WeaponDamageLevel);
        if (PlayerProgress.TrySpendMythril(cost)) PlayerProgress.WeaponDamageLevel++;
    }

    public void UpgradeRange()
    {
        int cost = PlayerProgress.CostForLevel(PlayerProgress.WeaponRangeLevel);
        if (PlayerProgress.TrySpendMythril(cost)) PlayerProgress.WeaponRangeLevel++;
    }

    public void UpgradeSpeed()
    {
        int cost = PlayerProgress.CostForLevel(PlayerProgress.WeaponSpeedLevel);
        if (PlayerProgress.TrySpendMythril(cost)) PlayerProgress.WeaponSpeedLevel++;
    }

    public void Play()
    {
        if (DivinitySelection.Selected == null && catalog != null)
            DivinitySelection.Select(catalog.defaultDivinity);
        if (DivinitySelection.Selected == null) return;
        if (string.IsNullOrEmpty(gameSceneName)) return;
        SceneManager.LoadScene(gameSceneName);
    }

    [ContextMenu("[Debug] +100 Mythril")]
    public void Debug_Grant100Mythril() => PlayerProgress.GrantMythril(100);

    [ContextMenu("[Debug] Reset progression")]
    public void Debug_ResetProgression() => PlayerProgress.ResetAll();
}
