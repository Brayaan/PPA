using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TEST SUITE: EnemyHealthSystem
/// 
/// PROPÓSITO:
/// Validar el sistema de vida de los enemigos, incluyendo:
/// - Aplicación de daño
/// - Límites de vida (no menor a 0)
/// - Knockback al recibir daño
/// - Hit-stun (inmunidad temporal)
/// - Muerte del enemigo cuando la vida llega a 0
/// 
/// USO:
/// Estas pruebas simulan enemigos en runtime y verifican su comportamiento
/// sin necesidad de escena Unity.
/// 
/// DEPENDENCIAS:
/// - Rigidbody2D (para knockback físico)
/// - Unity Physics2D (FixedUpdate)
/// - Destrucción de GameObjects (Destroy)
/// </summary>
public class EnemyHealthSystemTests
{
    // =========================
    // REFERENCIA DEL ENEMIGO
    // =========================
    private GameObject _enemyGO;

    // =========================
    // HELPER: CREACIÓN DE ENEMIGO
    // =========================
    /// <summary>
    /// Crea un enemigo simulado con:
    /// - Rigidbody2D sin gravedad (controlado)
    /// - EnemyHealthSystem con knockback configurado
    /// 
    /// USO:
    /// Permite generar enemigos consistentes para todas las pruebas.
    /// </summary>
    private EnemyHealthSystem CreateEnemy(Vector3 position = default)
    {
        _enemyGO = new GameObject("TestEnemy");
        _enemyGO.transform.position = position;

        Rigidbody2D rb = _enemyGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        EnemyHealthSystem health = _enemyGO.AddComponent<EnemyHealthSystem>();
        health.knockbackDuration = 0.1f;

        return health;
    }

    // =========================
    // TEARDOWN (LIMPIEZA)
    // =========================
    /// <summary>
    /// Se ejecuta después de cada test.
    /// Elimina el enemigo creado para evitar contaminación entre pruebas.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (_enemyGO != null)
            Object.Destroy(_enemyGO);
    }

    // =========================
    // TEST 1: DAÑO NORMAL
    // =========================
    /// <summary>
    /// Verifica que el enemigo pierde vida correctamente
    /// cuando recibe daño.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_ReducesHealthByDamageAmount()
    {
        EnemyHealthSystem health = CreateEnemy();
        yield return null;

        int initialHealth = health.currentHealth;
        int damage = 15;

        // Posición del atacante (a la izquierda del enemigo)
        Vector2 attackerPos = new Vector2(
            _enemyGO.transform.position.x - 1f,
            _enemyGO.transform.position.y
        );

        health.TakeDamage(damage, attackerPos);

        Assert.AreEqual(initialHealth - damage, health.currentHealth);
    }

    // =========================
    // TEST 2: VIDA MÍNIMA
    // =========================
    /// <summary>
    /// Verifica que la vida nunca baja de 0,
    /// incluso si el daño es mayor que la vida actual.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_HealthDoesNotGoBelowZero()
    {
        EnemyHealthSystem health = CreateEnemy();
        yield return null;

        Vector2 attackerPos = new Vector2(
            _enemyGO.transform.position.x - 1f,
            _enemyGO.transform.position.y
        );

        health.TakeDamage(health.maxHealth + 100, attackerPos);

        Assert.AreEqual(0, health.currentHealth);
    }

    // =========================
    // TEST 3: KNOCKBACK
    // =========================
    /// <summary>
    /// Verifica que el enemigo es empujado en dirección opuesta
    /// al atacante cuando recibe daño.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_KnockbackPushesAwayFromAttacker()
    {
        EnemyHealthSystem health = CreateEnemy(Vector3.zero);
        yield return null;

        Vector2 attackerPos = new Vector2(-2f, 0f);

        health.TakeDamage(10, attackerPos);

        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = _enemyGO.GetComponent<Rigidbody2D>();

        Assert.Greater(rb.linearVelocity.x, 0f);
    }

    // =========================
    // TEST 4: HIT STUN
    // =========================
    /// <summary>
    /// Verifica que durante el hit-stun el enemigo no recibe
    /// un segundo knockback inmediatamente.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_SecondHitDuringHitStun_DoesNotApplyKnockbackAgain()
    {
        EnemyHealthSystem health = CreateEnemy(Vector3.zero);
        yield return null;

        Rigidbody2D rb = _enemyGO.GetComponent<Rigidbody2D>();

        Vector2 attackerPos = new Vector2(-2f, 0f);

        health.TakeDamage(10, attackerPos);
        yield return new WaitForFixedUpdate();

        rb.linearVelocity = Vector2.zero;

        health.TakeDamage(10, attackerPos);
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(0f, rb.linearVelocity.x, 0.001f);
    }

    // =========================
    // TEST 5: MUERTE DEL ENEMIGO
    // =========================
    /// <summary>
    /// Verifica que el enemigo se destruye cuando su vida llega a 0.
    /// </summary>
    [UnityTest]
    public IEnumerator TakeDamage_EnemyDiesWhenHealthReachesZero()
    {
        EnemyHealthSystem health = CreateEnemy();
        yield return null;

        Vector2 attackerPos = new Vector2(
            _enemyGO.transform.position.x - 1f,
            _enemyGO.transform.position.y
        );

        health.TakeDamage(health.maxHealth, attackerPos);

        Assert.AreEqual(0, health.currentHealth);

        // Ignora logs de destrucción tardía de Unity
        LogAssert.ignoreFailingMessages = true;
        yield return new WaitForSeconds(0.7f);
        LogAssert.ignoreFailingMessages = false;

        Assert.IsTrue(_enemyGO == null);

        _enemyGO = null;
    }
}