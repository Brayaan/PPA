using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TESTS: HealthSystem
///
/// PROPÓSITO:
/// Validar el sistema de vida del jugador:
/// - Reducción de vida
/// - Límite mínimo (0)
/// - Knockback físico
/// - Estado de hitstun
/// - Restauración del control del jugador
///
/// USO:
/// Ejecutar en Unity Test Runner (PlayMode).
/// Cada test crea su propio jugador aislado.
/// </summary>
public class HealthSystemTests
{
    // Objeto principal de prueba
    private GameObject _playerGO;

    /// <summary>
    /// Crea un jugador completo con:
    /// - Rigidbody2D sin gravedad (determinista)
    /// - Collider base
    /// - PlayerMovement (dependencia del sistema de daño)
    /// - HealthSystem (sistema bajo prueba)
    /// </summary>
    private HealthSystem CreatePlayer(Vector3 position = default)
    {
        _playerGO = new GameObject("TestPlayer");
        _playerGO.transform.position = position;

        Rigidbody2D rb = _playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        BoxCollider2D box = _playerGO.AddComponent<BoxCollider2D>();

        PlayerMovement movement = _playerGO.AddComponent<PlayerMovement>();
        movement.boxCollider = box;

        HealthSystem health = _playerGO.AddComponent<HealthSystem>();
        health.knockbackDuration = 0.1f;

        return health;
    }

    /// <summary>
    /// Limpieza después de cada test para evitar interferencia entre pruebas.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.DestroyImmediate(_playerGO);
    }

    /// <summary>
    /// CASO 1:
    /// El daño recibido debe reducir la vida correctamente.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_ReducesHealthByDamageAmount()
    {
        HealthSystem health = CreatePlayer();

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        int initialHealth = health.currentHealth;
        int damage = 25;

        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(damage, attackerPos);

        Assert.AreEqual(initialHealth - damage, health.currentHealth);
    }

    /// <summary>
    /// CASO 2:
    /// La vida nunca debe ser menor a 0 incluso con daño excesivo.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_HealthDoesNotGoBelowZero()
    {
        HealthSystem health = CreatePlayer();

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(health.maxHealth + 50, attackerPos);

        Assert.AreEqual(0, health.currentHealth);
    }

    /// <summary>
    /// CASO 3:
    /// El knockback debe empujar al jugador en dirección opuesta al atacante.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_KnockbackPushesAwayFromAttacker()
    {
        HealthSystem health = CreatePlayer(Vector3.zero);

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        Vector2 attackerPos = new Vector2(-2f, 0f);

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(10, attackerPos);

        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();

        Assert.Greater(rb.linearVelocity.x, 0f);
    }

    /// <summary>
    /// CASO 4:
    /// Durante el hitstun, el movimiento del jugador debe quedar deshabilitado.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_PlayerMovementDisabledDuringHitStun()
    {
        HealthSystem health = CreatePlayer();

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        PlayerMovement movement = _playerGO.GetComponent<PlayerMovement>();

        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(10, attackerPos);

        Assert.IsFalse(movement.enabled);
    }

    /// <summary>
    /// CASO 5:
    /// Después del knockback, el control del jugador debe restaurarse automáticamente.
    /// </summary>
    [UnityTest]
    public IEnumerator AfterKnockbackDuration_PlayerMovementIsReenabled()
    {
        HealthSystem health = CreatePlayer();
        health.knockbackDuration = 0.05f;

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        PlayerMovement movement = _playerGO.GetComponent<PlayerMovement>();

        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(10, attackerPos);

        LogAssert.ignoreFailingMessages = true;

        yield return new WaitForSeconds(health.knockbackDuration + 0.1f);

        LogAssert.ignoreFailingMessages = false;

        Assert.IsTrue(movement.enabled);
    }
}