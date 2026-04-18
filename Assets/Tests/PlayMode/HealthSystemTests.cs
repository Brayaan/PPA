using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para HealthSystem.
/// Cada prueba crea su propio GameObject con todos los componentes requeridos
/// y lo destruye al final via TearDown.
/// Se omite la prueba de UI de vida porque depende de sprites externos.
///
/// LogAssert.Expect se utiliza para suprimir los Debug.LogError esperados que
/// Unity Test Framework trataría como fallos de prueba:
///   - "healthImage no está asignada en el Inspector"
///     (disparado por UpdateHealthUI() en Start() y en cada TakeDamage())
///   - "Faltan referencias requeridas en el Inspector de TestPlayer"
///     (disparado por PlayerMovement.Update() mientras faltan referencias de
///     inspector como wallCheck, groundCheck, etc.)
/// </summary>
public class HealthSystemTests
{
    // GameObject compartido; se crea en cada prueba y se destruye en TearDown.
    private GameObject _playerGO;

    // -------------------------------------------------------------------------
    // Helper: crea un GameObject con los componentes mínimos que necesita
    // HealthSystem para ejecutarse sin errores de referencia nula.
    // PlayerMovement.Start() requiere un BoxCollider2D asignado; se lo pasamos
    // por campo público antes de que Unity llame a Start en el primer frame.
    // -------------------------------------------------------------------------
    private HealthSystem CreatePlayer(Vector3 position = default)
    {
        _playerGO = new GameObject("TestPlayer");
        _playerGO.transform.position = position;

        // Rigidbody2D — requerido por ApplyHit y PlayerMovement
        Rigidbody2D rb = _playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // desactivar gravedad para pruebas deterministas

        // BoxCollider2D — requerido por PlayerMovement.Start()
        BoxCollider2D box = _playerGO.AddComponent<BoxCollider2D>();

        // PlayerMovement — requerido por HealthSystem para bloquear el input
        PlayerMovement movement = _playerGO.AddComponent<PlayerMovement>();
        // Asignar referencias requeridas por inspector antes del primer Update
        movement.boxCollider = box;

        // HealthSystem — componente bajo prueba
        HealthSystem health = _playerGO.AddComponent<HealthSystem>();
        // healthImage se deja null a propósito; UpdateHealthUI lo maneja con guard
        health.knockbackDuration = 0.1f; // duración corta para pruebas rápidas

        return health;
    }

