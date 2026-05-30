using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TEST SUITE: EnergySystem
/// 
/// PROPÓSITO:
/// Validar el sistema de energía del jugador en combate.
/// 
/// Este sistema controla cómo se gana y consume energía a través de:
/// - Ataques (puñetazo, patada)
/// - Recibir daño
/// - Bloquear
/// 
/// También valida límites:
/// - No puede superar el máximo de energía
/// - No puede bajar de 0
/// 
/// DEPENDENCIA IMPORTANTE:
/// EnergySystem depende de UI (energyImage), pero en tests se deja null.
/// Esto genera LogError controlado que se ignora con LogAssert.
/// </summary>
public class EnergySystemTests
{
    // =========================
    // REFERENCIA DEL JUGADOR
    // =========================
    private GameObject _playerGO;

    // =========================
    // HELPER: CREACIÓN DEL SISTEMA
    // =========================
    /// <summary>
    /// Crea un jugador simulado con EnergySystem.
    /// No se asigna UI intencionalmente (energyImage = null),
    /// ya que no se está probando la interfaz.
    /// </summary>
    private EnergySystem CreateEnergySystem()
    {
        _playerGO = new GameObject("TestPlayer");
        EnergySystem energy = _playerGO.AddComponent<EnergySystem>();
        return energy;
    }

    // =========================
    // TEARDOWN (LIMPIEZA)
    // =========================
    /// <summary>
    /// Limpia el GameObject después de cada test
    /// para evitar interferencia entre pruebas.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null)
            Object.Destroy(_playerGO);
    }

    // =========================
    // TEST 1: GANANCIA POR ATAQUE
    // =========================
    /// <summary>
    /// Verifica que diferentes ataques generan distinta cantidad de energía:
    /// - Puñetazo → +5
    /// - Patada → +10
    /// - Desconocido → +0
    /// </summary>
    [UnityTest]
    public IEnumerator GainEnergyFromAttack_IncreasesEnergyByCorrectAmount()
    {
        EnergySystem energy = CreateEnergySystem();

        // Start del sistema → genera LogError por UI faltante
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        int before = energy.currentEnergy;

        // Puñetazo
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Puñetazo");

        Assert.AreEqual(before + 5, energy.currentEnergy);

        before = energy.currentEnergy;

        // Patada
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Patada");

        Assert.AreEqual(before + 10, energy.currentEnergy);

        before = energy.currentEnergy;

        // Ataque desconocido
        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Desconocido");

        Assert.AreEqual(before, energy.currentEnergy);
    }

    // =========================
    // TEST 2: DAÑO RECIBIDO
    // =========================
    /// <summary>
    /// Verifica que recibir daño también genera energía (+2).
    /// </summary>
    [UnityTest]
    public IEnumerator GainEnergyFromDamage_IncreasesEnergyByTwo()
    {
        EnergySystem energy = CreateEnergySystem();

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        int before = energy.currentEnergy;

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromDamage();

        Assert.AreEqual(before + 2, energy.currentEnergy);
        Assert.Greater(energy.currentEnergy, 0);
    }

    // =========================
    // TEST 3: BLOQUEO
    // =========================
    /// <summary>
    /// Verifica que bloquear genera energía (+3).
    /// </summary>
    [UnityTest]
    public IEnumerator GainEnergyFromBlock_IncreasesEnergyByThree()
    {
        EnergySystem energy = CreateEnergySystem();

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        int before = energy.currentEnergy;

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromBlock();

        Assert.AreEqual(before + 3, energy.currentEnergy);
        Assert.IsTrue(energy.currentEnergy > 0);
    }

    // =========================
    // TEST 4: LÍMITE MÁXIMO
    // =========================
    /// <summary>
    /// Verifica que la energía no puede superar maxEnergy.
    /// </summary>
    [UnityTest]
    public IEnumerator Energy_DoesNotExceedMaxEnergy()
    {
        EnergySystem energy = CreateEnergySystem();

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        // Colocar energía cerca del máximo
        energy.currentEnergy = energy.maxEnergy - 1;

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.GainEnergyFromAttack("Patada");

        Assert.AreEqual(energy.maxEnergy, energy.currentEnergy);
        Assert.IsTrue(energy.IsFull());
    }

    // =========================
    // TEST 5: LÍMITE MÍNIMO
    // =========================
    /// <summary>
    /// Verifica que la energía nunca baja de 0.
    /// </summary>
    [UnityTest]
    public IEnumerator Energy_DoesNotGoBelowZero()
    {
        EnergySystem energy = CreateEnergySystem();

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        yield return null;

        LogAssert.Expect(LogType.Error, "energyImage no está asignada en el Inspector");
        energy.ConsumeEnergy(energy.maxEnergy + 50);

        Assert.AreEqual(0, energy.currentEnergy);
        Assert.IsFalse(energy.IsFull());
    }
}