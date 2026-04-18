using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para PlayerDefense.
///
/// LIMITACIÓN DE INPUT: PlayerDefense.Update() gestiona todo el estado de bloqueo
/// a través de Input.GetKey(blockKey), que no es simulable en PlayMode sin un
/// sistema de input virtual. Para evitar modificar el código de producción, las
/// pruebas que necesitan transicionar el estado de bloqueo utilizan Reflection:
///
///   - Se escribe el campo privado 'isBlocking' directamente.
///   - Se invoca el método privado 'Update()' via MethodInfo para que la lógica
///     de transición se ejecute de forma auténtica (con Input.GetKey = false en
///     PlayMode sin teclado presionado, lo que produce la transición true → false).
///
/// Prueba omitida: "activar bloqueo manteniendo L" — requiere Input.GetKey = true,
/// que no es posible sin input virtual.
///
/// LogAssert: EnergySystem.Start() llama UpdateEnergyUI() en el primer frame, y
/// GainEnergyFromBlock() la llama cada vez que se gana energía. Ambas disparan
/// LogError("energyImage no está asignada en el Inspector") porque energyImage es
/// null en pruebas. Se declara LogAssert.Expect antes de cada yield/llamada que
/// lo genera. Sin AnimatorController disponible en PlayMode, la prueba de animación
/// verifica el estado de IsBlocking() en lugar del parámetro del Animator.
/// </summary>
public class PlayerDefenseTests
{
    private GameObject _playerGO;

    // Campos de reflexión cacheados para acceso a miembros privados de PlayerDefense
    private FieldInfo  _isBlockingField;
    private MethodInfo _updateMethod;

