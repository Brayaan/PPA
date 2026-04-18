using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para PlayerMovement.
///
/// Pruebas omitidas por limitaciones de sandbox:
///   - Movimiento con A/D: depende de Input.GetKey no simulable.
///   - Salto con Space: depende de Input.GetKeyDown + groundCheck real.
///   - Detección de paredes: requiere colliders de escena y capas configuradas.
///   - Agachado: el resize del collider está gateado por Input.GetKey(crouchKey),
///     no simulable en PlayMode sin input virtual.
///
/// Pruebas incluidas que SÍ son deterministas sin input real:
///   1. No se mueve durante knockback  →  ApplyKnockback() es público.
///   2. Knockback izquierda→derecha    →  ApplyKnockback() con atacante a la izq.
///   3. Knockback derecha→izquierda    →  ApplyKnockback() con atacante a la der.
///
/// LogAssert:
///   - PlayerMovement.Start() dispara LogError("boxCollider no está asignado...")
///     si boxCollider es null → en las pruebas siempre se asigna; sin LogError.
///   - Las tres pruebas usan CreatePlayer() sin wallCheck/groundCheck/ceilingCheck/
///     animator. En el primer frame Update() falla el guard y dispara
///     LogError("Faltan referencias requeridas en el Inspector de TestPlayer").
///     Se declara con LogAssert.Expect antes de cada yield return null inicial.
///     Tras ApplyKnockback() el script queda disabled → Update() no vuelve a correr.
/// </summary>
public class PlayerMovementTests
{
    private GameObject _playerGO;

    // FieldInfo cacheado para leer isKnockedBack (campo privado)
    private FieldInfo _isKnockedBackField;

    // -------------------------------------------------------------------------
    // Helper base: crea un player con Rigidbody2D + BoxCollider2D + PlayerMovement.
    // boxCollider se asigna siempre para que Start() no dispare LogError.
    // gravityScale = 0 para resultados deterministas.
    // -------------------------------------------------------------------------
    private PlayerMovement CreatePlayer(Vector3 position = default)
    {
        _playerGO = new GameObject("TestPlayer");
        _playerGO.transform.position = position;

        Rigidbody2D rb = _playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        BoxCollider2D box = _playerGO.AddComponent<BoxCollider2D>();

        PlayerMovement movement = _playerGO.AddComponent<PlayerMovement>();
        movement.boxCollider = box;
        movement.knockbackDuration = 0.1f; // duración corta para pruebas rápidas

        _isKnockedBackField = typeof(PlayerMovement).GetField(
            "isKnockedBack", BindingFlags.NonPublic | BindingFlags.Instance);

        return movement;
    }


    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.Destroy(_playerGO);
    }

    // =========================================================================
    // 1. El player no se mueve durante el knockback
    //    ApplyKnockback() pone isKnockedBack = true y deshabilita el script.
    //    Con el script disabled, FixedUpdate() no se ejecuta, por lo que
    //    rb.linearVelocity.x queda solo con el impulso del knockback y no es
    //    sobreescrito por la lógica de movimiento (move * speed).
    // =========================================================================
    [UnityTest]
    public IEnumerator ApplyKnockback_DisablesMovementDuringKnockback()
    {
        PlayerMovement movement = CreatePlayer(Vector3.zero);

        // En este frame Start() corre correctamente (boxCollider asignado).
        // Update() también corre: wallCheck/groundCheck/ceilingCheck/animator son null
        // → guard dispara LogError("Faltan referencias requeridas en el Inspector de TestPlayer").
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");
        yield return null;

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();

        // El atacante viene desde la izquierda
        Vector2 attackerPos = new Vector2(-2f, 0f);
        movement.ApplyKnockback(attackerPos);

        // El script debe deshabilitarse inmediatamente tras ApplyKnockback()
        Assert.IsFalse(movement.enabled,
            "PlayerMovement debe deshabilitarse durante el knockback para bloquear Update/FixedUpdate.");

        // isKnockedBack debe ser true
        bool isKnockedBack = (bool)_isKnockedBackField.GetValue(movement);
        Assert.IsTrue(isKnockedBack,
            "isKnockedBack debe ser true inmediatamente tras ApplyKnockback().");

        // Esperar un FixedUpdate: con el script disabled, FixedUpdate no corre
        // y rb.linearVelocity NO es sobreescrito por (move * speed).
        // Update() tampoco corre (script disabled) → sin LogError adicional.
        yield return new WaitForFixedUpdate();

        // La velocidad X debe ser positiva (impulso alejándose del atacante izquierdo)
        // y no cero (FixedUpdate no la anuló con move * speed = 0)
        Assert.Greater(rb.linearVelocity.x, 0f,
            "La velocidad X debe conservar el impulso del knockback; FixedUpdate no debe sobreescribirla.");
    }

    // =========================================================================
    // 2. Golpe de izquierda a derecha empuja correctamente
    //    Atacante a la izquierda del player → dirección = derecha → velocity.x > 0
    // =========================================================================
    [UnityTest]
    public IEnumerator ApplyKnockback_AttackerOnLeft_PushesPlayerRight()
    {
        PlayerMovement movement = CreatePlayer(Vector3.zero);

        // Update() en este frame: wallCheck/groundCheck/ceilingCheck/animator null
        // → guard dispara LogError.
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");
        yield return null;

        // Atacante a la izquierda
        Vector2 attackerPos = new Vector2(-3f, 0f);
        movement.ApplyKnockback(attackerPos);
        // Script disabled tras ApplyKnockback → Update() no corre en frames siguientes.

        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();
        Assert.Greater(rb.linearVelocity.x, 0f,
            "Con el atacante a la izquierda, el knockback debe empujar al player hacia la derecha (velocity.x > 0).");
    }

    // =========================================================================
    // 4. Golpe de derecha a izquierda empuja correctamente
    //    Atacante a la derecha del player → dirección = izquierda → velocity.x < 0
    // =========================================================================
    [UnityTest]
    public IEnumerator ApplyKnockback_AttackerOnRight_PushesPlayerLeft()
    {
        PlayerMovement movement = CreatePlayer(Vector3.zero);

        // Update() en este frame: wallCheck/groundCheck/ceilingCheck/animator null
        // → guard dispara LogError.
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");
        yield return null;

        // Atacante a la derecha
        Vector2 attackerPos = new Vector2(3f, 0f);
        movement.ApplyKnockback(attackerPos);
        // Script disabled tras ApplyKnockback → Update() no corre en frames siguientes.

        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();
        Assert.Less(rb.linearVelocity.x, 0f,
            "Con el atacante a la derecha, el knockback debe empujar al player hacia la izquierda (velocity.x < 0).");
    }
}
