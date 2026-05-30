using UnityEngine;
using TMPro;
using System.Collections;

// Requiere un CanvasGroup en el mismo GameObject
[RequireComponent(typeof(CanvasGroup))]
public class WinScreenAnimation : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public TMP_Text resultText; // Texto que se anima al mostrar la pantalla

    void Awake()
    {
        // Obtenemos el CanvasGroup al iniciar
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        // Cuando el panel se enciende, reiniciamos su estado para animarlo
        canvasGroup.alpha = 0f; // Panel invisible al inicio

        // Texto pequeño listo para crecer
        if (resultText != null)
            resultText.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        StartCoroutine(AnimateUI()); // Lanzamos la animación
    }

    IEnumerator AnimateUI()
    {
        float duration = 0.6f; // Duración total de la animación
        float elapsed = 0f;    // Tiempo transcurrido

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // Progreso de 0 a 1

            // 1. Efecto Fade In (Aparece suavemente la pantalla oscura)
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);

            // 2. Efecto de crecimiento y "golpe" en el texto
            if (resultText != null)
            {
                // Curva Ease-Out (Rápido al inicio, lento al final)
                float scaleT = 1f - Mathf.Pow(1f - t, 3f);
                resultText.transform.localScale = Vector3.Lerp(new Vector3(0.1f, 0.1f, 0.1f), Vector3.one, scaleT);
            }

            yield return null; // Esperamos al siguiente frame
        }

        // Asegurarnos de que termine exacto en tamaño 1
        if (resultText != null)
            resultText.transform.localScale = Vector3.one;
    }
}