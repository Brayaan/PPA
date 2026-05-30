using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AudioManagerTests
{
    private GameObject audioManagerGO;
    private AudioManager audioManager;

    [SetUp]
    public void SetUp()
    {
        // Limpiar
        if (AudioManager.Instance != null)
        {
            Object.DestroyImmediate(AudioManager.Instance.gameObject);
        }

        audioManagerGO = new GameObject("AudioManager");
        audioManager = audioManagerGO.AddComponent<AudioManager>();
        
        // Simular clips
        audioManager.punchSound = AudioClip.Create("Punch", 1, 1, 1000, false);
        audioManager.kickSound = AudioClip.Create("Kick", 1, 1, 1000, false);
        audioManager.blockSound = AudioClip.Create("Block", 1, 1, 1000, false);
        audioManager.deathSound = AudioClip.Create("Death", 1, 1, 1000, false);
    }

    [TearDown]
    public void TearDown()
    {
        if (audioManagerGO != null)
        {
            Object.Destroy(audioManagerGO);
        }
    }

    [Test]
    public void AudioManager_Singleton_IsCreated()
    {
        Assert.IsNotNull(AudioManager.Instance, "El singleton de AudioManager debe estar asignado.");
        Assert.AreEqual(audioManager, AudioManager.Instance, "La instancia debe ser la creada en el test.");
    }

    [UnityTest]
    public IEnumerator AudioManager_PlayHitSound_PlaysCorrectSoundAndPitch()
    {
        // Ejecutar un frame para que Awake/SetupAudioSources se inicien completamente
        yield return null;

        AudioSource[] sources = audioManager.GetComponents<AudioSource>();
        // sources[0] es music, sources[1] es sfx segun el script original
        Assert.IsTrue(sources.Length >= 2, "AudioManager deberia crear AudioSources para musica y SFX.");

        AudioSource sfxSource = sources[1];

        // Probar golpe normal
        audioManager.PlayHitSound("Punch");
        Assert.IsTrue(sfxSource.pitch >= audioManager.pitchMin && sfxSource.pitch <= audioManager.pitchMax, "Pitch deberia variar.");

        // Probar patada
        audioManager.PlayHitSound("Kick");
        Assert.IsTrue(sfxSource.pitch >= audioManager.pitchMin && sfxSource.pitch <= audioManager.pitchMax, "Pitch deberia variar.");
    }
}
