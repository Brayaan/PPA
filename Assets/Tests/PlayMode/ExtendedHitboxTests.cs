using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para ExtendedHitbox.
///
/// ExtendedHitbox.OnTriggerEnter2D se invoca directamente via reflexión para
/// eliminar la dependencia del motor de físicas (timing no determinista).
///
/// Diferencias clave respecto a Hitbox:
///   - Usa OnTriggerEnter2D (una sola vez al entrar, sin cooldown propio).
///   - Solo daña objetivos con tag "Enemy" a través de EnemyHealthSystem.
///   - Requiere un Hitbox en el mismo GameObject (para leer damage).
///   - Requiere PlayerAttack en la raíz (para consultar IsAttacking()).
///   - No tiene guard de auto-golpe explícito: la separación es por tag "Enemy".
///
/// LogAssert: ningún método en el camino de código probado lanza Debug.LogError.
///   - EnemyHealthSystem.Start/TakeDamage: solo Debug.Log informativo.
///   - Hitbox.Start / PlayerAttack.Start: sin LogError.
/// </summary>
public class ExtendedHitboxTests
{
    private GameObject _attackerGO;
    private GameObject _targetGO;

    // Reflexión para invocar OnTriggerEnter2D (método privado de Unity)
    private MethodInfo _onTriggerEnter2D;

    // Reflexión para escribir PlayerAttack.isAttacking (campo privado)
    private FieldInfo _isAttackingField;

