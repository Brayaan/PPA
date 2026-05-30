using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer Groups")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Audio Clips (Efectos)")]
    public AudioClip punchSound;
    public AudioClip kickSound;
    public AudioClip blockSound;
    public AudioClip deathSound;
    public AudioClip roundStartSound; // "Fight!"

    [Header("Audio Clips (Música)")]
    public AudioClip matchStartJingle; // Música/sonido intro del Round 1
    public AudioClip backgroundMusic;

    [Header("Ajustes SFX")]
    [Range(0.8f, 1.2f)]
    public float pitchMin = 0.9f;
    [Range(0.8f, 1.2f)]
    public float pitchMax = 1.1f;

    // Reproductores de Audio Internos
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        // Al quitar DontDestroyOnLoad, este AudioManager morirá al salir de la escena de pelea,
        // permitiendo que la escena del menú vuelva a tocar su propia música limpia.
        Instance = this;
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // Crear el reproductor de música
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Crear el reproductor de SFX
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.outputAudioMixerGroup = sfxGroup;
        sfxSource.playOnAwake = false;

        // Lógica de intro musical
        if (matchStartJingle != null)
        {
            // Reproducir el jingle en el canal de música o sfx (usaremos sfx para que no lo corte el loop)
            sfxSource.PlayOneShot(matchStartJingle);
            
            if (backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.PlayDelayed(matchStartJingle.length); // La música espera a que termine el jingle
            }
        }
        else if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    // CA-01: Reproducir sonido de golpe según tipo (con Pitch Variation)
    public void PlayHitSound(string attackName)
    {
        AudioClip clipToPlay = punchSound; // Por defecto

        if (!string.IsNullOrEmpty(attackName))
        {
            // Detectar si el ataque se llama Kick, Patada, etc.
            if (attackName.ToLower().Contains("kick") || attackName.ToLower().Contains("patada"))
            {
                clipToPlay = kickSound;
            }
        }

        if (clipToPlay != null)
        {
            // Pitch Variation: cambia sutilmente el tono para que no suene repetitivo
            sfxSource.pitch = Random.Range(pitchMin, pitchMax);
            sfxSource.PlayOneShot(clipToPlay);
        }
    }

    // CA-03: Sonido de bloqueo
    public void PlayBlockSound()
    {
        if (blockSound != null)
        {
            // El bloqueo suele tener un pitch más constante, pero variarlo levemente es bueno
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(blockSound);
        }
    }

    // CA-04: Sonido de inicio de partida
    public void PlayRoundStartSound()
    {
        if (roundStartSound != null)
        {
            sfxSource.pitch = 1f; // El anunciador siempre debe sonar normal
            sfxSource.PlayOneShot(roundStartSound);
        }
    }

    // Sonido al morir
    public void PlayDeathSound()
    {
        if (deathSound != null)
        {
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(deathSound);
        }
    }
}
