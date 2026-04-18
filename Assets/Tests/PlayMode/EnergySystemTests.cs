using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para EnergySystem.
/// Prueba omitida: actualización de UI de energía — depende de sprites externos
/// cargados desde Resources y de una Image de Unity UI asignada en el Inspector.
///
/// LogAssert: EnergySystem.UpdateEnergyUI() lanza LogError("energyImage no está
/// asignada en el Inspector") en CADA llamada porque energyImage es null en
/// pruebas. Esto ocurre en:
///   - Start()                    → primer yield return null de cada prueba.
///   - GainEnergyFromDamage()     → llamada directa.
///   - GainEnergyFromAttack()     → llamada directa.
///   - GainEnergyFromBlock()      → llamada directa.
///   - ConsumeEnergy()            → llamada directa.
/// Se declara LogAssert.Expect antes de cada yield/llamada que lo dispara.
/// </summary>
public class EnergySystemTests
{
    private GameObject _playerGO;

    // -------------------------------------------------------------------------
    // Helper: crea un GameObject con EnergySystem listo para pruebas.
    // energyImage se deja null a propósito; UpdateEnergyUI() lo maneja con guard.
    // -------------------------------------------------------------------------
    private EnergySystem CreateEnergySystem()
    {
        _playerGO = new GameObject("TestPlayer");
        EnergySystem energy = _playerGO.AddComponent<EnergySystem>();
        // energyImage null → UpdateEnergyUI() lanzará LogError gestionado con LogAssert
        return energy;
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.Destroy(_playerGO);
    }

    // =========================================================================
    // 1. Al golpear al enemigo sube la energía del atacante
    //    GainEnergyFromAttack("Puñetazo") gana 5; GainEnergyFromAttack("Patada") gana 10.
    // =========================================================================
    [UnityTest]
    public IEnumerator GainEnergyFromAttack_IncreasesEnergyByCorrectAmount()
    {
        EnergySystem energy = CreateEnergySystem();

        // Start() → UpdateEnergyUI() → LogError("energyImage...")
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null; // currentEnergy queda en 0 tras Start()

        // --- Puñetazo: +5 ---
        int before = energy.currentEnergy; // 0
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Puñetazo");

        Assert.AreEqual(before + 5, energy.currentEnergy,
            "GainEnergyFromAttack('Puñetazo') debe incrementar currentEnergy en 5.");

        // --- Patada: +10 ---
        before = energy.currentEnergy; // 5
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Patada");

        Assert.AreEqual(before + 10, energy.currentEnergy,
            "GainEnergyFromAttack('Patada') debe incrementar currentEnergy en 10.");

        // --- Nombre desconocido: +0 ---
        before = energy.currentEnergy; // 15
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Desconocido");

        Assert.AreEqual(before, energy.currentEnergy,
            "GainEnergyFromAttack con nombre desconocido no debe cambiar currentEnergy.");
    }

    // =========================================================================
    // 2. Al recibir daño sube la energía del receptor
    //    GainEnergyFromDamage() suma 2 por llamada.
    // =========================================================================
    [UnityTest]
    public IEnumerator GainEnergyFromDamage_IncreasesEnergyByTwo()
    {
        EnergySystem energy = CreateEnergySystem();

        // Start() → UpdateEnergyUI() → LogError
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        int before = energy.currentEnergy; // 0

        // GainEnergyFromDamage() → UpdateEnergyUI() → LogError
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromDamage();

        Assert.AreEqual(before + 2, energy.currentEnergy,
            "GainEnergyFromDamage() debe incrementar currentEnergy en 2.");
        Assert.Greater(energy.currentEnergy, 0,
            "La energía debe ser mayor que cero tras recibir daño.");
    }

    // =========================================================================
    // 3. Al bloquear sube la energía
    //    GainEnergyFromBlock() suma 3 por activación.
    // =========================================================================
    [UnityTest]
    public IEnumerator GainEnergyFromBlock_IncreasesEnergyByThree()
    {
        EnergySystem energy = CreateEnergySystem();

        // Start() → UpdateEnergyUI() → LogError
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        int before = energy.currentEnergy; // 0

        // GainEnergyFromBlock() → UpdateEnergyUI() → LogError
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromBlock();

        Assert.AreEqual(before + 3, energy.currentEnergy,
            "GainEnergyFromBlock() debe incrementar currentEnergy en 3.");
        Assert.IsTrue(energy.currentEnergy > 0,
            "La energía debe ser mayor que cero tras activar el bloqueo.");
    }

    // =========================================================================
    // 4. La energía no supera maxEnergy (100)
    //    Si currentEnergy + ganancia > maxEnergy, queda clampeada en maxEnergy.
    // =========================================================================
    [UnityTest]
    public IEnumerator Energy_DoesNotExceedMaxEnergy()
    {
        EnergySystem energy = CreateEnergySystem();

        // Start() → UpdateEnergyUI() → LogError
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        // Llevar currentEnergy casi al límite manualmente (campo público)
        energy.currentEnergy = energy.maxEnergy - 1; // 99

        // GainEnergyFromAttack("Patada") intenta sumar 10 → debería clampear a 100
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Patada");

        Assert.AreEqual(energy.maxEnergy, energy.currentEnergy,
            "currentEnergy no debe superar maxEnergy aunque la ganancia lo exceda.");
        Assert.IsTrue(energy.IsFull(),
            "IsFull() debe devolver true cuando currentEnergy == maxEnergy.");
    }

    // =========================================================================
    // 5. La energía no baja de 0
    //    ConsumeEnergy() clampea currentEnergy a 0 si el consumo es excesivo.
    // =========================================================================
    [UnityTest]
    public IEnumerator Energy_DoesNotGoBelowZero()
    {
        EnergySystem energy = CreateEnergySystem();

        // Start() → UpdateEnergyUI() → LogError
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        // currentEnergy empieza en 0 tras Start(); consumir más de lo disponible
        // ConsumeEnergy() → UpdateEnergyUI() → LogError
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.ConsumeEnergy(energy.maxEnergy + 50);

        Assert.AreEqual(0, energy.currentEnergy,
            "currentEnergy no debe bajar de cero aunque el consumo sea excesivo.");
        Assert.IsFalse(energy.IsFull(),
            "IsFull() debe devolver false cuando currentEnergy es 0.");
    }
}
