using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para EnemyHealthSystem.
/// Cada prueba crea su propio GameObject con todos los componentes necesarios
/// y lo destruye en TearDown (o verifica que Die() lo destruya a tiempo).
///
/// EnemyHealthSystem no accede a referencias de Inspector en Start(),
/// por lo que no se generan Debug.LogError esperados salvo en casos indicados.
/// Los Debug.Log informativos ("Enemigo vida: X", "Enemigo derrotado") son
/// de nivel Log, no Error, y no rompen las pruebas por sí solos.
/// </summary>
public class EnemyHealthSystemTests
{
    // GameObject del enemigo; se crea antes de cada prueba y se destruye en TearDown.
    private GameObject _enemyGO;

    // -------------------------------------------------------------------------
    // Helper: crea un GameObject con los componentes mínimos requeridos por
    // EnemyHealthSystem. Se desactiva la gravedad para resultados deterministas.
    // -------------------------------------------------------------------------
    private EnemyHealthSystem CreateEnemy(Vector3 position = default)
    {
        _enemyGO = new GameObject("TestEnemy");
        _enemyGO.transform.position = position;

        // Rigidbody2D — requerido por ApplyHit para aplicar la fuerza de knockback
        Rigidbody2D rb = _enemyGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // desactivar gravedad para resultados deterministas

        // EnemyHealthSystem — componente bajo prueba
        EnemyHealthSystem health = _enemyGO.AddComponent<EnemyHealthSystem>();
        health.knockbackDuration = 0.1f; // duración corta para mantener la suite rápida

        return health;
    }

    // Destruir el GameObject después de cada prueba.
    // Si Die() ya lo destruyó, la referencia puede ser null; Object.Destroy lo tolera.
    [TearDown]
    public void TearDown()
    {
        if (_enemyGO != null)
            Object.Destroy(_enemyGO);
    }

    // =========================================================================
    // 1. Al recibir daño la vida baja correctamente
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_ReducesHealthByDamageAmount()
    {
        EnemyHealthSystem health = CreateEnemy();

        // Esperar un frame para que Start() inicialice currentHealth
        yield return null;

        int initialHealth = health.currentHealth; // igual a maxHealth tras Start()
        int damage = 15;
        Vector2 attackerPos = new Vector2(_enemyGO.transform.position.x - 1f,
                                          _enemyGO.transform.position.y);

        health.TakeDamage(damage, attackerPos);

        Assert.AreEqual(initialHealth - damage, health.currentHealth,
            "La vida del enemigo debe bajar exactamente en la cantidad de daño recibido.");
    }

    // =========================================================================
    // 2. La vida no baja de 0
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_HealthDoesNotGoBelowZero()
    {
        EnemyHealthSystem health = CreateEnemy();
        yield return null;

        Vector2 attackerPos = new Vector2(_enemyGO.transform.position.x - 1f,
                                          _enemyGO.transform.position.y);

        // Aplicar más daño del que tiene el enemigo
        health.TakeDamage(health.maxHealth + 100, attackerPos);

        Assert.AreEqual(0, health.currentHealth,
            "La vida del enemigo no debe ser menor que cero aunque el daño sea excesivo.");
    }

    // =========================================================================
    // 3. El knockback empuja en la dirección correcta
    //    El impulso debe alejar al enemigo del atacante.
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_KnockbackPushesAwayFromAttacker()
    {
        // Colocar al enemigo en el origen
        EnemyHealthSystem health = CreateEnemy(Vector3.zero);
        yield return null;

        // El atacante está a la izquierda → el empuje debe ir hacia la derecha
        Vector2 attackerPos = new Vector2(-2f, 0f);
        health.TakeDamage(10, attackerPos);

        // Esperar un FixedUpdate para que AddForce se aplique al Rigidbody2D
        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _enemyGO.GetComponent<Rigidbody2D>();

        // La componente X de la velocidad debe ser positiva (hacia la derecha)
        Assert.Greater(rb.linearVelocity.x, 0f,
            "El knockback debe empujar al enemigo en dirección opuesta al atacante.");
    }

    // =========================================================================
    // 4. No recibe knockback doble durante el hit-stun activo
    //    El segundo golpe debe bajar la vida pero NO aplicar un segundo impulso
    //    (ApplyHit retorna temprano mientras isHit == true).
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_SecondHitDuringHitStun_DoesNotApplyKnockbackAgain()
    {
        EnemyHealthSystem health = CreateEnemy(Vector3.zero);
        yield return null;

        Rigidbody2D rb = _enemyGO.GetComponent<Rigidbody2D>();

        // Primer golpe: activa hit-stun y aplica impulso
        Vector2 attackerPos = new Vector2(-2f, 0f);
        health.TakeDamage(10, attackerPos);

        // Esperar un FixedUpdate para que el primer AddForce se procese
        yield return new WaitForFixedUpdate();

        float velocityAfterFirstHit = rb.linearVelocity.x;

        // Zerear velocidad manualmente para detectar si el segundo golpe añade fuerza
        rb.linearVelocity = Vector2.zero;

        // Segundo golpe inmediato — hit-stun sigue activo, ApplyHit debe ignorarse
        health.TakeDamage(10, attackerPos);

        yield return new WaitForFixedUpdate();

        // La velocidad debe seguir en cero: no se aplicó un segundo AddForce
        Assert.AreEqual(0f, rb.linearVelocity.x, 0.001f,
            "El segundo golpe durante el hit-stun no debe aplicar un impulso adicional.");
    }

    // =========================================================================
    // 5. El enemigo muere al llegar a 0 de vida
    //    Die() hace Destroy(gameObject, 0.5 s); verificamos tras ese delay.
    // =========================================================================
    [UnityTest]
    public IEnumerator TakeDamage_EnemyDiesWhenHealthReachesZero()
    {
        EnemyHealthSystem health = CreateEnemy();
        yield return null;

        Vector2 attackerPos = new Vector2(_enemyGO.transform.position.x - 1f,
                                          _enemyGO.transform.position.y);

        // Golpe letal: quita toda la vida de una vez
        health.TakeDamage(health.maxHealth, attackerPos);

        // Confirmar que la vida llegó a cero antes de que el objeto sea destruido
        Assert.AreEqual(0, health.currentHealth,
            "La vida debe ser exactamente 0 tras un golpe letal.");

        // Die() destruye el GameObject con un delay de 0.5 s.
        // Esperamos más que eso para verificar la destrucción.
        // Se ignoran los mensajes de objetos destruidos que Unity puede emitir.
        LogAssert.ignoreFailingMessages = true;
        yield return new WaitForSeconds(0.7f);
        LogAssert.ignoreFailingMessages = false;

        // Un GameObject destruido devuelve null en comparación con el operador ==
        // de Unity (que sobrecarga == para objetos destruidos).
        Assert.IsTrue(_enemyGO == null,
            "El GameObject del enemigo debe haber sido destruido tras llegar a 0 de vida.");

        // Limpiar la referencia para que TearDown no intente destruir un objeto ya destruido
        _enemyGO = null;
    }
}
