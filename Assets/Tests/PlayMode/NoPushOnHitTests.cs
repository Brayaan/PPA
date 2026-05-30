using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NoPushOnHitTests
{
    private GameObject testGO;
    private NoPushOnHit noPush;
    private Rigidbody2D rb;

    [SetUp]
    public void SetUp()
    {
        testGO = new GameObject("TestObject");
        rb = testGO.AddComponent<Rigidbody2D>();
        rb.mass = 5f; // Masa original simulada
        noPush = testGO.AddComponent<NoPushOnHit>();
    }

    [TearDown]
    public void TearDown()
    {
        if (testGO != null) Object.Destroy(testGO);
    }

    [UnityTest]
    public IEnumerator NoPushOnHit_ChangesMassAndRestores()
    {
        yield return null; // Esperar Start() para que guarde la masa original (5f)

        // Inicio del hit
        noPush.OnHitStart();
        
        Assert.AreEqual(1000f, rb.mass, "La masa debió cambiar a 1000f durante el HitStart.");

        // Fin del hit
        noPush.OnHitEnd();

        Assert.AreEqual(5f, rb.mass, "La masa debió restaurarse a la original (5f) tras el HitEnd.");
    }
}
