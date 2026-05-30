using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TESTS: Hitbox
///
/// PROPÓSITO:
/// Validar el sistema de daño cuerpo a cuerpo:
/// - Aplicación de daño
/// - Control de cooldown
/// - Bloqueo del daño
/// - Knockback al atacante cuando el golpe es bloqueado
///
/// USO:
/// Ejecutar en Unity Test Runner (PlayMode).
/// Se utilizan objetos creados dinámicamente (sin escena).
/// Se usa reflexión para simular estados internos del sistema.
/// </summary>
public class HitboxTests
{
    // Objetos principales de prueba
    private GameObject _attackerGO;
    private GameObject _targetGO;

    // Acceso a métodos y estados privados del sistema
    private MethodInfo _onTriggerStay2D;
    private FieldInfo _isAttackingField;
    private FieldInfo _isBlockingField;

    /// <summary>
    /// Crea el atacante con:
    /// - Rigidbody2D sin gravedad
    /// - PlayerAttack (estado de ataque)
    /// - Hitbox hijo con collider trigger
    /// </summary>
    private Hitbox CreateAttacker()
    {
        _attackerGO = new GameObject("Attacker");

        Rigidbody2D rb = _attackerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        PlayerAttack playerAttack = _attackerGO.AddComponent<PlayerAttack>();

        _isAttackingField = typeof(PlayerAttack).GetField(
            "isAttacking", BindingFlags.NonPublic | BindingFlags.Instance);

        // Hitbox como hijo del atacante
        GameObject hitboxGO = new GameObject("HitboxChild");
        hitboxGO.transform.SetParent(_attackerGO.transform);

        BoxCollider2D col = hitboxGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Hitbox hitbox = hitboxGO.AddComponent<Hitbox>();
        hitbox.damage = 20;
        hitbox.attackName = "Puñetazo";
        hitbox.hitCooldown = 0.3f;

        // Método interno de detección de golpe
        _onTriggerStay2D = typeof(Hitbox).GetMethod(
            "OnTriggerStay2D", BindingFlags.NonPublic | BindingFlags.Instance);

        return hitbox;
    }

    /// <summary>
    /// Crea el objetivo (jugador o enemigo simulado) con HealthSystem.
    /// Puede incluir sistema de defensa opcional.
    /// </summary>
    private HealthSystem CreateTarget(bool withDefense = false)
    {
        _targetGO = new GameObject("Target");
        _targetGO.tag = "Player";

        Rigidbody2D rb = _targetGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        _targetGO.AddComponent<BoxCollider2D>();

        HealthSystem health = _targetGO.AddComponent<HealthSystem>();
        health.knockbackDuration = 0.05f;

        if (withDefense)
        {
            PlayerDefense defense = _targetGO.AddComponent<PlayerDefense>();
            _isBlockingField = typeof(PlayerDefense).GetField(
                "isBlocking", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        return health;
    }

    /// <summary>
    /// Limpieza automática después de cada test.
    /// Evita contaminación entre pruebas.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (_attackerGO != null) Object.Destroy(_attackerGO);
        if (_targetGO != null) Object.Destroy(_targetGO);
    }

    /// <summary>
    /// CASO 1:
    /// El hitbox no debe aplicar daño múltiples veces dentro del cooldown.
    /// </summary>
    [UnityTest]
    public IEnumerator Hitbox_DoesNotDamageMultipleTimesWithinCooldown()
    {
        Hitbox hitbox = CreateAttacker();

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget();
        yield return null;

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();
        int initialHealth = health.currentHealth;

        // Primer impacto válido
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        int healthAfterFirstHit = health.currentHealth;

        // Segundo intento dentro del cooldown (no debe aplicar daño)
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        Assert.AreEqual(initialHealth - hitbox.damage, healthAfterFirstHit);
        Assert.AreEqual(healthAfterFirstHit, health.currentHealth);
    }

    /// <summary>
    /// CASO 2:
    /// El golpe debe aplicar daño correctamente cuando es válido.
    /// </summary>
    [UnityTest]
    public IEnumerator Hitbox_AppliesDamageWhenHittingTarget()
    {
        Hitbox hitbox = CreateAttacker();

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget();
        yield return null;

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        int initialHealth = health.currentHealth;
        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        Assert.AreEqual(initialHealth - hitbox.damage, health.currentHealth);
        Assert.Greater(initialHealth, health.currentHealth);
    }

    /// <summary>
    /// CASO 3:
    /// Si el objetivo está bloqueando, no debe recibir daño.
    /// </summary>
    [UnityTest]
    public IEnumerator Hitbox_DoesNotDamageTargetWhenBlocking()
    {
        Hitbox hitbox = CreateAttacker();

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget(true);
        yield return null;

        PlayerDefense defense = _targetGO.GetComponent<PlayerDefense>();
        _isBlockingField.SetValue(defense, true);

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        int initialHealth = health.currentHealth;
        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();

        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        Assert.AreEqual(initialHealth, health.currentHealth);
    }

    /// <summary>
    /// CASO 4:
    /// Si el golpe es bloqueado, el atacante recibe knockback.
    /// </summary>
    [UnityTest]
    public IEnumerator Hitbox_PushesAttackerBackOnBlock()
    {
        Hitbox hitbox = CreateAttacker();

        // Posicionamiento para determinar dirección del knockback
        _attackerGO.transform.position = new Vector3(-1f, 0f, 0f);

        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget(true);
        _targetGO.transform.position = new Vector3(1f, 0f, 0f);

        yield return null;

        PlayerDefense defense = _targetGO.GetComponent<PlayerDefense>();
        _isBlockingField.SetValue(defense, true);

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        Rigidbody2D attackerRb = _attackerGO.GetComponent<Rigidbody2D>();
        attackerRb.linearVelocity = Vector2.zero;

        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        yield return new WaitForFixedUpdate();

        Assert.Less(attackerRb.linearVelocity.x, 0f);
    }
}