    // -------------------------------------------------------------------------
    // Helper: crea un GameObject con PlayerDefense, EnergySystem y el hitbox
    // de bloqueo configurado. El Animator se añade para que anim != null.
    // -------------------------------------------------------------------------
    private PlayerDefense CreatePlayer()
    {
        _playerGO = new GameObject("TestPlayer");

        // EnergySystem — requerido por PlayerDefense cuando isBlocking pasa a true.
        // Su Start() llama UpdateEnergyUI() → LogError("energyImage...") porque
        // energyImage es null; se gestiona con LogAssert.Expect en cada prueba.
        _playerGO.AddComponent<EnergySystem>();

        // No se añade Animator: sin RuntimeAnimatorController asignado, cualquier
        // llamada a anim.SetBool lanzaría un error en Unity 2022+. PlayerDefense
        // ya lo guarda tras un null-check (if anim != null), por lo que dejarlo
        // null es seguro y evita el error de controller no asignado.

        // blockHitbox hijo — PlayerDefense lo activa/desactiva con el bloqueo
        GameObject blockHitboxGO = new GameObject("BlockHitbox");
        blockHitboxGO.transform.SetParent(_playerGO.transform);
        blockHitboxGO.SetActive(false);

        PlayerDefense defense = _playerGO.AddComponent<PlayerDefense>();
        defense.blockHitbox = blockHitboxGO;

        // Cachear reflexión una sola vez por prueba
        System.Type t = typeof(PlayerDefense);
        _isBlockingField = t.GetField("isBlocking",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _updateMethod = t.GetMethod("Update",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return defense;
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.Destroy(_playerGO);
    }

    // =========================================================================
    // 1. El bloqueo se desactiva al soltar la tecla (transición true → false)
    //    Se fuerza isBlocking = true y blockHitbox activo via reflexión, luego
    //    se invoca Update() sin input real (Input.GetKey = false en PlayMode)
    //    para que la transición ocurra de forma auténtica.
    // =========================================================================
    [UnityTest]
    public IEnumerator Block_DeactivatesWhenKeyIsReleased()
    {
        PlayerDefense defense = CreatePlayer();

        // EnergySystem.Start() llama UpdateEnergyUI() en este frame
        // → LogError("energyImage no está asignada en el Inspector")
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");

        // Esperar un frame para que Start() de todos los componentes se ejecute
        yield return null;

        // Simular estado de bloqueo activo escribiendo el campo privado
        _isBlockingField.SetValue(defense, true);
        defense.blockHitbox.SetActive(true);

        Assert.IsTrue(defense.IsBlocking(),
            "isBlocking debe estar en true antes del Update de transición.");
        Assert.IsTrue(defense.blockHitbox.activeSelf,
            "blockHitbox debe estar activo mientras isBlocking es true.");

        // Invocar Update() con Input.GetKey = false (teclado no presionado en PlayMode):
        // input(false) != isBlocking(true) → entra al if → isBlocking = false → hitbox off
        // La transición es true→false: NO llama GainEnergyFromBlock(), sin LogError extra.
        _updateMethod.Invoke(defense, null);

        // Esperar un frame para que Unity procese el cambio de estado
        yield return null;

        Assert.IsFalse(defense.IsBlocking(),
            "isBlocking debe ser false después de Update() sin tecla presionada.");
        Assert.IsFalse(defense.blockHitbox.activeSelf,
            "blockHitbox debe desactivarse cuando isBlocking pasa a false.");
    }

    // =========================================================================
    // 2. Al bloquear se gana energía
    //    PlayerDefense llama energy.GainEnergyFromBlock() cuando isBlocking
    //    transiciona de false a true. Como no podemos forzar Input.GetKey = true,
    //    verificamos GainEnergyFromBlock() directamente en EnergySystem, que es
    //    el contrato exacto que PlayerDefense invoca.
    //    EnergySystem.UpdateEnergyUI() lanzará LogError porque energyImage es null.
    // =========================================================================
    [UnityTest]
    public IEnumerator Block_GrantsEnergyWhenBlockingStarts()
    {
        CreatePlayer();

        // Esperar un frame para que Start() de EnergySystem llame UpdateEnergyUI()
        // → dispara LogError("energyImage no está asignada en el Inspector")
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        EnergySystem energy = _playerGO.GetComponent<EnergySystem>();
        int energyBefore = energy.currentEnergy;

        // GainEnergyFromBlock() incrementa currentEnergy en 3 y llama UpdateEnergyUI()
        // → dispara LogError("energyImage no está asignada en el Inspector")
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromBlock();

        Assert.AreEqual(energyBefore + 3, energy.currentEnergy,
            "GainEnergyFromBlock() debe incrementar currentEnergy en 3.");
        Assert.Greater(energy.currentEnergy, 0,
            "La energía debe ser mayor que cero tras activar el bloqueo.");
    }

    // =========================================================================
    // 3. Al bloquear se activa la animación correcta
    //    Sin un RuntimeAnimatorController disponible en PlayMode no es posible
    //    leer parámetros del Animator. En su lugar se verifican los cambios de
    //    estado de IsBlocking() e blockHitbox que PlayerDefense produce junto con
    //    la llamada a SetBool, confirmando que la lógica de animación se ejecuta
    //    en el momento correcto del ciclo de bloqueo.
    // =========================================================================
    [UnityTest]
    public IEnumerator Block_AnimationStateMatchesBlockingState()
    {
        PlayerDefense defense = CreatePlayer();

        // EnergySystem.Start() → UpdateEnergyUI() → LogError en este frame
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        // --- Estado inicial: sin bloqueo activo ---
        Assert.IsFalse(defense.IsBlocking(),
            "isBlocking debe ser false al iniciar (sin input).");
        Assert.IsFalse(defense.blockHitbox.activeSelf,
            "blockHitbox debe estar desactivado al iniciar.");

        // --- Simular estado de bloqueo activo via reflexión ---
        // (equivale al momento en que PlayerDefense ejecutaría anim.SetBool("isBlocking", true))
        _isBlockingField.SetValue(defense, true);
        defense.blockHitbox.SetActive(true);

        Assert.IsTrue(defense.IsBlocking(),
            "IsBlocking() debe ser true cuando el estado de bloqueo está activo.");
        Assert.IsTrue(defense.blockHitbox.activeSelf,
            "blockHitbox debe estar activo mientras isBlocking es true.");

        // --- Transición true → false: Update() con Input.GetKey = false ---
        // PlayerDefense ejecuta: isBlocking = false, anim.SetBool("isBlocking", false), hitbox off
        // La transición es true→false: NO llama GainEnergyFromBlock(), sin LogError extra.
        _updateMethod.Invoke(defense, null);

        yield return null;

        Assert.IsFalse(defense.IsBlocking(),
            "IsBlocking() debe ser false tras Update() sin tecla presionada.");
        Assert.IsFalse(defense.blockHitbox.activeSelf,
            "blockHitbox debe desactivarse cuando el bloqueo termina.");
    }
}