    // -------------------------------------------------------------------------
    // Helper: crea el GameObject del atacante con PlayerAttack, Hitbox y
    // ExtendedHitbox en el mismo hijo. gravityScale = 0 para determinismo.
    // -------------------------------------------------------------------------
    private ExtendedHitbox CreateAttacker()
    {
        _attackerGO = new GameObject("Attacker");

        Rigidbody2D rb = _attackerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        // PlayerAttack en la raíz → ExtendedHitbox.Start() lo busca con GetComponent en root
        _attackerGO.AddComponent<PlayerAttack>();

        _isAttackingField = typeof(PlayerAttack).GetField(
            "isAttacking", BindingFlags.NonPublic | BindingFlags.Instance);

        // Hijo con Hitbox + ExtendedHitbox + Collider trigger
        GameObject hitboxGO = new GameObject("ExtendedHitboxChild");
        hitboxGO.transform.SetParent(_attackerGO.transform);

        BoxCollider2D col = hitboxGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        // Hitbox requerido por ExtendedHitbox para leer 'damage'
        Hitbox hitbox = hitboxGO.AddComponent<Hitbox>();
        hitbox.damage = 15;
        hitbox.attackName = "Puñetazo";

        ExtendedHitbox extHitbox = hitboxGO.AddComponent<ExtendedHitbox>();

        // Cachear reflexión para OnTriggerEnter2D
        _onTriggerEnter2D = typeof(ExtendedHitbox).GetMethod(
            "OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance);

        return extHitbox;
    }

    // -------------------------------------------------------------------------
    // Helper: crea el objetivo enemigo con EnemyHealthSystem y tag "Enemy".
    // gravityScale = 0 para determinismo.
    // -------------------------------------------------------------------------
    private EnemyHealthSystem CreateEnemy()
    {
        _targetGO = new GameObject("Enemy");
        _targetGO.tag = "Enemy"; // tag que ExtendedHitbox.OnTriggerEnter2D comprueba

        Rigidbody2D rb = _targetGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        _targetGO.AddComponent<BoxCollider2D>();

        EnemyHealthSystem enemyHealth = _targetGO.AddComponent<EnemyHealthSystem>();
        enemyHealth.knockbackDuration = 0.05f;

        return enemyHealth;
    }

    [TearDown]
    public void TearDown()
    {
        if (_attackerGO != null) Object.Destroy(_attackerGO);
        if (_targetGO   != null) Object.Destroy(_targetGO);
    }

    // =========================================================================
    // 1. El hitbox extendido solo aplica daño si el ataque está activo
    //    Con IsAttacking() == false → OnTriggerEnter2D retorna sin dañar.
    //    Con IsAttacking() == true  → aplica daño correctamente.
    // =========================================================================
    [UnityTest]
    public IEnumerator ExtendedHitbox_OnlyDamagesWhenAttackIsActive()
    {
        ExtendedHitbox extHitbox = CreateAttacker();
        EnemyHealthSystem enemyHealth = CreateEnemy();

        // Esperar un frame para que Start() inicialice originalHitbox y playerAttack
        yield return null;

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        Collider2D enemyCol = _targetGO.GetComponent<Collider2D>();

        // --- Con IsAttacking() == false: no debe aplicar daño ---
        _isAttackingField.SetValue(playerAttack, false);
        int initialHealth = enemyHealth.currentHealth;

        _onTriggerEnter2D.Invoke(extHitbox, new object[] { enemyCol });

        Assert.AreEqual(initialHealth, enemyHealth.currentHealth,
            "ExtendedHitbox no debe aplicar daño cuando IsAttacking() == false.");

        // --- Con IsAttacking() == true: debe aplicar daño ---
        _isAttackingField.SetValue(playerAttack, true);

        _onTriggerEnter2D.Invoke(extHitbox, new object[] { enemyCol });

        Assert.AreEqual(initialHealth - extHitbox.GetComponent<Hitbox>().damage,
            enemyHealth.currentHealth,
            "ExtendedHitbox debe reducir currentHealth en 'damage' cuando IsAttacking() == true.");
    }

    // =========================================================================
    // 2. El hitbox extendido no daña al propio personaje
    //    ExtendedHitbox filtra por tag "Enemy". Si el propio atacante tiene un
    //    Collider con tag distinto de "Enemy" (o sin componente EnemyHealthSystem),
    //    OnTriggerEnter2D no aplica daño al root del atacante.
    //    Se verifica que invocar OnTriggerEnter2D con el Collider propio del
    //    atacante no modifica ningún EnemyHealthSystem (el atacante no tiene uno).
    // =========================================================================
    [UnityTest]
    public IEnumerator ExtendedHitbox_DoesNotDamageOwnCharacter()
    {
        ExtendedHitbox extHitbox = CreateAttacker();

        // El atacante tiene tag "Untagged" por defecto; tampoco tiene EnemyHealthSystem
        // Añadir un Collider al atacante raíz para simular que el trigger lo detecta
        BoxCollider2D selfCol = _attackerGO.AddComponent<BoxCollider2D>();

        // Crear un enemigo real para confirmar que el sistema funciona (referencia)
        EnemyHealthSystem enemyHealth = CreateEnemy();

        yield return null; // Start() de todos los componentes

        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        int enemyInitialHealth = enemyHealth.currentHealth;

        // Invocar con el Collider PROPIO del atacante raíz (tag != "Enemy")
        _onTriggerEnter2D.Invoke(extHitbox, new object[] { selfCol });

        // La vida del enemigo no debe cambiar (se invocó con collider del atacante, no del enemigo)
        Assert.AreEqual(enemyInitialHealth, enemyHealth.currentHealth,
            "Invocar OnTriggerEnter2D con el propio Collider del atacante no debe dañar al enemigo.");

        // El atacante no tiene EnemyHealthSystem → no hay auto-daño posible
        EnemyHealthSystem selfHealth = _attackerGO.GetComponent<EnemyHealthSystem>();
        Assert.IsNull(selfHealth,
            "El atacante no debe tener EnemyHealthSystem; el sistema no puede dañarse a sí mismo.");

        // Confirmar que el sistema SÍ funciona con el enemigo correcto
        Collider2D enemyCol = _targetGO.GetComponent<Collider2D>();
        _onTriggerEnter2D.Invoke(extHitbox, new object[] { enemyCol });

        Assert.Less(enemyHealth.currentHealth, enemyInitialHealth,
            "OnTriggerEnter2D con el Collider del enemigo real sí debe aplicar daño.");
    }
}
