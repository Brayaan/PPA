using UnityEngine;

public class ExtendedHitbox : MonoBehaviour
{
    private Hitbox originalHitbox;
    private PlayerAttack playerAttack;

    void Start()
    {
        // Obtener referencias desde el mismo objeto y la raíz
        originalHitbox = GetComponent<Hitbox>();
        playerAttack = transform.root.GetComponent<PlayerAttack>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Cancelar si faltan componentes críticos del sistema
        if (originalHitbox == null || playerAttack == null)
            return;

        // Solo procesar durante un ataque activo
        if (!playerAttack.IsAttacking() || !gameObject.activeSelf)
            return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealthSystem enemyHealth = other.GetComponent<EnemyHealthSystem>();
            PlayerDefense defense = other.GetComponentInParent<PlayerDefense>();

            if (defense != null && defense.IsBlocking())
            {
                Debug.Log("Ataque bloqueado por " + other.name);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayBlockSound();
                return;
            }

            if (enemyHealth != null)
            {
                Vector2 attackerPosition = transform.root.position;
                // Usar el daño del Hitbox padre, no un valor fijo
                int damage = originalHitbox.damage;

                enemyHealth.TakeDamage(damage, attackerPosition);
                
                if (AudioManager.Instance != null) AudioManager.Instance.PlayHitSound(originalHitbox.attackName);

                // Dar energía al atacante por conectar el golpe
                EnergySystem attackerEnergy = transform.root.GetComponent<EnergySystem>();
                if (attackerEnergy != null)
                {
                    attackerEnergy.GainEnergyFromAttack("Puñetazo");
                }

                Debug.Log($"Golpe a enemigo!");
            }
        }
    }
}