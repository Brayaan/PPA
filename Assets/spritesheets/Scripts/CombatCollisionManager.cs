using UnityEngine;

public class CombatCollisionManager : MonoBehaviour
{
    public bool enemiesPushEachOther = false;

    void Start()
    {
        ConfigureAllEnemies();
    }

    private void ConfigureAllEnemies()
    {
        // FindGameObjectsWithTag lanza excepción si el tag no existe
        GameObject[] enemies;
        try
        {
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
        }
        catch (UnityEngine.UnityException)
        {
            Debug.LogError("El tag 'Enemy' no está registrado en Project Settings > Tags", this);
            return;
        }

        // Obtener todos los colliders una sola vez antes del loop
        Collider2D[] allColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

        foreach (GameObject enemy in enemies)
        {
            ConfigureEnemyPhysics(enemy, allColliders);
        }
    }

    private void ConfigureEnemyPhysics(GameObject enemy, Collider2D[] allColliders)
    {
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            if (!enemiesPushEachOther)
            {
                // Prevenir que la física rote al Rigidbody
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                Collider2D myCollider = enemy.GetComponent<Collider2D>();

                // Saltar enemigos sin Collider2D para evitar NullReferenceException
                if (myCollider == null)
                {
                    Debug.LogWarning("El enemigo " + enemy.name + " no tiene Collider2D", enemy);
                    return;
                }

                // Ignorar colisiones físicas entre todos los enemigos
                foreach (Collider2D other in allColliders)
                {
                    if (other.gameObject.CompareTag("Enemy") && other.gameObject != enemy)
                    {
                        Physics2D.IgnoreCollision(myCollider, other, true);
                    }
                }
            }
        }
    }
}