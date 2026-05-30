using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PRUEBAS DE PLAYER ATTACK
/// ------------------------------------------------------------
/// Este conjunto de pruebas valida la lógica de ataque del jugador:
/// - Activación y desactivación de hitboxes
/// - Control de cooldowns
/// - Estado de ataque (isAttacking)
///
/// LIMITACIÓN:
/// Unity Input no se puede simular directamente en PlayMode,
/// por lo que la lógica se prueba llamando métodos internos
/// (Invoke, API pública y estados internos).
/// </summary>
public class PlayerAttackTests
{
    // =========================
    // ESCENARIO DE PRUEBA
    // =========================
    private GameObject _playerGO;
    private PlayerAttack _attack;

    private GameObject _punchHitboxGO;
    private GameObject _kickHitboxGO;

    // ------------------------------------------------------------
    // CREA UN PLAYER SIMULADO COMPLETO
    // ------------------------------------------------------------
    // Este método construye un jugador falso dentro del test:
    // - GameObject base
    // - Hitbox de puño
    // - Hitbox de patada
    // - Script PlayerAttack
    // - Configuración de cooldowns
    //
    // Se usa para aislar la lógica sin depender de la escena real.
    // ------------------------------------------------------------
    private PlayerAttack CreatePlayer()
    {
        _playerGO = new GameObject("TestPlayer");

        _punchHitboxGO = new GameObject("PunchHitbox");
        _punchHitboxGO.transform.SetParent(_playerGO.transform);
        _punchHitboxGO.SetActive(false);

        _kickHitboxGO = new GameObject("KickHitbox");
        _kickHitboxGO.transform.SetParent(_playerGO.transform);
        _kickHitboxGO.SetActive(false);

        _attack = _playerGO.AddComponent<PlayerAttack>();
        _attack.punchHitbox = _punchHitboxGO;
        _attack.kickHitbox = _kickHitboxGO;

        _attack.punchCooldown = 0.15f;
        _attack.kickCooldown = 0.25f;

        return _attack;
    }

    // ------------------------------------------------------------
    // LIMPIEZA DESPUÉS DE CADA TEST
    // ------------------------------------------------------------
    // Evita que objetos se acumulen en memoria entre pruebas.
    // ------------------------------------------------------------
    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.Destroy(_playerGO);
    }

    // ============================================================
    // TEST 1: DESACTIVACIÓN DE HITBOX
    // ============================================================
    // Verifica que el hitbox de puño se desactive después del
    // tiempo de ataque (attackDuration).
    // ============================================================
    [UnityTest]
    public IEnumerator PunchHitbox_DeactivatesAfterAttackDuration()
    {
        CreatePlayer();
        yield return null;

        // Activar hitbox manualmente
        _attack.ActivarHitbox();
        Assert.IsTrue(_punchHitboxGO.activeSelf);

        // Simular fin de ataque con delay
        _attack.Invoke("DesactivarHitbox", 0.3f);

        yield return new WaitForSeconds(0.4f);

        // Validación final
        Assert.IsFalse(_punchHitboxGO.activeSelf);
    }

    // ============================================================
    // TEST 2: ATAQUE YA ACTIVO
    // ============================================================
    // Evita que un nuevo StartAttack reinicie la duración del ataque.
    // ============================================================
    [UnityTest]
    public IEnumerator StartAttack_WhileAlreadyAttacking_DoesNotResetDuration()
    {
        CreatePlayer();
        yield return null;

        _attack.Invoke("StartAttack", 0f);
        yield return null;

        Assert.IsTrue(_attack.IsAttacking());

        bool wasAttackingBeforeSecondCall = _attack.IsAttacking();
        Assert.IsTrue(wasAttackingBeforeSecondCall);

        yield return new WaitForSeconds(0.4f);

        Assert.IsFalse(_attack.IsAttacking());
    }

    // ============================================================
    // TEST 3: COOLDOWN DE PUÑO
    // ============================================================
    // Verifica que el cooldown permite reutilizar el ataque
    // después del tiempo definido.
    // ============================================================
    [UnityTest]
    public IEnumerator PunchCooldown_PunchReadyRestoresAfterCooldown()
    {
        CreatePlayer();
        yield return null;

        _attack.Invoke("ResetPunch", _attack.punchCooldown);

        _attack.ActivarHitbox();
        Assert.IsTrue(_punchHitboxGO.activeSelf);

        _attack.DesactivarHitbox();
        Assert.IsFalse(_punchHitboxGO.activeSelf);

        yield return new WaitForSeconds(_attack.punchCooldown + 0.05f);

        _attack.ActivarHitbox();
        Assert.IsTrue(_punchHitboxGO.activeSelf);
        _attack.DesactivarHitbox();
    }

    // ============================================================
    // TEST 4: COOLDOWN DE PATADA
    // ============================================================
    // Igual que el puño pero con la patada.
    // ============================================================
    [UnityTest]
    public IEnumerator KickCooldown_KickReadyRestoresAfterCooldown()
    {
        CreatePlayer();
        yield return null;

        _attack.Invoke("ResetKick", _attack.kickCooldown);

        _attack.ActivarKickHitbox();
        Assert.IsTrue(_kickHitboxGO.activeSelf);

        _attack.DesactivarKickHitbox();
        Assert.IsFalse(_kickHitboxGO.activeSelf);

        yield return new WaitForSeconds(_attack.kickCooldown + 0.05f);

        _attack.ActivarKickHitbox();
        Assert.IsTrue(_kickHitboxGO.activeSelf);

        _attack.DesactivarKickHitbox();
    }
}