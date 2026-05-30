using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Jugar : MonoBehaviour
{
    // Nombre de la escena a cargar
    public string batalla;

    [Header("Botones del Menú Principal")]
    public Button botonJugar;
    public Button botonOpciones;
    public Button botonSalir;

    // Función que se conecta al botón de Jugar
    public void CargarEscena()
    {
        SceneManager.LoadScene(batalla);
    }

    // Función que se conecta al botón de Salir
    public void SalirJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit(); // Cierra el juego exportado (.exe)

        // Detiene el juego si estás probando dentro del editor de Unity
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // ==========================================
    // SECCIÓN DE OPCIONES (AUDIO Y CONTROLES)
    // ==========================================
    
    [Header("Paneles Principales")]
    public GameObject panelPrincipal; // El panel llamado "panel" que tiene Jugar, Opciones, Salir

    [Header("Paneles de Opciones")]
    public GameObject panelOpcionesBase; // El que tiene los 2 botones (Audio / Controles)
    public GameObject panelTutorial;     // La imagen de controles
    public GameObject panelAudio;        // El panel con los sliders de volumen

    [Header("Configuración de Audio")]
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        // Asegurarnos de que los menús estén en el estado correcto al iniciar
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelOpcionesBase != null) panelOpcionesBase.SetActive(false);
        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (panelAudio != null) panelAudio.SetActive(false);

        // Inicializar sliders con el volumen guardado en el Mixer
        if (audioMixer != null)
        {
            float musicVol;
            if (musicSlider != null && audioMixer.GetFloat("MusicVolume", out musicVol))
                musicSlider.value = Mathf.Pow(10, musicVol / 20);

            float sfxVol;
            if (sfxSlider != null && audioMixer.GetFloat("SFXVolume", out sfxVol))
                sfxSlider.value = Mathf.Pow(10, sfxVol / 20);
        }
    }

    // 1. Navegación del Menú Base de Opciones
    public void AbrirOpcionesBase()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(false); // Oculta el panel principal
        if (panelOpcionesBase != null) panelOpcionesBase.SetActive(true); // Muestra el panel de opciones
    }
    public void CerrarOpcionesBase()
    {
        if (panelOpcionesBase != null) panelOpcionesBase.SetActive(false); // Oculta el panel de opciones
        if (panelPrincipal != null) panelPrincipal.SetActive(true); // Vuelve a mostrar el panel principal
    }

    // 2. Navegación de Controles (Tutorial)
    public void AbrirTutorial()
    {
        if (panelOpcionesBase != null) panelOpcionesBase.SetActive(false);
        if (panelTutorial != null) panelTutorial.SetActive(true);
    }
    public void CerrarTutorial()
    {
        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (panelOpcionesBase != null) panelOpcionesBase.SetActive(true); // Volver atrás
    }

    // 3. Navegación de Audio
    public void AbrirAudio()
    {
        if (panelOpcionesBase != null) panelOpcionesBase.SetActive(false);
        if (panelAudio != null) panelAudio.SetActive(true);
    }
    public void CerrarAudio()
    {
        if (panelAudio != null) panelAudio.SetActive(false);
        if (panelOpcionesBase != null) panelOpcionesBase.SetActive(true); // Volver atrás
    }

    // ==========================================
    // LOGICA DE VOLUMEN (Mixer)
    // ==========================================
    public void SetMusicVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            float decibels = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
            audioMixer.SetFloat("MusicVolume", decibels);
        }
    }

    public void SetSFXVolume(float sliderValue)
    {
        if (audioMixer != null)
        {
            float decibels = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
            audioMixer.SetFloat("SFXVolume", decibels);
        }
    }
}