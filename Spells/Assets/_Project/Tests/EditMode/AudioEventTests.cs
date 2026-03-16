using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests for AudioEvent ScriptableObject.
/// </summary>
[TestFixture]
public class AudioEventTests
{
    private AudioEvent audioEvent;

    [SetUp]
    public void SetUp()
    {
        audioEvent = ScriptableObject.CreateInstance<AudioEvent>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(audioEvent);
    }

    [Test]
    public void GetRandomClip_NoClips_ReturnsNull()
    {
        audioEvent.clips = null;
        Assert.IsNull(audioEvent.GetRandomClip());
    }

    [Test]
    public void GetRandomClip_EmptyArray_ReturnsNull()
    {
        audioEvent.clips = new AudioClip[0];
        Assert.IsNull(audioEvent.GetRandomClip());
    }

    [Test]
    public void GetRandomVolume_WithinRange()
    {
        audioEvent.volumeMin = 0.5f;
        audioEvent.volumeMax = 0.8f;

        for (int i = 0; i < 50; i++)
        {
            float vol = audioEvent.GetRandomVolume();
            Assert.GreaterOrEqual(vol, 0.5f);
            Assert.LessOrEqual(vol, 0.8f);
        }
    }

    [Test]
    public void GetRandomPitch_WithinRange()
    {
        audioEvent.pitchMin = 0.9f;
        audioEvent.pitchMax = 1.1f;

        for (int i = 0; i < 50; i++)
        {
            float pitch = audioEvent.GetRandomPitch();
            Assert.GreaterOrEqual(pitch, 0.9f);
            Assert.LessOrEqual(pitch, 1.1f);
        }
    }

    [Test]
    public void DefaultVolume_IsNearFull()
    {
        Assert.GreaterOrEqual(audioEvent.volumeMin, 0.7f, "Default volume should be reasonably loud");
        Assert.LessOrEqual(audioEvent.volumeMax, 1f);
    }

    [Test]
    public void DefaultPitch_IsNearNormal()
    {
        Assert.GreaterOrEqual(audioEvent.pitchMin, 0.8f, "Default pitch shouldn't be too low");
        Assert.LessOrEqual(audioEvent.pitchMax, 1.2f, "Default pitch shouldn't be too high");
    }

    [Test]
    public void DefaultSpatialBlend_Is2D()
    {
        Assert.AreEqual(0f, audioEvent.spatialBlend,
            "Default should be 2D (full stereo) for a side-view game");
    }
}
