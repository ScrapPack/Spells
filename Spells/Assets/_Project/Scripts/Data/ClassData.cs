using UnityEngine;

/// <summary>
/// Defines a playable class (Wizard, Warrior, etc.).
/// References a CombatData for combat configuration and a projectile prefab.
/// ClassManager applies this data to a player at spawn time.
/// </summary>
[CreateAssetMenu(fileName = "ClassData", menuName = "Spells/Class Data")]
public class ClassData : ScriptableObject
{
    [Header("Identity")]
    public string className = "Wizard";
    [TextArea(2, 4)]
    public string description = "Jack-of-all-trades caster with rapid arcane bolts.";

    [Header("Combat Configuration")]
    [Tooltip("CombatData asset defining HP, projectile, knockback, parry for this class")]
    public CombatData combatData;

    [Header("Projectile")]
    [Tooltip("Prefab spawned when this class shoots")]
    public GameObject projectilePrefab;

    [Header("Card Pool")]
    [Tooltip("Tags identifying which power card pools this class draws from")]
    public string[] cardPoolTags = new string[] { "General" };

    [Header("Visual")]
    [Tooltip("Primary color for this class (UI, projectile tint, etc.)")]
    public Color classColor = Color.white;
    [Tooltip("Sprite used in character select and HUD")]
    public Sprite classIcon;
}
