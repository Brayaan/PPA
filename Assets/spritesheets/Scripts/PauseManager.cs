using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI Menú de Pausa")]
    public GameObject pauseMenuUI;
    public GameObject optionsMenuUI; // Panel separado para los sliders

    [Header("Configuración de Escenas")]
    public string mainMenuSceneName = "MenuPrincipal";

    [Header("Audio Mixer (Volumen Independiente)")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    private bool isPaused = false;

    void Start()
    {
        // Asegurarnos de que el menú de pausa esté oculto al iniciar
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);

        // Inicializar sliders con los valores actuales del Mixer si están asignados
        if (audioMixer != null)
        {
            float musicVol;
            if (musicSlider != null && audioMixer.GetFloat("MusicVolume", out musicVol))
                musicSlider.value = Mathf.Pow(10, musicVol / 20); // Convertir DB a lineal

            float sfxVol;
            if (sfxSlider != null && audioMixer.GetFloat("SFXVolume", out sfxVol))
                sfxSlider.value = Mathf.Pow(10, sfxVol / 20); // Convertir DB a lineal
        }
    }

    void Update()
    {
        // CA-01: Activar pausa con la tecla Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // CA-06: Camino triste - No permitir pausa durante animación de victoria
            if (CombatManager.Instance != null && (!CombatManager.Instance.isRoundActive || CombatManager.Instance.isCombatEnded))
            {
                Debug.Log("No se puede pausar durante una transición o finalización de round.");
                return;
            }

            if (isPaused)
            {
                // Si estamos en el menú de opciones, Esc vuelve al menú de pausa principal
                if (optionsMenuUI != null && optionsMenuUI.activeSelf)
                {
                    CloseOptions();
                }
                else
                {
                    Resume();
                }
            }
            else
            {
                Pause();
            }
        }
    }

    // CA-02: Reanudar partida
    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);
        Time.timeScale = 1f; // Descongelar el juego
        isPaused = false;
    }

    void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);
        Time.timeScale = 0f; // Congelar físicas, movimientos y timers
        isPaused = true;
    }

    // Submenú de Opciones
    public void OpenOptions()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
    }

    // CA-03: Reiniciar partida
    public void RestartMatch()
    {
        Resume(); // Asegurarnos de que el tiempo vuelva a la normalidad
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RestartMatch(); // Reinicia el combate limpio
        }
    }

    // CA-04: Volver al menú principal
    public void LoadMainMenu()
    {
        Resume(); // Descongelar antes de cambiar de escena
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // CA-05: Ajuste de volumen (Música)
    public void SetMusicVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            // Convertir de escala lineal (0.0001 a 1) a logarítmica (Decibelios)
            float decibels = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
            audioMixer.SetFloat("MusicVolume", decibels);
        }
    }

    // CA-05: Ajuste de volumen (SFX)
    public void SetSFXVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            float decibels = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
            audioMixer.SetFloat("SFXVolume", decibels);
        }
    }
}
