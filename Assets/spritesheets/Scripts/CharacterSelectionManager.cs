using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Base de Datos")]
    public List<CharacterData> availableCharacters;

    [Header("Imágenes del Carrusel (Estilo Netflix)")]
    public Image leftImage;
    public Image centerImage; // El personaje actual/seleccionado
    public Image rightImage;

    [Header("Textos")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;

    [Header("Botones y Flechas")]
    public Button leftArrow;
    public Button rightArrow;
    public Button selectButton;

    [Header("Escenas")]
    public string battleSceneName = "Batalla";
    public string menuSceneName = "Menu"; // El nombre de tu escena del menú principal

    private int currentIndex = 0;
    
    // Variables para la animación
    private float animationTimer = 0f;
    private int currentAnimationFrame = 0;

    void Start()
    {
        // Validación CA-04: Sin personajes disponibles
        if (availableCharacters == null || availableCharacters.Count == 0)
        {
            Debug.LogError("No hay personajes en la lista. Bloqueando el menú.");
            leftArrow.gameObject.SetActive(false);
            rightArrow.gameObject.SetActive(false);
            if (selectButton != null) selectButton.gameObject.SetActive(false);
            return;
        }

        // Inicializar el carrusel en el primer personaje
        currentIndex = 0;
        UpdateCarouselUI();
    }

    void Update()
    {
        // Permitir usar flechas del teclado
        if (availableCharacters != null && availableCharacters.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) NextCharacter();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) PreviousCharacter();
            if (Input.GetKeyDown(KeyCode.Return)) SelectCharacter();

            // Lógica de Animación (Reproducir los PNGs como GIF solo para el del centro)
            if (centerImage != null && centerImage.gameObject.activeSelf)
            {
                CharacterData currentCharacter = availableCharacters[currentIndex];
                if (currentCharacter.idleAnimation != null && currentCharacter.idleAnimation.Length > 0)
                {
                    animationTimer += Time.deltaTime;
                    if (animationTimer >= currentCharacter.animationSpeed)
                    {
                        animationTimer = 0f;
                        currentAnimationFrame = (currentAnimationFrame + 1) % currentCharacter.idleAnimation.Length;
                        centerImage.sprite = currentCharacter.idleAnimation[currentAnimationFrame];
                    }
                }
            }
        }
    }

    public void NextCharacter()
    {
        currentIndex = (currentIndex + 1) % availableCharacters.Count;
        UpdateCarouselUI();
    }

    public void PreviousCharacter()
    {
        currentIndex = (currentIndex - 1 + availableCharacters.Count) % availableCharacters.Count;
        UpdateCarouselUI();
    }

    private void UpdateCarouselUI()
    {
        CharacterData currentCharacter = availableCharacters[currentIndex];

        // Mostrar textos con formato profesional (Rich Text de TextMeshPro)
        if (nameText != null) 
            nameText.text = $"<color=#FFD700>»</color> {currentCharacter.characterName.ToUpper()} <color=#FFD700>«</color>";
            
        if (descriptionText != null) 
            descriptionText.text = $"<color=#888888><size=80%>DATOS DEL LUCHADOR</size></color>\n<color=#FFFFFF>{currentCharacter.description}</color>";
        
        // Reiniciar la animación al cambiar de personaje para el del centro
        currentAnimationFrame = 0;
        animationTimer = 0f;
        
        // --- IMAGEN CENTRAL (El seleccionado / animado) ---
        if (centerImage != null)
        {
            centerImage.gameObject.SetActive(true);
            if (currentCharacter.idleAnimation != null && currentCharacter.idleAnimation.Length > 0)
                centerImage.sprite = currentCharacter.idleAnimation[0]; 
        }

        // Calcular índices para hacer el carrusel infinito (circular)
        int leftIndex = (currentIndex - 1 + availableCharacters.Count) % availableCharacters.Count;
        int rightIndex = (currentIndex + 1) % availableCharacters.Count;

        // --- IMAGEN IZQUIERDA (Estática) ---
        if (leftImage != null)
        {
            leftImage.gameObject.SetActive(true);
            CharacterData leftChar = availableCharacters[leftIndex];
            if (leftChar.idleAnimation != null && leftChar.idleAnimation.Length > 0)
                leftImage.sprite = leftChar.idleAnimation[0];
        }

        // --- IMAGEN DERECHA (Estática) ---
        if (rightImage != null)
        {
            rightImage.gameObject.SetActive(true);
            CharacterData rightChar = availableCharacters[rightIndex];
            if (rightChar.idleAnimation != null && rightChar.idleAnimation.Length > 0)
                rightImage.sprite = rightChar.idleAnimation[0];
        }

        // Flechas siempre activas porque el carrusel ahora es infinito
        if (leftArrow != null) leftArrow.interactable = true;
        if (rightArrow != null) rightArrow.interactable = true;
    }

    public void SelectCharacter()
    {
        // Como es solo el catálogo por ahora, el botón Seleccionar simplemente
        // avanza al combate según el flujo: menú -> carrusel -> combate.
        Debug.Log("Saliendo del carrusel, cargando escena de batalla...");
        SceneManager.LoadScene(battleSceneName);
    }

    public void VolverAlMenu()
    {
        Debug.Log("Volviendo al menú principal...");
        SceneManager.LoadScene(menuSceneName);
    }
}
