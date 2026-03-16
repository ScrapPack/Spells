using UnityEngine;

/// <summary>
/// ScriptableObject representing a sound event with randomization.
/// Decouples "what should play" from "how it plays" — systems reference
/// AudioEvents by type, the audio system resolves them to actual clips.
///
/// Supports: random clip selection, pitch/volume randomization,
/// cooldown (prevent spam), and spatial blend for 2D/3D mixing.
///
/// Usage: Create AudioEvent assets, assign to CombatData or ClassData,
/// play via AudioManager (not yet built — this is the data layer).
/// </summary>
[CreateAssetMenu(fileName = "AudioEvent", menuName = "Spells/Audio Event")]
public class AudioEvent : ScriptableObject
{
    [Header("Clips")]
    [Tooltip("One or more audio clips — a random one is chosen each play")]
    public AudioClip[] clips;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volumeMin = 0.8f;
    [Range(0f, 1f)]
    public float volumeMax = 1f;

    [Header("Pitch")]
    [Range(0.5f, 2f)]
    public float pitchMin = 0.95f;
    [Range(0.5f, 2f)]
    public float pitchMax = 1.05f;

    [Header("Cooldown")]
    [Tooltip("Minimum seconds between plays of this event (prevent spam)")]
    public float cooldown = 0f;

    [Header("Spatial")]
    [Range(0f, 1f)]
    [Tooltip("0 = 2D (full stereo), 1 = 3D (positional)")]
    public float spatialBlend = 0f;

    /// <summary>
    /// Get a random clip from the pool.
    /// Returns null if no clips assigned.
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    /// <summary>
    /// Get randomized volume within the min/max range.
    /// </summary>
    public float GetRandomVolume()
    {
        return Random.Range(volumeMin, volumeMax);
    }

    /// <summary>
    /// Get randomized pitch within the min/max range.
    /// </summary>
    public float GetRandomPitch()
    {
        return Random.Range(pitchMin, pitchMax);
    }
}
