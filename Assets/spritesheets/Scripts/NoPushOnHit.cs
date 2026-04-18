using UnityEngine;

public class NoPushOnHit : MonoBehaviour
{
    private Rigidbody2D rb;
    private float originalMass;
    private bool isHit = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Guardar masa original para restaurarla al terminar el golpe
            originalMass = rb.mass;
        }
    }

    // Llamar al inicio del hit-stun para resistir empujes físicos
    public void OnHitStart()
    {
        if (rb != null)
        {
            // Masa extrema para evitar ser desplazado por física de contacto
            rb.mass = 1000f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        isHit = true;
    }

    // Llamar al terminar el hit-stun para restaurar el estado normal
    public void OnHitEnd()
    {
        if (rb != null)
        {
            // Restaurar masa original al salir del hit-stun
            rb.mass = originalMass;
        }
        isHit = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Durante el hit-stun ignorar colisiones con enemigos
        if (isHit && (collision.gameObject.CompareTag("Enemy")))
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider, true);
        }
    }
}
