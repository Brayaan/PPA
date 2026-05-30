using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// ============================================================
/// SISTEMA: PLAYER MOVEMENT TESTS
/// ============================================================
/// OBJETIVO:
/// Validar el comportamiento del movimiento del jugador durante:
/// - Knockback (retroceso al recibir daño)
/// - Dirección del empuje según posición del atacante
///
/// LIMITACIONES:
/// - No se simula input del jugador (A/D/Space).
/// - Se prueba únicamente lógica interna (ApplyKnockback).
/// - Se usa Reflection para acceder a estados privados.
/// ============================================================
/// </summary>
public class PlayerMovementTests
{
    // ============================================================
    // ESCENARIO DE PRUEBA
    // ============================================================
    private GameObject _playerGO;

    // Acceso a estado interno del knockback
    private FieldInfo _isKnockedBackField;

    // ============================================================
    // CREACIÓN DEL PLAYER SIMULADO
    // ============================================================
    // Construye un jugador falso con:
    // - Rigidbody2D (simulación física)
    // - BoxCollider2D (requerido por el script)
    // - PlayerMovement (sistema bajo prueba)
    // ============================================================
    private PlayerMovement CreatePlayer(Vector3 position = default)
    {
        _playerGO = new GameObject("TestPlayer");
        _playerGO.transform.position = position;

        Rigidbody2D rb = _playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        BoxCollider2D box = _playerGO.AddComponent<BoxCollider2D>();

        PlayerMovement movement = _playerGO.AddComponent<PlayerMovement>();
        movement.boxCollider = box;
        movement.knockbackDuration = 0.1f;

        // Acceso a variable privada de estado
        _isKnockedBackField = typeof(PlayerMovement)
            .GetField("isKnockedBack", BindingFlags.NonPublic | BindingFlags.Instance);

        return movement;
    }

    // ============================================================
    // LIMPIEZA POST-TEST
    // ============================================================
    // Elimina el GameObject para evitar acumulación en memoria.
    // ============================================================
    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.DestroyImmediate(_playerGO);
    }

    // ============================================================
    // TEST 1: BLOQUEO DE MOVIMIENTO DURANTE KNOCKBACK
    // ============================================================
    // OBJETIVO:
    // Verificar que el jugador no puede moverse mientras está en knockback.
    //
    // FLUJO:
    // 1. Crear jugador
    // 2. Aplicar knockback
    // 3. Validar que el script se desactiva
    // 4. Verificar estado interno y física
    // ============================================================
    [UnityTest]
    public IEnumerator ApplyKnockback_DisablesMovementDuringKnockback()
    {
        PlayerMovement movement = CreatePlayer(Vector3.zero);

        // Error esperado del sistema (validación de referencias)
        LogAssert.ignoreFailingMessages = true;
        yield return null;

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();

        // Simulación de ataque desde la izquierda
        Vector2 attackerPos = new Vector2(-2f, 0f);
        movement.ApplyKnockback(attackerPos);

        Assert.IsFalse(movement.enabled);

        bool isKnockedBack = (bool)_isKnockedBackField.GetValue(movement);
        Assert.IsTrue(isKnockedBack);

        yield return new WaitForFixedUpdate();

        Assert.Greater(rb.linearVelocity.x, 0f);
    }

    // ============================================================
    // TEST 2: KNOCKBACK DESDE LA IZQUIERDA
    // ============================================================
    // OBJETIVO:
    // El jugador debe ser empujado hacia la derecha
    // si el atacante está a la izquierda.
    // ============================================================
    [UnityTest]
    public IEnumerator ApplyKnockback_AttackerOnLeft_PushesPlayerRight()
    {
        PlayerMovement movement = CreatePlayer(Vector3.zero);

        LogAssert.ignoreFailingMessages = true;
        yield return null;

        Vector2 attackerPos = new Vector2(-3f, 0f);
        movement.ApplyKnockback(attackerPos);

        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();

        Assert.Greater(rb.linearVelocity.x, 0f);
    }

    // ============================================================
    // TEST 3: KNOCKBACK DESDE LA DERECHA
    // ============================================================
    // OBJETIVO:
    // El jugador debe ser empujado hacia la izquierda
    // si el atacante está a la derecha.
    // ============================================================
    [UnityTest]
    public IEnumerator ApplyKnockback_AttackerOnRight_PushesPlayerLeft()
    {
        PlayerMovement movement = CreatePlayer(Vector3.zero);

        LogAssert.ignoreFailingMessages = true;
        yield return null;

        Vector2 attackerPos = new Vector2(3f, 0f);
        movement.ApplyKnockback(attackerPos);

        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();

        Assert.Less(rb.linearVelocity.x, 0f);
    }
}