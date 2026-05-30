using UnityEngine;

public class EnemyHealthSystem : MonoBehaviour
{
    public int maxHealth = 50;
    public int currentHealth;

    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    private Rigidbody2D rb;
    private bool isHit = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log($"Enemigo vida: {currentHealth}");

        ApplyHit(attackerPosition);

        if (currentHealth <= 0)
            Die();
    }

    void ApplyHit(Vector2 attackerPosition)
    {
        if (isHit) return;

        isHit = true;

        Vector2 direction = (transform.position - (Vector3)attackerPosition).normalized;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.AddForce(new Vector2(direction.x * knockbackForce, 0f), ForceMode2D.Impulse);
        }

        Invoke(nameof(EndHit), Mathf.Max(0f, knockbackDuration));
    }

    void EndHit()
    {
        isHit = false;
    }

    void Die()
    {
        Debug.Log("Enemigo derrotado");
        Destroy(gameObject, 0.5f);
    }
}