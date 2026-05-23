using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class WeaponUI : MonoBehaviour
{
    private WeaponStats weaponStats;
    private PlayerStats playerStats;

    public TextMeshProUGUI damageText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI mythrilText;

    public int damageUpgradeCost = 50;
    public int rangeUpgradeCost = 30;
    public int speedUpgradeCost = 40;

    private void Start()
    {
        weaponStats = FindObjectOfType<WeaponStats>();
        playerStats = FindObjectOfType<PlayerStats>();
    }
    private void Update()
    {
        Debug.Log(damageText);
        Debug.Log(weaponStats);
        UpdateUI();
    }

    void UpdateUI()
    {
        damageText.text = "Damage: " + weaponStats.Damage;

        rangeText.text = "Range: " + weaponStats.Range;

        speedText.text = "Attack Timer: " + weaponStats.Attack_Timer;

        mythrilText.text = "Mythril: " + playerStats.Mythril;
    }

    public void UpgradeDamage()
    {
        if (playerStats.Mythril >= damageUpgradeCost)
        {
            playerStats.Mythril -= damageUpgradeCost;
            weaponStats.UpgradeAttack();
        }
    }

    public void UpgradeRange()
    {
        if (playerStats.Mythril >= rangeUpgradeCost)
        {
            playerStats.Mythril -= rangeUpgradeCost;
            weaponStats.UpgradeRanges();
        }
    }

    public void UpgradeSpeed()
    {
        if (playerStats.Mythril >= speedUpgradeCost)
        {
            playerStats.Mythril -= speedUpgradeCost;
            weaponStats.UpgradeAttackTimer();
        }
    }

    public void BackToGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}