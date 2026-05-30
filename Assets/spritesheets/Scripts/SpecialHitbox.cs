using UnityEngine;

public class SpecialHitbox : MonoBehaviour
{
    [HideInInspector]
    public int damage = 20;

    public float specialKnockbackForce = 10f;

    private float lastHitTime = float.NegativeInfinity;
    public float hitCooldown = 0.15f;
    public GameObject owner;

    void OnEnable()
    {
        lastHitTime = float.NegativeInfinity;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        OnTriggerStay2D(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log($"[{gameObject.name}] Detectó colisión con: {other.name} (Root: {other.transform.root.name})");

        // Determinar el root del atacante
        Transform attackerRoot = owner != null ? owner.transform.root : transform.root;

        // Ignorar todo lo que no sea Enemy o Player
        if (!other.transform.root.CompareTag("Enemy") && !other.transform.root.CompareTag("Player"))
        {
            Debug.Log($"[{gameObject.name}] Ignorado porque {other.transform.root.name} no es Enemy ni Player.");
            return;
        }

        // Evitar golpearse a sí mismo
        if (other.transform.root == attackerRoot)
        {
            Debug.Log($"[{gameObject.name}] Ignorado porque es el mismo atacante.");
            return;
        }

        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        Vector2 attackerPos = attackerRoot.position;

        // Intentar EnemyHealthSystem primero (enemigos reales)
        EnemyHealthSystem enemyHealth = other.GetComponentInParent<EnemyHealthSystem>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage, attackerPos);
            
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitSound("Special");
            ApplyExtraKnockback(other, attackerPos);
            Debug.Log($"[Especial] Golpe a {other.name} — daño: {damage}");
            return;
        }

        // Fallback: HealthSystem (Player o Duplicate)
        HealthSystem health = other.GetComponentInParent<HealthSystem>();

        if (health != null)
        {
            PlayerDefense defense = other.GetComponentInParent<PlayerDefense>();

            if (defense != null && defense.IsBlocking())
            {
                Debug.Log("Ataque especial bloqueado por " + other.name);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayBlockSound();
                return;
            }

            health.TakeDamage(damage, attackerPos);
            
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitSound("Special");
            ApplyExtraKnockback(other, attackerPos);

            EnergySystem targetEnergy = other.GetComponentInParent<EnergySystem>();
            if (targetEnergy != null)
                targetEnergy.GainEnergyFromDamage();

            Debug.Log($"[Especial] Golpe a {other.name} — daño: {damage}");
        }
    }

    void ApplyExtraKnockback(Collider2D target, Vector2 attackerPos)
    {
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 dir = ((Vector3)target.transform.position - (Vector3)attackerPos).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dir.x * specialKnockbackForce, 3f), ForceMode2D.Impulse);
    }
}