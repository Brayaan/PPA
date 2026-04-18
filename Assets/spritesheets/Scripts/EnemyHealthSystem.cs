using UnityEngine;

public class EnemyHealthSystem : MonoBehaviour
{
    public int maxHealth = 50;
    public int currentHealth;

    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    private Rigidbody2D rb;
    // Previene stacking de knockback durante el hit-stun
    private bool isHit = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        currentHealth -= damage;

        // Clampear vida para no bajar de cero
        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log($"Enemigo vida: {currentHealth}");

        ApplyHit(attackerPosition);

        // Verificar muerte después de aplicar el golpe
        if (currentHealth <= 0)
            Die();
    }

    void ApplyHit(Vector2 attackerPosition)
    {
        // Ignorar golpe si el hit-stun aún está activo
        if (isHit) return;

        isHit = true;

        // Dirección del empuje opuesta al atacante
        Vector2 direction = (transform.position - (Vector3)attackerPosition).normalized;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(direction.x * knockbackForce, 2f), ForceMode2D.Impulse);
        }

        // Mathf.Max evita que duration negativo colapse el cooldown
        Invoke(nameof(EndHit), Mathf.Max(0f, knockbackDuration));
    }

    void EndHit()
    {
        isHit = false;
    }

    void Die()
    {
        Debug.Log("Enemigo derrotado");
        // Delay para permitir efectos visuales antes de destruir
        Destroy(gameObject, 0.5f);
    }
}