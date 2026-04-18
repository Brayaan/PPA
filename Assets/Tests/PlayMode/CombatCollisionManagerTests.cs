using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas PlayMode para CombatCollisionManager.
///
/// CombatCollisionManager.Start() llama ConfigureAllEnemies(), que busca todos
/// los GameObjects con tag "Enemy" en la escena y aplica
/// Physics2D.IgnoreCollision(colA, colB, true) entre sus Collider2D.
///
/// El efecto se verifica con Physics2D.GetIgnoreCollision(colA, colB), que
/// devuelve true si la colisión entre ambos colliders ha sido ignorada.
///
/// Prerequisito: el tag "Enemy" debe existir en Project Settings → Tags.
/// CombatCollisionManager.Start() lo protege con try/catch; si el tag falta
/// lanza LogError("El tag 'Enemy' no está registrado...") y retorna sin
/// configurar nada — en ese caso la prueba fallará igualmente por el
/// LogError no declarado, alertando del problema de configuración.
///
/// LogAssert: ningún LogError se espera en el camino de código probado
/// (tag "Enemy" existe, ambos enemigos tienen Collider2D y Rigidbody2D).
/// El Debug.LogWarning de collider nulo no ocurre porque los GOs sí tienen
/// Collider2D; los warnings no rompen las pruebas en ningún caso.
/// </summary>
public class CombatCollisionManagerTests
{
    private GameObject _managerGO;
    private GameObject _enemy1GO;
    private GameObject _enemy2GO;

    // -------------------------------------------------------------------------
    // Helper: crea un enemigo con tag "Enemy", Rigidbody2D y BoxCollider2D.
    // gravityScale = 0 para que las físicas no interfieran.
    // -------------------------------------------------------------------------
    private GameObject CreateEnemy(string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        go.tag = "Enemy";

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        go.AddComponent<BoxCollider2D>();

        return go;
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerGO != null) Object.Destroy(_managerGO);
        if (_enemy1GO  != null) Object.Destroy(_enemy1GO);
        if (_enemy2GO  != null) Object.Destroy(_enemy2GO);
    }

    // =========================================================================
    // Los enemigos no se empujan entre sí al inicio del juego
    // CombatCollisionManager.Start() llama Physics2D.IgnoreCollision entre los
    // Collider2D de todos los GameObjects con tag "Enemy" cuando
    // enemiesPushEachOther == false (valor por defecto).
    // Se verifica con Physics2D.GetIgnoreCollision(col1, col2) == true.
    // =========================================================================
    [UnityTest]
    public IEnumerator CombatCollisionManager_EnemiesDoNotPushEachOther()
    {
        // Crear dos enemigos con tag "Enemy", Rigidbody2D y Collider2D
        _enemy1GO = CreateEnemy("Enemy1", new Vector3(-1f, 0f, 0f));
        _enemy2GO = CreateEnemy("Enemy2", new Vector3( 1f, 0f, 0f));

        // Crear el manager con enemiesPushEachOther = false (valor por defecto)
        _managerGO = new GameObject("CombatCollisionManager");
        CombatCollisionManager manager = _managerGO.AddComponent<CombatCollisionManager>();
        manager.enemiesPushEachOther = false;

        // Esperar un frame para que Start() ejecute ConfigureAllEnemies()
        // y aplique Physics2D.IgnoreCollision entre los colliders de los enemigos
        yield return null;

        Collider2D col1 = _enemy1GO.GetComponent<Collider2D>();
        Collider2D col2 = _enemy2GO.GetComponent<Collider2D>();

        Assert.IsNotNull(col1, "Enemy1 debe tener un Collider2D.");
        Assert.IsNotNull(col2, "Enemy2 debe tener un Collider2D.");

        // Physics2D.GetIgnoreCollision devuelve true si IgnoreCollision fue aplicado
        bool collisionIgnored = Physics2D.GetIgnoreCollision(col1, col2);

        Assert.IsTrue(collisionIgnored,
            "Physics2D.GetIgnoreCollision debe devolver true: CombatCollisionManager " +
            "debe haber ignorado la colisión entre los Collider2D de los dos enemigos.");
    }
}
