using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CombatManagerTests
{
    private GameObject managerGO;
    private CombatManager combatManager;

    [SetUp]
    public void SetUp()
    {
        // Limpiar instancias previas por si quedaron de otros tests
        if (CombatManager.Instance != null)
        {
            Object.DestroyImmediate(CombatManager.Instance.gameObject);
        }

        managerGO = new GameObject("CombatManager");
        combatManager = managerGO.AddComponent<CombatManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (managerGO != null)
        {
            Object.Destroy(managerGO);
        }
    }

    [Test]
    public void CombatManager_Singleton_IsCreated()
    {
        // Se espera 1 frame para que Awake() se asegure de asignarlo (si fuera un objeto real, pero al usar AddComponent se llama Awake inmediatamente)
        Assert.IsNotNull(CombatManager.Instance, "El singleton de CombatManager deberia estar asignado.");
        Assert.AreEqual(combatManager, CombatManager.Instance, "La instancia debe ser el componente creado.");
    }

    [UnityTest]
    public IEnumerator CombatManager_StartMatch_SetsRoundActive()
    {
        // Establecer tiempo entre rondas a 0 para acelerar el test
        combatManager.timeBetweenRounds = 0f;
        
        combatManager.StartMatch();

        // Esperamos un momento para que termine la corrutina StartNewRoundCoroutine
        yield return new WaitForSeconds(0.1f);

        Assert.IsTrue(combatManager.isRoundActive, "La ronda debe estar activa despues de StartMatch y de esperar el tiempo de inicio.");
        Assert.IsFalse(combatManager.isCombatEnded, "El combate no deberia estar finalizado.");
    }
}
