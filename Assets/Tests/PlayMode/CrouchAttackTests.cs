using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CrouchAttackTests
{
    private GameObject playerGO;
    private CrouchAttack crouchAttack;
    private Animator animator;

    [SetUp]
    public void SetUp()
    {
        playerGO = new GameObject("TestPlayer");
        animator = playerGO.AddComponent<Animator>();
        crouchAttack = playerGO.AddComponent<CrouchAttack>();
        crouchAttack.animator = animator;
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGO != null) Object.Destroy(playerGO);
    }

    [UnityTest]
    public IEnumerator CrouchAttack_RealizarAtaque_ActivatesAttackAndResets()
    {
        MethodInfo realizarAtaqueMethod = typeof(CrouchAttack).GetMethod("RealizarAtaque", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo atacandoField = typeof(CrouchAttack).GetField("atacando", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(realizarAtaqueMethod, "El metodo RealizarAtaque no se encontró.");
        Assert.IsNotNull(atacandoField, "La variable atacando no se encontró.");

        // Verificar estado inicial
        bool atacandoInicial = (bool)atacandoField.GetValue(crouchAttack);
        Assert.IsFalse(atacandoInicial, "No deberia estar atacando al inicio.");

        // Ejecutar la corrutina manualmente
        IEnumerator coroutine = (IEnumerator)realizarAtaqueMethod.Invoke(crouchAttack, null);
        crouchAttack.StartCoroutine(coroutine);

        // Esperamos 1 frame para que inicie
        yield return null;

        // Debería estar atacando
        bool atacandoDurante = (bool)atacandoField.GetValue(crouchAttack);
        Assert.IsTrue(atacandoDurante, "Deberia estar atacando despues de invocar la corrutina.");

        // La duracion es de 0.5f en el script, esperamos un poco más
        yield return new WaitForSeconds(0.6f);

        // Debería dejar de atacar
        bool atacandoDespues = (bool)atacandoField.GetValue(crouchAttack);
        Assert.IsFalse(atacandoDespues, "Deberia dejar de atacar tras pasar el tiempo.");
    }
}
