using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class CharacterSelectionManagerTests
{
    private GameObject selectionManagerGO;
    private CharacterSelectionManager selectionManager;

    [SetUp]
    public void SetUp()
    {
        selectionManagerGO = new GameObject("CharacterSelectionManager");
        // Desactivar el GameObject para que Start() no se ejecute todavía
        selectionManagerGO.SetActive(false);
        
        selectionManager = selectionManagerGO.AddComponent<CharacterSelectionManager>();

        // Configurar botones
        selectionManager.leftArrow = new GameObject("LeftArrow").AddComponent<Button>();
        selectionManager.rightArrow = new GameObject("RightArrow").AddComponent<Button>();
        selectionManager.selectButton = new GameObject("SelectButton").AddComponent<Button>();
    }

    [TearDown]
    public void TearDown()
    {
        if (selectionManagerGO != null) Object.Destroy(selectionManagerGO);
    }

    [UnityTest]
    public IEnumerator CharacterSelectionManager_EmptyList_DisablesUI()
    {
        LogAssert.ignoreFailingMessages = true; // Ignorar cualquier error que imprima el script por diseño
        
        selectionManager.availableCharacters = new List<CharacterData>();
        
        // Activar el GameObject disparará el Start() inmediatamente
        selectionManagerGO.SetActive(true);
        
        yield return null; // Esperar un frame por seguridad

        Assert.IsFalse(selectionManager.leftArrow.gameObject.activeSelf, "La flecha izquierda debe desactivarse si la lista está vacía.");
        Assert.IsFalse(selectionManager.rightArrow.gameObject.activeSelf, "La flecha derecha debe desactivarse si la lista está vacía.");
        Assert.IsFalse(selectionManager.selectButton.gameObject.activeSelf, "El boton seleccionar debe desactivarse si la lista está vacía.");
    }
}
