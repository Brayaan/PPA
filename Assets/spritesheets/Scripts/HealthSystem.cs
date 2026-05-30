using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    public Image healthImage;

    private Sprite[] healthSprites;

    public float knockbackForce = 5f;
    public float knockbackDuration = 0.05f;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private Animator anim;

    private bool isHit = false;

    // ── NUEVO: evitar que TakeDamage se llame tras la muerte ──
    private bool isDead = false;

    // Posición inicial para reiniciar en cada ronda
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        healthSprites = Resources.LoadAll<Sprite>("HeartCounter/heart_counter-Sheet");

        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        anim = GetComponent<Animator>();

        UpdateHealthUI();
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        // ── NUEVO: ignorar daño si ya está muerto ──
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log("Vida actual: " + currentHealth);

        UpdateHealthUI();

        // ── NUEVO: verificar muerte antes de aplicar hit-stun ──
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        ApplyHit(attackerPosition);
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[Healing] Vida restaurada: +{amount} → {currentHealth}/{maxHealth}");
        UpdateHealthUI();
    }

    // ── NUEVO: muerte del jugador ──
    void Die()
    {
        isDead = true;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        // Disparar animación de recibir daño → Death fluirá desde el Animator
        if (anim != null)
        {
            if (TieneParametro("Hit"))
                anim.SetTrigger("Hit");

            // isDead como bool para que el Animator transite hacia Death
            if (TieneParametro("isDead"))
                anim.SetBool("isDead", true);
        }

        // Notificar al CombatManager que este jugador perdió
        if (CombatManager.Instance != null)
            CombatManager.Instance.NotifyPlayerDeath(this);

        Debug.Log($"[HealthSystem] {gameObject.name} murió.");
    }

    void ApplyHit(Vector2 attackerPosition)
    {
        if (isHit) return;

        isHit = true;

        if (movement != null)
            movement.enabled = false;

        if (anim != null && TieneParametro("Hit"))
            anim.SetTrigger("Hit");

        Vector2 direction = (transform.position - (Vector3)attackerPosition).normalized;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(direction.x * knockbackForce, 0f), ForceMode2D.Impulse);
        }

        Invoke(nameof(EndHit), Mathf.Max(0f, knockbackDuration));
    }

    bool TieneParametro(string nombre)
    {
        foreach (var param in anim.parameters)
            if (param.name == nombre) return true;
        return false;
    }

    void EndHit()
    {
        isHit = false;

        if (movement != null && !isDead)
        {
            if (CombatManager.Instance == null || !CombatManager.Instance.isCombatEnded)
            {
                movement.enabled = true;
            }
        }
    }

    void UpdateHealthUI()
    {
        if (maxHealth <= 0)
        {
            Debug.LogError("maxHealth debe ser mayor que cero", this);
            return;
        }

        if (healthImage == null)
        {
            Debug.LogError("healthImage no está asignada en el Inspector", this);
            return;
        }

        if (healthSprites == null || healthSprites.Length == 0)
        {
            Debug.LogError("No se cargaron los sprites", this);
            return;
        }

        int index = Mathf.RoundToInt(((float)currentHealth / maxHealth) * (healthSprites.Length - 1));
        
        // Volver a invertir porque el frame 0 es lleno y el frame 30 es vacío
        index = (healthSprites.Length - 1) - index;
        
        healthImage.sprite = healthSprites[index];
        
        // FORZAR VISIBILIDAD:
        healthImage.enabled = true;
        healthImage.color = Color.white;
    }

    // ── NUEVO: Reinicio para siguientes rondas ──
    public void ResetPlayer()
    {
        isDead = false;
        isHit = false;
        currentHealth = maxHealth;
        transform.position = startPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        UpdateHealthUI();

        if (anim != null)
        {
            if (TieneParametro("isDead"))
                anim.SetBool("isDead", false);
            
            anim.Play("Idle", -1, 0f);
        }

        if (movement != null)
        {
            movement.enabled = true;
        }
    }
}