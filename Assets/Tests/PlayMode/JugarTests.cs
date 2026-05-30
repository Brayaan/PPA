using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class JugarTests
{
    private GameObject menuGO;
    private Jugar jugarMenu;

    private GameObject panelPrincipal;
    private GameObject panelOpcionesBase;

    [SetUp]
    public void SetUp()
    {
        menuGO = new GameObject("MenuManager");
        jugarMenu = menuGO.AddComponent<Jugar>();

        panelPrincipal = new GameObject("PanelPrincipal");
        panelOpcionesBase = new GameObject("PanelOpcionesBase");

        jugarMenu.panelPrincipal = panelPrincipal;
        jugarMenu.panelOpcionesBase = panelOpcionesBase;
    }

    [TearDown]
    public void TearDown()
    {
        if (menuGO != null) Object.Destroy(menuGO);
        if (panelPrincipal != null) Object.Destroy(panelPrincipal);
        if (panelOpcionesBase != null) Object.Destroy(panelOpcionesBase);
    }

    [UnityTest]
    public IEnumerator Jugar_AbrirYCerrarOpcionesBase_TogglesPanels()
    {
        yield return null; // Ejecutar Start()

        // Inicialmente el panel principal debe estar activo
        Assert.IsTrue(panelPrincipal.activeSelf);
        Assert.IsFalse(panelOpcionesBase.activeSelf);

        // Abrir opciones
        jugarMenu.AbrirOpcionesBase();

        Assert.IsFalse(panelPrincipal.activeSelf);
        Assert.IsTrue(panelOpcionesBase.activeSelf);

        // Cerrar opciones
        jugarMenu.CerrarOpcionesBase();

        Assert.IsTrue(panelPrincipal.activeSelf);
        Assert.IsFalse(panelOpcionesBase.activeSelf);
    }
}
