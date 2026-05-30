using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WinScreenAnimationTests
{
    private GameObject testGO;
    private WinScreenAnimation anim;
    private CanvasGroup canvasGroup;

    [SetUp]
    public void SetUp()
    {
        testGO = new GameObject("TestWinScreen");
        
        // El script requiere un CanvasGroup
        canvasGroup = testGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        anim = testGO.AddComponent<WinScreenAnimation>();
    }

    [TearDown]
    public void TearDown()
    {
        if (testGO != null) Object.Destroy(testGO);
    }

    [UnityTest]
    public IEnumerator WinScreenAnimation_OnEnable_AnimatesAlphaTo1()
    {
        // Forzamos activar el objeto para disparar OnEnable
        testGO.SetActive(false);
        testGO.SetActive(true);

        // Deberia empezar en alpha 0 segun el OnEnable
        Assert.Less(canvasGroup.alpha, 0.1f, "Alpha inicial deberia ser ~0");

        // Esperamos mas que la duracion (0.6f + margen)
        yield return new WaitForSeconds(0.7f);

        // Deberia terminar en alpha 1
        Assert.AreEqual(1f, canvasGroup.alpha, "Alpha deberia llegar a 1 al finalizar la corrutina.");
    }
}
