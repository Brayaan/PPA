using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    public Image healthImage;

    private Sprite[] healthSprites;

    public float knockbackForce = 5f;
    // Tiempo mínimo de hit-stun entre golpes consecutivos
    public float knockbackDuration = 0.05f;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private Animator anim;

    // Bloquea golpes entrantes durante el hit-stun activo
    private bool isHit = false;

    void Start()
    {
        // Cargar spritesheet de vida desde la carpeta Resources
        healthSprites = Resources.LoadAll<Sprite>("HeartCounter/heart_counter-Sheet");

        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        anim = GetComponent<Animator>();

        UpdateHealthUI();
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        currentHealth -= damage;

        // Clampear vida para no bajar de cero
        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log("Vida actual: " + currentHealth);

        UpdateHealthUI();

        ApplyHit(attackerPosition);
    }

    void ApplyHit(Vector2 attackerPosition)
    {
        // Descartar golpe si el hit-stun ya está activo
        if (isHit) return;

        isHit = true;

        // Deshabilitar input del jugador durante el hit-stun
        if (movement != null)
            movement.enabled = false;

        // Activar animación de recibir golpe si existe el parámetro
        if (anim != null && TieneParametro("Hit"))
            anim.SetTrigger("Hit");

        // Calcular dirección del empuje desde la posición del atacante
        Vector2 direction = (transform.position - (Vector3)attackerPosition).normalized;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(direction.x * knockbackForce, 2f), ForceMode2D.Impulse);
        }

        // Mathf.Max evita que duration negativo colapse el cooldown
        Invoke(nameof(EndHit), Mathf.Max(0f, knockbackDuration));
    }

    // Verificar si el Animator tiene un parámetro registrado por nombre
    bool TieneParametro(string nombre)
    {
        foreach (var param in anim.parameters)
            if (param.name == nombre) return true;
        return false;
    }

    void EndHit()
    {
        isHit = false;

        // Restaurar input del jugador al salir del hit-stun
        if (movement != null)
            movement.enabled = true;
    }

    void UpdateHealthUI()
    {
        // Guard: evitar división por cero si maxHealth es inválido
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

        // Calcular índice e invertir orden para que 0 = vida llena
        int index = Mathf.RoundToInt(((float)currentHealth / maxHealth) * (healthSprites.Length - 1));

        index = (healthSprites.Length - 1) - index;

        healthImage.sprite = healthSprites[index];
    }
}