    // Destruir el GameObject después de cada prueba
    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.Destroy(_playerGO);
    }

    // =========================================================================
    // 1. Al recibir golpe, la vida baja correctamente
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_ReducesHealthByDamageAmount()
    {
        HealthSystem health = CreatePlayer();

        // Start() llamará UpdateHealthUI() → "healthImage" y
        // PlayerMovement.Update() → "Faltan referencias" en este frame.
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        // Esperar un frame para que Start() se ejecute y currentHealth se inicialice
        yield return null;

        int initialHealth = health.currentHealth; // igual a maxHealth tras Start()
        int damage = 25;
        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        // TakeDamage llama UpdateHealthUI() → "healthImage" de nuevo.
        // ApplyHit deshabilita PlayerMovement, por lo que no habrá más
        // "Faltan referencias" en frames posteriores.
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(damage, attackerPos);

        Assert.AreEqual(initialHealth - damage, health.currentHealth,
            "La vida debe bajar exactamente en la cantidad de daño recibido.");
    }

    // =========================================================================
    // 2. La vida no baja de 0
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_HealthDoesNotGoBelowZero()
    {
        HealthSystem health = CreatePlayer();

        // Start() → UpdateHealthUI() + PlayerMovement.Update() en este frame.
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        // TakeDamage → UpdateHealthUI() → "healthImage".
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        // Aplicar más daño del que tiene el jugador
        health.TakeDamage(health.maxHealth + 50, attackerPos);

        Assert.AreEqual(0, health.currentHealth,
            "La vida no debe ser menor que cero aunque el daño sea excesivo.");
    }

    // =========================================================================
    // 3. El knockback empuja en la dirección correcta
    //    La fuerza de impulso debe empujar al jugador ALEJÁNDOLO del atacante.
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_KnockbackPushesAwayFromAttacker()
    {
        // Colocar al jugador en el origen
        HealthSystem health = CreatePlayer(Vector3.zero);

        // Start() → UpdateHealthUI() + PlayerMovement.Update() en este frame.
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        // El atacante está a la izquierda → el impulso debe ir hacia la derecha
        Vector2 attackerPos = new Vector2(-2f, 0f);

        // TakeDamage → UpdateHealthUI() → "healthImage".
        // ApplyHit deshabilita PlayerMovement, así que WaitForFixedUpdate
        // no genera más "Faltan referencias".
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(10, attackerPos);

        // Esperar un FixedUpdate para que AddForce se aplique
        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _playerGO.GetComponent<Rigidbody2D>();

        // La componente X de la velocidad debe ser positiva (empuje hacia la derecha)
        Assert.Greater(rb.linearVelocity.x, 0f,
            "El knockback debe empujar al jugador en dirección opuesta al atacante.");
    }

    // =========================================================================
    // 4. El control se bloquea durante el knockback (hit-stun)
    //    HealthSystem.ApplyHit deshabilita PlayerMovement mientras isHit es true.
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_PlayerMovementDisabledDuringHitStun()
    {
        HealthSystem health = CreatePlayer();

        // Start() → UpdateHealthUI() + PlayerMovement.Update() en este frame.
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        PlayerMovement movement = _playerGO.GetComponent<PlayerMovement>();
        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        // TakeDamage → UpdateHealthUI() → "healthImage".
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(10, attackerPos);

        // Inmediatamente después del golpe, el input debe estar bloqueado
        Assert.IsFalse(movement.enabled,
            "PlayerMovement debe estar deshabilitado durante el hit-stun.");
    }

    // =========================================================================
    // 5. El control se recupera después del knockback
    //    Tras esperar knockbackDuration, EndHit() debe re-habilitar PlayerMovement.
    // =========================================================================
    [UnityTest]
    public IEnumerator AfterKnockbackDuration_PlayerMovementIsReenabled()
    {
        HealthSystem health = CreatePlayer();
        // Usar duración corta para no ralentizar la suite
        health.knockbackDuration = 0.05f;

        // Start() → UpdateHealthUI() + PlayerMovement.Update() en este frame.
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        LogAssert.Expect(LogType.Error, "Faltan referencias requeridas en el Inspector de TestPlayer");

        yield return null;

        PlayerMovement movement = _playerGO.GetComponent<PlayerMovement>();
        Vector2 attackerPos = new Vector2(_playerGO.transform.position.x - 1f,
                                          _playerGO.transform.position.y);

        // TakeDamage → UpdateHealthUI() → "healthImage".
        // ApplyHit deshabilita PlayerMovement (no más "Faltan referencias" durante hit-stun).
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");

        health.TakeDamage(10, attackerPos);

        // Durante el WaitForSeconds, EndHit() re-habilita PlayerMovement tras
        // knockbackDuration (0.05 s). Los frames restantes (~0.1 s) dispararán
        // "Faltan referencias" repetidamente porque wallCheck/groundCheck/etc.
        // siguen sin asignarse. Se ignoran esos logs para no romper la prueba.
        LogAssert.ignoreFailingMessages = true;

        // Esperar más tiempo que knockbackDuration para que EndHit() se ejecute
        yield return new WaitForSeconds(health.knockbackDuration + 0.1f);

        LogAssert.ignoreFailingMessages = false;

        Assert.IsTrue(movement.enabled,
            "PlayerMovement debe estar habilitado nuevamente tras terminar el hit-stun.");
    }
}
