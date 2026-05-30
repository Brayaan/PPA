using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TESTS: ExtendedHitbox
///
/// PROPÓSITO:
/// Validar el comportamiento del sistema de hitbox extendido durante ataques.
/// Se usa reflexión para simular estados internos sin depender del input real.
///
/// USO:
/// Estas pruebas se ejecutan en Unity Test Runner (PlayMode).
/// No requieren escena previa porque crean sus propios GameObjects.
/// </summary>
public class ExtendedHitboxTests
{
    // Referencias a objetos de prueba (atacante y objetivo)
    private GameObject _attackerGO;
    private GameObject _targetGO;

    // Reflexión para acceder a métodos y campos privados del sistema
    private MethodInfo _onTriggerEnter2D;
    private FieldInfo _isAttackingField;

    /// <summary>
    /// Crea el atacante con:
    /// - Rigidbody2D sin gravedad (determinismo)
    /// - PlayerAttack (estado de ataque)
    /// - ExtendedHitbox en un hijo con collider trigger
    /// </summary>
    private ExtendedHitbox CreateAttacker()
    {
        _attackerGO = new GameObject("Attacker");

        Rigidbody2D rb = _attackerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        _attackerGO.AddComponent<PlayerAttack>();

        // Acceso al estado interno de ataque (isAttacking)
        _isAttackingField = typeof(PlayerAttack).GetField(
            "isAttacking", BindingFlags.NonPublic | BindingFlags.Instance);

        // Hitbox como hijo del atacante (estructura típica de combate)
        GameObject hitboxGO = new GameObject("ExtendedHitboxChild");
        hitboxGO.transform.SetParent(_attackerGO.transform);

        BoxCollider2D col = hitboxGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        // Componente base de daño
        Hitbox hitbox = hitboxGO.AddComponent<Hitbox>();
        hitbox.damage = 15;
        hitbox.attackName = "Puñetazo";

        // Hitbox extendido (sistema bajo prueba)
        ExtendedHitbox extHitbox = hitboxGO.AddComponent<ExtendedHitbox>();

        // Acceso al evento interno de colisión
        _onTriggerEnter2D = typeof(ExtendedHitbox).GetMethod(
            "OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);

        return extHitbox;
    }

    /// <summary>
    /// Crea un enemigo estándar con:
    /// - Rigidbody2D sin gravedad
    /// - Collider 2D
    /// - EnemyHealthSystem
    /// </summary>
    private EnemyHealthSystem CreateEnemy()
    {
        _targetGO = new GameObject("Enemy");
        _targetGO.tag = "Enemy";

        Rigidbody2D rb = _targetGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        _targetGO.AddComponent<BoxCollider2D>();

        EnemyHealthSystem enemyHealth = _targetGO.AddComponent<EnemyHealthSystem>();
        enemyHealth.knockbackDuration = 0.05f;

        return enemyHealth;
    }

    /// <summary>
    /// Limpieza automática después de cada test.
    /// Garantiza que no haya contaminación entre pruebas.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (_attackerGO != null) Object.Destroy(_attackerGO);
        if (_targetGO != null) Object.Destroy(_targetGO);
    }

    /// <summary>
    /// CASO 1:
    /// El hitbox SOLO debe hacer daño cuando el ataque está activo.
    /// </summary>
    [UnityTest]
    public IEnumerator ExtendedHitbox_OnlyDamagesWhenAttackIsActive()
    {
        ExtendedHitbox extHitbox = CreateAttacker();
        EnemyHealthSystem enemyHealth = CreateEnemy();

        yield return null;

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        Collider2D enemyCol = _targetGO.GetComponent<Collider2D>();

        // Estado: ataque desactivado → no debe hacer daño
        _isAttackingField.SetValue(playerAttack, false);
        int initialHealth = enemyHealth.currentHealth;

        _onTriggerEnter2D.Invoke(extHitbox, new object[] { enemyCol });

        Assert.AreEqual(initialHealth, enemyHealth.currentHealth);

        // Estado: ataque activo → debe aplicar daño
        _isAttackingField.SetValue(playerAttack, true);

        _onTriggerEnter2D.Invoke(extHitbox, new object[] { enemyCol });

        Assert.AreEqual(
            initialHealth - extHitbox.GetComponent<Hitbox>().damage,
            enemyHealth.currentHealth
        );
    }

    /// <summary>
    /// CASO 2:
    /// El sistema no debe permitir auto-daño al propio atacante.
    /// </summary>
    [UnityTest]
    public IEnumerator ExtendedHitbox_DoesNotDamageOwnCharacter()
    {
        ExtendedHitbox extHitbox = CreateAttacker();

        // Collider del propio atacante (simulación de contacto consigo mismo)
        BoxCollider2D selfCol = _attackerGO.AddComponent<BoxCollider2D>();

        EnemyHealthSystem enemyHealth = CreateEnemy();

        yield return null;

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        int enemyInitialHealth = enemyHealth.currentHealth;

        // Intento de colisión con el propio atacante → no debe afectar enemigos
        _onTriggerEnter2D.Invoke(extHitbox, new object[] { selfCol });

        Assert.AreEqual(enemyInitialHealth, enemyHealth.currentHealth);

        // El atacante no debe tener sistema de vida de enemigo
        EnemyHealthSystem selfHealth = _attackerGO.GetComponent<EnemyHealthSystem>();
        Assert.IsNull(selfHealth);

        // Caso válido: golpe a enemigo real sí debe aplicar daño
        Collider2D enemyCol = _targetGO.GetComponent<Collider2D>();
        _onTriggerEnter2D.Invoke(extHitbox, new object[] { enemyCol });

        Assert.Less(enemyHealth.currentHealth, enemyInitialHealth);
    }
}