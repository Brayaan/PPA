using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public string attackName;
    public int damage = 1;
    private PlayerAttack attack;

    private float lastHitTime = float.NegativeInfinity;
    // Intervalo mínimo entre golpes por activación del hitbox
    public float hitCooldown = 0.2f;

    // OnTriggerStay2D se ejecuta cada frame mientras el collider permanece dentro
    private void OnTriggerStay2D(Collider2D other)
    {
        // Cancelar si el ataque no está activo actualmente
        if (attack == null || !attack.IsAttacking() || !gameObject.activeSelf)
            return;

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            // Evitar que el hitbox golpee al propio personaje
            if (other.gameObject == transform.root.gameObject)
                return;

            PlayerDefense defense = other.GetComponent<PlayerDefense>();

            // Si el objetivo bloquea, empujar al atacante de vuelta
            if (defense != null && defense.IsBlocking())
            {
                Debug.Log(other.name + " bloqueó el ataque");

                Rigidbody2D attackerRb = transform.root.GetComponent<Rigidbody2D>();

                if (attackerRb != null)
                {
                    // Dirección de rebote desde el bloqueador hacia el atacante
                    Vector2 direction = (transform.root.position - other.transform.position).normalized;

                    attackerRb.linearVelocity = Vector2.zero;
                    attackerRb.AddForce(new Vector2(direction.x * 4f, 1.5f), ForceMode2D.Impulse);
                }

                return;
            }

            HealthSystem health = other.GetComponent<HealthSystem>();

            if (health != null)
            {
                // Mathf.Max garantiza que hitCooldown negativo se trate como cero
                if (Time.time - lastHitTime < Mathf.Max(0f, hitCooldown))
                    return;

                lastHitTime = Time.time;

                Vector2 attackerPosition = transform.root.position;

                health.TakeDamage(damage, attackerPosition);

                // Dar energía al objetivo por absorber el impacto
                EnergySystem targetEnergy = other.GetComponent<EnergySystem>();
                if (targetEnergy != null)
                    targetEnergy.GainEnergyFromDamage();

                // Dar energía al atacante por conectar el golpe
                EnergySystem attackerEnergy = transform.root.GetComponent<EnergySystem>();
                if (attackerEnergy != null)
                    attackerEnergy.GainEnergyFromAttack(attackName);

                Debug.Log("Golpeaste a: " + other.name + " con: " + attackName);
            }
        }
    }

    void Start()
    {
        // Buscar PlayerAttack en la raíz de la jerarquía
        attack = transform.root.GetComponent<PlayerAttack>();
    }

    void OnDisable()
    {
        // Resetear cooldown para que el próximo ataque siempre conecte
        lastHitTime = float.NegativeInfinity;
    }
}