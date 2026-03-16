using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Per-player HUD showing HP pips, ammo count, and active power card icons.
/// Subscribes to HealthSystem, ProjectileSpawner, and CardInventory events.
/// Designed for 4 simultaneous HUDs (one per player corner).
/// </summary>
public class CombatHUD : MonoBehaviour
{
    [Header("HP Display")]
    [SerializeField] private Image[] hpPips;
    [SerializeField] private Color hpFullColor = Color.red;
    [SerializeField] private Color hpEmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Header("Ammo Display")]
    [SerializeField] private GameObject ammoContainer;
    [SerializeField] private Image[] ammoPips;
    [SerializeField] private Color ammoFullColor = Color.yellow;
    [SerializeField] private Color ammoEmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    [Header("Class Info")]
    [SerializeField] private Image classIcon;
    [SerializeField] private TextMeshProUGUI classNameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Cards")]
    [SerializeField] private Transform cardIconContainer;

    [Header("Round")]
    [SerializeField] private TextMeshProUGUI roundWinsText;

    private HealthSystem health;
    private ProjectileSpawner spawner;
    private int lastAmmo = -1;

    /// <summary>
    /// Bind this HUD to a player. Call after ClassManager.Initialize.
    /// </summary>
    public void Bind(GameObject player, ClassData classData)
    {
        health = player.GetComponent<HealthSystem>();
        spawner = player.GetComponent<ProjectileSpawner>();

        if (health != null)
        {
            health.OnHealthChanged.AddListener(UpdateHP);
            UpdateHP(health.CurrentHP, health.MaxHP);
        }

        // Class info
        if (classIcon != null && classData.classIcon != null)
            classIcon.sprite = classData.classIcon;
        if (classNameText != null)
            classNameText.text = classData.className;

        // Show/hide ammo display based on class
        if (ammoContainer != null)
            ammoContainer.SetActive(classData.combatData != null && classData.combatData.maxAmmo > 0);
    }

    private void UpdateHP(int current, int max)
    {
        if (hpPips == null) return;

        for (int i = 0; i < hpPips.Length; i++)
        {
            if (hpPips[i] == null) continue;
            hpPips[i].gameObject.SetActive(i < max);
            hpPips[i].color = i < current ? hpFullColor : hpEmptyColor;
        }
    }

    private void Update()
    {
        // Update ammo display (polled because ProjectileSpawner doesn't have events for ammo)
        if (spawner != null && ammoPips != null)
        {
            int currentAmmo = spawner.CurrentAmmo;
            if (currentAmmo != lastAmmo)
            {
                lastAmmo = currentAmmo;
                for (int i = 0; i < ammoPips.Length; i++)
                {
                    if (ammoPips[i] == null) continue;
                    ammoPips[i].color = i < currentAmmo ? ammoFullColor : ammoEmptyColor;
                }
            }
        }
    }

    /// <summary>
    /// Update round wins display.
    /// </summary>
    public void SetRoundWins(int wins)
    {
        if (roundWinsText != null)
            roundWinsText.text = wins.ToString();
    }

    /// <summary>
    /// Update level display.
    /// </summary>
    public void SetLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"Lv.{level}";
    }

    public void Unbind()
    {
        if (health != null)
            health.OnHealthChanged.RemoveListener(UpdateHP);

        health = null;
        spawner = null;
    }
}
