using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 3f;
    public float jumpForce = 10f;

    [Header("Knockback Settings")]
    public bool canBeKnockedBack = true;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;
    // Bloquea Update y FixedUpdate mientras el personaje vuela
    private bool isKnockedBack = false;

    public float wallCheckDistance = 0.2f;
    public float crouchSpeed = 2f;

    [SerializeField] private Rigidbody2D rb;
    public Transform groundCheck;
    public Transform ceilingCheck;
    public LayerMask groundLayer;
    // Debe incluir las capas Player y Duplicate
    public LayerMask characterLayer;
    public Transform wallCheck;

    public BoxCollider2D boxCollider;
    public Animator animator;

    public bool isCrouching;
    public bool facingRight = true;
    public bool isAttacking;

    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.S;

    private float moveInput;
    private bool isGrounded;
    private bool isBlocked;
    private bool isTouchingWall;

    private Vector2 originalSize;
    private Vector2 crouchSize;

    private Vector2 originalOffset;
    private Vector2 crouchOffset;
    private Vector3 originalScale;

    void Start()
    {
        // Guard: boxCollider es necesario antes de leer su tamaño
        if (boxCollider == null)
        {
            Debug.LogError("boxCollider no está asignado en el Inspector de " + gameObject.name, this);
            enabled = false;
            return;
        }

        // Guardar dimensiones para restaurar al salir del agachado
        originalSize = boxCollider.size;
        originalOffset = boxCollider.offset;
        originalScale = transform.localScale;

        crouchSize = new Vector2(originalSize.x, originalSize.y / 2);
        crouchOffset = new Vector2(originalOffset.x, originalOffset.y - (originalSize.y / 4));

        // Obtener Rigidbody2D si no fue asignado en el Inspector
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Bloquear todo input durante el vuelo del knockback
        if (isKnockedBack) return;

        // Guard: verificar referencias críticas antes de usar en Update
        if (wallCheck == null || groundCheck == null || ceilingCheck == null || boxCollider == null || animator == null)
        {
            Debug.LogError("Faltan referencias requeridas en el Inspector de " + gameObject.name, this);
            return;
        }

        float input = 0;

        if (Input.GetKey(leftKey))
        {
            input = -1;
        }
        else if (Input.GetKey(rightKey))
        {
            input = 1;
        }

        // Detectar pared lateral con raycast en la dirección del input
        if (input != 0)
        {
            isTouchingWall = Physics2D.Raycast(wallCheck.position, Vector2.right * Mathf.Sign(input), wallCheckDistance, groundLayer);
        }
        else
        {
            isTouchingWall = false;
        }

        // Determinar velocidad según pared, agachado o movimiento libre
        if (isTouchingWall)
        {
            moveInput = 0;
        }
        else if (isCrouching)
        {
            // Guard: evitar división por cero si speed es inválido
            moveInput = speed > 0 ? input * (crouchSpeed / speed) : 0;
        }
        else
        {
            moveInput = input;
        }

        animator.SetFloat("Speed", Mathf.Abs(moveInput));

        // Voltear el sprite si cambia la dirección horizontal
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }

        // Detectar si el personaje toca el suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        // Detectar techo para bloquear levantarse del agachado
        isBlocked = Physics2D.OverlapCircle(ceilingCheck.position, 0.2f, groundLayer);

        if (Input.GetKeyDown(jumpKey) && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        animator.SetBool("isJumping", !isGrounded);

        if (Input.GetKey(crouchKey))
        {
            isCrouching = true;
        }
        else if (!isBlocked)
        {
            isCrouching = false;
        }

        animator.SetBool("isCrouching", isCrouching);

        // Redimensionar el collider físico al agacharse y al levantarse
        if (isCrouching)
        {
            boxCollider.size = crouchSize;
            boxCollider.offset = crouchOffset;
        }
        else
        {
            boxCollider.size = originalSize;
            boxCollider.offset = originalOffset;
        }
    }

    void FixedUpdate()
    {
        if (isKnockedBack) return;

        // wallCheck es requerido antes de ejecutar el raycast
        if (wallCheck == null) return;

        float move = moveInput;

        if (moveInput != 0)
        {
            // RaycastAll filtra colliders propios por raíz de jerarquía
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                wallCheck.position,
                Vector2.right * Mathf.Sign(moveInput),
                0.1f,
                characterLayer);

            foreach (RaycastHit2D hit in hits)
            {
                // Saltar colliders que pertenecen a esta misma jerarquía
                if (hit.collider.transform.root == transform.root) continue;

                bool hitsCharacter = ((1 << hit.collider.gameObject.layer) & characterLayer) != 0;

                if (hitsCharacter)
                {
                    // Detener este personaje al tocar al oponente
                    move = 0;
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                    // No zerear velocidad del oponente si está en knockback
                    Rigidbody2D otherRb = hit.collider.transform.root.GetComponent<Rigidbody2D>();
                    if (otherRb != null)
                    {
                        PlayerMovement otherMovement = hit.collider.transform.root.GetComponent<PlayerMovement>();
                        if (otherMovement == null || !otherMovement.isKnockedBack)
                        {
                            otherRb.linearVelocity = new Vector2(0, otherRb.linearVelocity.y);
                        }
                    }

                    break;
                }
            }
        }

        rb.linearVelocity = new Vector2(move * speed, rb.linearVelocity.y);
    }

    void Flip()
    {
        facingRight = !facingRight;

        // Invertir escala horizontal para voltear el sprite
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void ApplyKnockback(Vector2 attackerPosition)
    {
        if (!canBeKnockedBack || isKnockedBack) return;

        isKnockedBack = true;

        // Calcular dirección del vuelo opuesta al atacante
        Vector2 direction = (transform.position - (Vector3)attackerPosition).normalized;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(direction.x * knockbackForce, 2f), ForceMode2D.Impulse);
        }

        // Deshabilitar script para que Update no interfiera con el vuelo
        this.enabled = false;

        // Mathf.Max evita que duration negativo colapse el knockback
        Invoke(nameof(EndKnockback), Mathf.Max(0f, knockbackDuration));
    }

    private void EndKnockback()
    {
        isKnockedBack = false;
        // Restaurar control del jugador al terminar el knockback
        this.enabled = true;

        // Eliminar velocidad horizontal residual al aterrizar
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
}