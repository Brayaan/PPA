using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// TEST SUITE: CombatCollisionManager
/// 
/// PROPÓSITO:
/// Verificar que el sistema de combate evita la colisión física entre enemigos
/// cuando la configuración enemiesPushEachOther está desactivada.
/// 
/// USO:
/// Este test simula dos enemigos en escena y valida que Unity Physics2D
/// ignore la colisión entre ellos cuando el manager lo indica.
/// 
/// LIMITACIONES:
/// - Depende de Physics2D (motor de físicas de Unity).
/// - No usa escena real; se construye todo en runtime.
/// - Requiere Rigidbody2D + Collider2D para simular interacción.
/// </summary>
public class CombatCollisionManagerTests
{
    // =========================
    // REFERENCIAS DE TEST
    // =========================
    
    // GameObject que contiene el sistema bajo prueba
    private GameObject _managerGO;

    // Dos enemigos simulados para la colisión
    private GameObject _enemy1GO;
    private GameObject _enemy2GO;

    // =========================
    // HELPER: CREACIÓN DE ENEMIGOS
    // =========================
    /// <summary>
    /// Crea un enemigo simulado con:
    /// - Tag "Enemy" (requisito del sistema)
    /// - Rigidbody2D sin gravedad (para evitar caída)
    /// - Collider2D para detección de colisiones
    /// 
    /// USO:
    /// Se usa para generar entidades consistentes en las pruebas
    /// sin necesidad de escena Unity.
    /// </summary>
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

    // =========================
    // TEARDOWN (LIMPIEZA)
    // =========================
    /// <summary>
    /// Se ejecuta después de cada test.
    /// Elimina los objetos creados para evitar contaminación entre pruebas.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (_managerGO != null) Object.Destroy(_managerGO);
        if (_enemy1GO != null) Object.Destroy(_enemy1GO);
        if (_enemy2GO != null) Object.Destroy(_enemy2GO);
    }

    // =========================
    // TEST PRINCIPAL
    // =========================
    /// <summary>
    /// CASO DE PRUEBA:
    /// Verifica que los enemigos NO se empujen entre sí cuando:
    /// enemiesPushEachOther = false
    /// 
    /// FLUJO:
    /// 1. Se crean dos enemigos en posiciones opuestas
    /// 2. Se crea el CombatCollisionManager con colisiones desactivadas
    /// 3. Se espera un frame para que Unity registre físicas
    /// 4. Se valida que Physics2D ignore la colisión entre ambos colliders
    /// 
    /// RESULTADO ESPERADO:
    /// Los enemigos no deben colisionar físicamente entre sí.
    /// </summary>
    [UnityTest]
    public IEnumerator CombatCollisionManager_EnemiesDoNotPushEachOther()
    {
        // Crear enemigos en escena simulada
        _enemy1GO = CreateEnemy("Enemy1", new Vector3(-1f, 0f, 0f));
        _enemy2GO = CreateEnemy("Enemy2", new Vector3(1f, 0f, 0f));

        // Crear sistema de control de colisiones
        _managerGO = new GameObject("CombatCollisionManager");
        CombatCollisionManager manager = _managerGO.AddComponent<CombatCollisionManager>();

        // Desactivar empuje entre enemigos
        manager.enemiesPushEachOther = false;

        // Esperar a que Unity registre colliders en el sistema de físicas
        yield return null;

        // Obtener colliders de ambos enemigos
        Collider2D col1 = _enemy1GO.GetComponent<Collider2D>();
        Collider2D col2 = _enemy2GO.GetComponent<Collider2D>();

        // Validación de existencia de componentes físicos
        Assert.IsNotNull(col1, "Enemy1 debe tener un Collider2D.");
        Assert.IsNotNull(col2, "Enemy2 debe tener un Collider2D.");

        // Verificar si Unity está ignorando la colisión entre ellos
        bool collisionIgnored = Physics2D.GetIgnoreCollision(col1, col2);

        // Validación final del comportamiento del sistema
        Assert.IsTrue(
            collisionIgnored,
            "Physics2D debe ignorar la colisión entre enemigos cuando enemiesPushEachOther = false."
        );
    }
}