using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class JumpWhitStrikeTests
{
    private GameObject playerGO;
    private JumpWhitStrike jumpStrike;
    private Rigidbody2D rb;

    [SetUp]
    public void SetUp()
    {
        playerGO = new GameObject("TestPlayer");
        rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Evitar caida durante el test

        jumpStrike = playerGO.AddComponent<JumpWhitStrike>();
        jumpStrike.rb = rb;
        jumpStrike.impulsoHorizontal = 5f;
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGO != null) Object.Destroy(playerGO);
    }

    [UnityTest]
    public IEnumerator JumpWhitStrike_GolpeConSalto_AddsForceAndSetsUsed()
    {
        MethodInfo golpeConSaltoMethod = typeof(JumpWhitStrike).GetMethod("GolpeConSalto", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo ataqueUsadoField = typeof(JumpWhitStrike).GetField("ataqueUsado", BindingFlags.NonPublic | BindingFlags.Instance);

        // Asegurarnos que comienza sin velocidad y ataque no usado
        Assert.AreEqual(0f, rb.linearVelocity.x);
        Assert.IsFalse((bool)ataqueUsadoField.GetValue(jumpStrike));

        // Ejecutar corrutina
        IEnumerator coroutine = (IEnumerator)golpeConSaltoMethod.Invoke(jumpStrike, null);
        jumpStrike.StartCoroutine(coroutine);

        // Esperar ciclo de fisicas
        yield return new WaitForFixedUpdate();

        // Debe haber añadido fuerza horizontal
        Assert.Greater(rb.linearVelocity.x, 0f, "El Rigidbody deberia haber ganado velocidad horizontal por el impulso.");
        
        // El ataque debe registrarse como usado
        Assert.IsTrue((bool)ataqueUsadoField.GetValue(jumpStrike), "ataqueUsado deberia ser true despues del salto.");
    }
}
