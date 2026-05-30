using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// ============================================================
/// SISTEMA: PLAYER DEFENSE TESTS
/// ============================================================
/// OBJETIVO:
/// Validar la lógica del sistema de defensa del jugador:
/// - Activación/desactivación del bloqueo
/// - Ganancia de energía al bloquear
/// - Consistencia del estado visual (hitbox + estado interno)
///
/// LIMITACIONES:
/// - Input.GetKey no se puede simular en PlayMode.
/// - Se usa Reflection para forzar estados internos.
/// - Update() se invoca manualmente para simular el ciclo real.
/// ============================================================
/// </summary>
public class PlayerDefenseTests
{
    // ============================================================
    // ESCENARIO DE PRUEBA
    // ============================================================
    private GameObject _playerGO;

    // Acceso a variables privadas del sistema
    private FieldInfo _isBlockingField;
    private MethodInfo _updateMethod;

    // ============================================================
    // SETUP DEL ENTORNO DE PRUEBA
    // ============================================================
    // Crea un jugador simulado con:
    // - EnergySystem (dependencia del sistema de defensa)
    // - Hitbox de bloqueo
    // - PlayerDefense configurado manualmente
    // ============================================================
    private PlayerDefense CreatePlayer()
    {
        _playerGO = new GameObject("TestPlayer");

        // Dependencia: sistema de energía
        _playerGO.AddComponent<EnergySystem>();

        // Hitbox de bloqueo (desactivado por defecto)
        GameObject blockHitboxGO = new GameObject("BlockHitbox");
        blockHitboxGO.transform.SetParent(_playerGO.transform);
        blockHitboxGO.SetActive(false);

        // Sistema principal bajo prueba
        PlayerDefense defense = _playerGO.AddComponent<PlayerDefense>();
        defense.blockHitbox = blockHitboxGO;

        // Acceso a miembros privados (estado interno del bloqueo)
        System.Type t = typeof(PlayerDefense);
        _isBlockingField = t.GetField("isBlocking", BindingFlags.NonPublic | BindingFlags.Instance);
        _updateMethod = t.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

        return defense;
    }

    // ============================================================
    // LIMPIEZA DESPUÉS DE CADA TEST
    // ============================================================
    // Evita acumulación de GameObjects en memoria.
    // ============================================================
    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.DestroyImmediate(_playerGO);
    }

    // ============================================================
    // TEST 1: DESACTIVACIÓN DEL BLOQUEO
    // ============================================================
    // OBJETIVO:
    // Verificar que el bloqueo se desactiva cuando el input deja de estar activo.
    //
    // FLUJO:
    // 1. Forzar estado de bloqueo
    // 2. Ejecutar Update manualmente
    // 3. Validar que el estado vuelve a falso
    // ============================================================
    [UnityTest]
    public IEnumerator Block_DeactivatesWhenKeyIsReleased()
    {
        PlayerDefense defense = CreatePlayer();

        // Error esperado del sistema de UI (no relevante al test)
        LogAssert.ignoreFailingMessages = true;

        yield return null;

        // Forzar estado interno de bloqueo
        _isBlockingField.SetValue(defense, true);
        defense.blockHitbox.SetActive(true);

        Assert.IsTrue(defense.IsBlocking());
        Assert.IsTrue(defense.blockHitbox.activeSelf);

        // Simulación del ciclo Update()
        _updateMethod.Invoke(defense, null);

        yield return null;

        Assert.IsFalse(defense.IsBlocking());
        Assert.IsFalse(defense.blockHitbox.activeSelf);
    }

    // ============================================================
    // TEST 2: GANANCIA DE ENERGÍA AL BLOQUEAR
    // ============================================================
    // OBJETIVO:
    // Verificar que bloquear incrementa la energía del jugador.
    //
    // REGLA:
    // GainEnergyFromBlock() debe aumentar +3 energía.
    // ============================================================
    [UnityTest]
    public IEnumerator Block_GrantsEnergyWhenBlockingStarts()
    {
        CreatePlayer();

        LogAssert.ignoreFailingMessages = true;
        yield return null;

        EnergySystem energy = _playerGO.GetComponent<EnergySystem>();
        int energyBefore = energy.currentEnergy;

        
        energy.GainEnergyFromBlock();

        Assert.AreEqual(energyBefore + 3, energy.currentEnergy);
        Assert.Greater(energy.currentEnergy, 0);
    }

    // ============================================================
    // TEST 3: CONSISTENCIA DE ESTADO DE BLOQUEO
    // ============================================================
    // OBJETIVO:
    // Validar que el estado interno (isBlocking) coincide
    // con el estado visual (blockHitbox).
    //
    // FLUJO:
    // 1. Estado inicial falso
    // 2. Forzar bloqueo
    // 3. Ejecutar Update
    // 4. Validar sincronización del estado
    // ============================================================
    [UnityTest]
    public IEnumerator Block_AnimationStateMatchesBlockingState()
    {
        PlayerDefense defense = CreatePlayer();

        LogAssert.ignoreFailingMessages = true;
        yield return null;

        Assert.IsFalse(defense.IsBlocking());
        Assert.IsFalse(defense.blockHitbox.activeSelf);

        _isBlockingField.SetValue(defense, true);
        defense.blockHitbox.SetActive(true);

        Assert.IsTrue(defense.IsBlocking());
        Assert.IsTrue(defense.blockHitbox.activeSelf);

        _updateMethod.Invoke(defense, null);

        yield return null;

        Assert.IsFalse(defense.IsBlocking());
        Assert.IsFalse(defense.blockHitbox.activeSelf);
    }
}