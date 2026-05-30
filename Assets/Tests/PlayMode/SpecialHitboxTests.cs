using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

public class SpecialHitboxTests
{
    private GameObject attackerGO;
    private GameObject victimGO;
    private SpecialHitbox hitbox;
    private HealthSystem victimHealth;

    [SetUp]
    public void SetUp()
    {

        attackerGO = new GameObject("Attacker");
        attackerGO.tag = "Player"; // Necesario para que no se ignore

        victimGO = new GameObject("Victim");
        victimGO.tag = "Enemy";

        // Configurar hitbox
        GameObject hitboxGO = new GameObject("Hitbox");
        hitboxGO.transform.SetParent(attackerGO.transform);
        hitbox = hitboxGO.AddComponent<SpecialHitbox>();
        hitbox.owner = attackerGO;
        hitbox.damage = 25;

        // Configurar victima
        victimHealth = victimGO.AddComponent<HealthSystem>();
        victimHealth.maxHealth = 100;
        victimHealth.currentHealth = 100;
        victimHealth.healthImage = new GameObject("DummyImage").AddComponent<Image>();
        
        // Simulamos Collider
        BoxCollider2D victimCol = victimGO.AddComponent<BoxCollider2D>();
    }

    [TearDown]
    public void TearDown()
    {
        if (attackerGO != null) Object.Destroy(attackerGO);
        if (victimGO != null) Object.Destroy(victimGO);
    }

    [UnityTest]
    public IEnumerator SpecialHitbox_OnTriggerStay_DealsDamageToEnemy()
    {
        LogAssert.ignoreFailingMessages = true;

        // Forzamos el OnTriggerStay simulando que colisionaron usando Reflection
        MethodInfo onTriggerMethod = typeof(SpecialHitbox).GetMethod("OnTriggerStay2D", BindingFlags.NonPublic | BindingFlags.Instance);

        // Pasarle el collider de la victima
        Collider2D col = victimGO.GetComponent<Collider2D>();

        Assert.AreEqual(100, victimHealth.currentHealth, "La victima deberia empezar con 100 HP.");

        onTriggerMethod.Invoke(hitbox, new object[] { col });

        yield return null;

        Assert.AreEqual(75, victimHealth.currentHealth, "La victima debio perder 25 HP por el daño.");
    }
}
