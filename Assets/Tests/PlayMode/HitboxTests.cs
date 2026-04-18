using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para Hitbox.
///
/// OnTriggerStay2D no se puede llamar directamente; se invoca via reflexión
/// pasando el Collider2D del objetivo. Esto hace las pruebas deterministas e
/// independientes del timing del motor de físicas.
///
/// Condiciones requeridas para que OnTriggerStay2D procese un golpe:
///   1. attack != null AND attack.IsAttacking() == true
///      → PlayerAttack en la raíz del atacante con isAttacking activado via reflexión.
///   2. other.CompareTag("Player") o CompareTag("Enemy")
///      → target con tag "Player" (debe existir en el proyecto Unity).
///   3. other.gameObject != transform.root.gameObject
///      → target en un GameObject diferente al del hitbox.
///   4. HealthSystem en el target para que se aplique daño.
///
/// LogAssert:
///   - HealthSystem.Start() → UpdateHealthUI() → LogError("healthImage no está
///     asignada en el Inspector") en el primer yield return null de cada prueba.
///   - HealthSystem.TakeDamage() → UpdateHealthUI() → LogError ídem, una vez
///     por cada golpe que conecte.
///   - Los Debug.Log informativos ("Golpeaste a: ...", "X bloqueó el ataque")
///     son nivel Log, no Error; no requieren LogAssert.Expect.
/// </summary>
public class HitboxTests
{
    // GameObjects del atacante y del objetivo; destruidos en TearDown
    private GameObject _attackerGO;
    private GameObject _targetGO;

    // Reflexión para invocar OnTriggerStay2D y escribir campos privados
    private MethodInfo  _onTriggerStay2D;
    private FieldInfo   _isAttackingField;  // PlayerAttack.isAttacking
    private FieldInfo   _isBlockingField;   // PlayerDefense.isBlocking

    // -------------------------------------------------------------------------
    // Helper: crea el GameObject del atacante con Rigidbody2D, Collider2D,
    // PlayerAttack y Hitbox. El hitbox es un hijo con isTrigger=true.
    // -------------------------------------------------------------------------
    private Hitbox CreateAttacker()
    {
        _attackerGO = new GameObject("Attacker");

        Rigidbody2D rb = _attackerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        // PlayerAttack en la raíz — Hitbox.Start() lo busca con GetComponent en root
        PlayerAttack playerAttack = _attackerGO.AddComponent<PlayerAttack>();

        // isAttacking es privado; lo activamos via reflexión en cada prueba
        _isAttackingField = typeof(PlayerAttack).GetField(
            "isAttacking", BindingFlags.NonPublic | BindingFlags.Instance);

        // Hitbox hijo con trigger
        GameObject hitboxGO = new GameObject("HitboxChild");
        hitboxGO.transform.SetParent(_attackerGO.transform);

        BoxCollider2D col = hitboxGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Hitbox hitbox = hitboxGO.AddComponent<Hitbox>();
        hitbox.damage = 20;
        hitbox.attackName = "Puñetazo";
        hitbox.hitCooldown = 0.3f;

        // Cachear reflexión para invocar el método privado
        _onTriggerStay2D = typeof(Hitbox).GetMethod(
            "OnTriggerStay2D", BindingFlags.NonPublic | BindingFlags.Instance);

        return hitbox;
    }

