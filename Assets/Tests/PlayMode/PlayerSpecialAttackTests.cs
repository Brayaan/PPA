using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

public class PlayerSpecialAttackTests
{
    private GameObject playerGO;
    private PlayerSpecialAttack specialAttack;
    private PlayerMovement playerMovement;
    private EnergySystem energySystem;
    private GameObject testHitbox;

    private MethodInfo executeSpecialAttackMethod;

    [SetUp]
    public void SetUp()
    {

        playerGO = new GameObject("TestPlayer");
        
        // Rigidbody2D es necesario para FixedUpdate
        Rigidbody2D rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        playerMovement = playerGO.AddComponent<PlayerMovement>();
        playerMovement.boxCollider = playerGO.AddComponent<BoxCollider2D>();
        playerMovement.wallCheck = new GameObject("WallCheck").transform;
        playerMovement.groundCheck = new GameObject("GroundCheck").transform;
        playerMovement.ceilingCheck = new GameObject("CeilingCheck").transform;
        
        Animator anim = playerGO.AddComponent<Animator>();
        anim.enabled = false; // Desactivarlo para evitar advertencias de "Animator is not playing an AnimatorController"
        playerMovement.animator = anim;
        
        energySystem = playerGO.AddComponent<EnergySystem>();
        energySystem.energyImage = new GameObject("DummyImage").AddComponent<Image>();
        
        specialAttack = playerGO.AddComponent<PlayerSpecialAttack>();
        specialAttack.playerMovement = playerMovement;
        specialAttack.energySystem = energySystem;

        testHitbox = new GameObject("TestHitbox");
        testHitbox.SetActive(false);

        SpecialAttackData testAttack = new SpecialAttackData
        {
            attackName = "TestSpecial",
            energyCost = 10,
            damage = 50,
            duration = 0.5f,
            hitDelay = 0.1f,
            hitbox = testHitbox,
            isHealing = false
        };

        specialAttack.specialAttacks[0] = testAttack;
        for (int i = 1; i < specialAttack.specialAttacks.Length; i++)
        {
            specialAttack.specialAttacks[i] = new SpecialAttackData();
        }

        executeSpecialAttackMethod = typeof(PlayerSpecialAttack).GetMethod("ExecuteSpecialAttack", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGO != null) Object.Destroy(playerGO);
        if (testHitbox != null) Object.Destroy(testHitbox);
    }

    [UnityTest]
    public IEnumerator ExecuteSpecialAttack_DisablesMovementAndActivatesState()
    {
        LogAssert.ignoreFailingMessages = true;

        // Asegurarnos que está activo el movimiento
        playerMovement.enabled = true;
        Assert.IsFalse(specialAttack.IsUsingSpecial());

        // Ejecutar ataque
        executeSpecialAttackMethod.Invoke(specialAttack, new object[] { specialAttack.specialAttacks[0] });

        // Verificamos estado inmediato
        Assert.IsTrue(specialAttack.IsUsingSpecial(), "El estado isUsingSpecial debe ser verdadero.");
        Assert.IsFalse(playerMovement.enabled, "El movimiento debe desactivarse durante el especial.");

        yield return null;
    }

    [UnityTest]
    public IEnumerator ExecuteSpecialAttack_ActivatesHitboxAfterDelay()
    {
        LogAssert.ignoreFailingMessages = true;

        Assert.IsFalse(testHitbox.activeSelf);

        // Ejecutar ataque
        executeSpecialAttackMethod.Invoke(specialAttack, new object[] { specialAttack.specialAttacks[0] });

        // Antes del hit delay, el hitbox sigue apagado
        Assert.IsFalse(testHitbox.activeSelf);

        // Esperar el hit delay (0.1f)
        yield return new WaitForSeconds(0.15f);

        // Debe haberse activado
        Assert.IsTrue(testHitbox.activeSelf, "El hitbox debe activarse después del delay.");
    }
}
