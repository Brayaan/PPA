using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PauseManagerTests
{
    private GameObject pauseManagerGO;
    private PauseManager pauseManager;
    private GameObject pauseMenuUI;
    private GameObject optionsMenuUI;

    [SetUp]
    public void SetUp()
    {
        pauseManagerGO = new GameObject("PauseManager");
        pauseManager = pauseManagerGO.AddComponent<PauseManager>();

        pauseMenuUI = new GameObject("PauseMenuUI");
        optionsMenuUI = new GameObject("OptionsMenuUI");

        pauseManager.pauseMenuUI = pauseMenuUI;
        pauseManager.optionsMenuUI = optionsMenuUI;
        
        // El Start se llamará automáticamente después de Awake en PlayMode, pero en Test a veces debemos forzarlo o esperar un frame
    }

    [TearDown]
    public void TearDown()
    {
        if (pauseManagerGO != null) Object.Destroy(pauseManagerGO);
        if (pauseMenuUI != null) Object.Destroy(pauseMenuUI);
        if (optionsMenuUI != null) Object.Destroy(optionsMenuUI);
        Time.timeScale = 1f; // Restaurar por si se quedó en 0
    }

    [UnityTest]
    public IEnumerator PauseManager_Resume_SetsTimeScaleToOneAndHidesUI()
    {
        yield return null; // Esperar al Start()

        // Simular que está pausado
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);

        pauseManager.Resume();

        Assert.AreEqual(1f, Time.timeScale, "Resume debe restaurar el timeScale a 1.");
        Assert.IsFalse(pauseMenuUI.activeSelf, "Resume debe ocultar la UI de pausa.");
    }

    [UnityTest]
    public IEnumerator PauseManager_OpenAndCloseOptions_TogglesCorrectUI()
    {
        yield return null;

        // Simular abriendo opciones
        pauseManager.OpenOptions();

        Assert.IsFalse(pauseMenuUI.activeSelf, "OpenOptions debe ocultar la UI principal.");
        Assert.IsTrue(optionsMenuUI.activeSelf, "OpenOptions debe mostrar la UI de opciones.");

        // Simular cerrando opciones
        pauseManager.CloseOptions();

        Assert.IsTrue(pauseMenuUI.activeSelf, "CloseOptions debe mostrar la UI principal.");
        Assert.IsFalse(optionsMenuUI.activeSelf, "CloseOptions debe ocultar la UI de opciones.");
    }
}