    // -------------------------------------------------------------------------
    // Helper: crea el GameObject del objetivo con HealthSystem y el tag "Player".
    // PlayerMovement se omite para evitar LogError de referencias faltantes.
    // -------------------------------------------------------------------------
    private HealthSystem CreateTarget(bool withDefense = false)
    {
        _targetGO = new GameObject("Target");
        _targetGO.tag = "Player"; // tag que Hitbox.OnTriggerStay2D comprueba

        Rigidbody2D rb = _targetGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        BoxCollider2D col = _targetGO.AddComponent<BoxCollider2D>();

        HealthSystem health = _targetGO.AddComponent<HealthSystem>();
        // healthImage null → UpdateHealthUI() disparará LogError gestionado con LogAssert
        health.knockbackDuration = 0.05f;

        if (withDefense)
        {
            PlayerDefense defense = _targetGO.AddComponent<PlayerDefense>();
            _isBlockingField = typeof(PlayerDefense).GetField(
                "isBlocking", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        return health;
    }

    [TearDown]
    public void TearDown()
    {
        if (_attackerGO != null) Object.Destroy(_attackerGO);
        if (_targetGO   != null) Object.Destroy(_targetGO);
    }

    // =========================================================================
    // 1. Un golpe no aplica daño múltiples veces dentro del hitCooldown
    //    Dos llamadas consecutivas a OnTriggerStay2D con Time.time sin avanzar
    //    deben conectar solo la primera; la segunda queda bloqueada por el
    //    cooldown (Time.time - lastHitTime < hitCooldown).
    // =========================================================================
    [UnityTest]
    public IEnumerator Hitbox_DoesNotDamageMultipleTimesWithinCooldown()
    {
        Hitbox hitbox = CreateAttacker();

        // HealthSystem.Start() → UpdateHealthUI() → LogError("healthImage...")
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget();
        yield return null; // Start() de todos los componentes

        // Activar estado de ataque en PlayerAttack via reflexión
        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();
        int initialHealth = health.currentHealth;

        // Primer golpe — conecta y actualiza lastHitTime
        // TakeDamage() → UpdateHealthUI() → LogError
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        int healthAfterFirstHit = health.currentHealth;
        Assert.AreEqual(initialHealth - hitbox.damage, healthAfterFirstHit,
            "El primer golpe debe reducir la vida en 'damage'.");

        // Segundo golpe inmediato — dentro del cooldown; NO debe aplicar daño
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        Assert.AreEqual(healthAfterFirstHit, health.currentHealth,
            "El segundo golpe dentro del cooldown NO debe reducir la vida.");
    }

    // =========================================================================
    // 2. El daño se aplica correctamente cuando el hitbox toca al oponente
    //    Un único golpe fuera de cooldown reduce currentHealth exactamente en 'damage'.
    // =========================================================================
    [UnityTest]
    public IEnumerator Hitbox_AppliesDamageWhenHittingTarget()
    {
        Hitbox hitbox = CreateAttacker();

        // HealthSystem.Start() → UpdateHealthUI() → LogError("healthImage...")
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget();
        yield return null; // Start() ejecuta, currentHealth = maxHealth

        // Activar ataque
        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        int initialHealth = health.currentHealth;
        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();

        // TakeDamage() → UpdateHealthUI() → LogError
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        Assert.AreEqual(initialHealth - hitbox.damage, health.currentHealth,
            "currentHealth debe bajar exactamente en 'damage' tras un golpe válido.");
        Assert.Greater(initialHealth, health.currentHealth,
            "La vida del objetivo debe ser menor después de recibir el golpe.");
    }

    // =========================================================================
    // 3. Al bloquear, el golpe no aplica daño al objetivo
    //    Si el objetivo tiene PlayerDefense con isBlocking=true, OnTriggerStay2D
    //    retorna antes de llamar HealthSystem.TakeDamage().
    // =========================================================================
    [UnityTest]
    public IEnumerator Hitbox_DoesNotDamageTargetWhenBlocking()
    {
        Hitbox hitbox = CreateAttacker();

        // HealthSystem.Start() → UpdateHealthUI() → LogError("healthImage...")
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget(withDefense: true);
        yield return null; // Start() de ambos GOs

        // Activar bloqueo en el objetivo via reflexión
        PlayerDefense defense = _targetGO.GetComponent<PlayerDefense>();
        _isBlockingField.SetValue(defense, true);

        // Activar ataque en el atacante
        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        int initialHealth = health.currentHealth;
        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();

        // El bloqueo activo → OnTriggerStay2D retorna antes de TakeDamage();
        // NO se dispara LogError de healthImage (TakeDamage nunca se llama).
        // Sí se emite Debug.Log("Target bloqueó el ataque") — nivel Log, no Error.
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        Assert.AreEqual(initialHealth, health.currentHealth,
            "El bloqueo debe absorber el golpe: currentHealth no debe cambiar.");
    }

    // =========================================================================
    // 4. Al bloquear, el atacante recibe rebote
    //    Cuando el objetivo bloquea, Hitbox aplica AddForce al Rigidbody2D del
    //    atacante en dirección opuesta al bloqueador → velocity.x != 0.
    //    El atacante está a la izquierda del objetivo → rebote hacia la izquierda.
    // =========================================================================
    [UnityTest]
    public IEnumerator Hitbox_PushesAttackerBackOnBlock()
    {
        Hitbox hitbox = CreateAttacker();

        // Posicionar: atacante a la izquierda, objetivo a la derecha
        _attackerGO.transform.position = new Vector3(-1f, 0f, 0f);

        // HealthSystem.Start() → UpdateHealthUI() → LogError("healthImage...")
        LogAssert.Expect(LogType.Error, "healthImage no está asignada en el Inspector");
        HealthSystem health = CreateTarget(withDefense: true);
        _targetGO.transform.position = new Vector3(1f, 0f, 0f);

        yield return null; // Start() de ambos GOs

        // Activar bloqueo en el objetivo
        PlayerDefense defense = _targetGO.GetComponent<PlayerDefense>();
        _isBlockingField.SetValue(defense, true);

        // Activar ataque en el atacante
        PlayerAttack playerAttack = _attackerGO.GetComponent<PlayerAttack>();
        _isAttackingField.SetValue(playerAttack, true);

        Rigidbody2D attackerRb = _attackerGO.GetComponent<Rigidbody2D>();
        attackerRb.linearVelocity = Vector2.zero; // estado limpio antes del rebote

        Collider2D targetCol = _targetGO.GetComponent<Collider2D>();
        _onTriggerStay2D.Invoke(hitbox, new object[] { targetCol });

        // Esperar un FixedUpdate para que AddForce se procese
        yield return new WaitForFixedUpdate();

        // El atacante estaba a la izquierda del objetivo → rebote hacia la izquierda
        Assert.Less(attackerRb.linearVelocity.x, 0f,
            "El rebote al bloquear debe empujar al atacante en dirección contraria al objetivo (hacia la izquierda).");
    }
}
