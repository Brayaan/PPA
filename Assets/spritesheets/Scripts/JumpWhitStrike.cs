using UnityEngine;
using System.Collections;

public class JumpWhitStrike : MonoBehaviour
{
    public Rigidbody2D rb;

    public float fuerzaSalto = 8f;
    public float impulsoHorizontal = 5f;

    private bool ataqueUsado = false;
    private bool enSuelo = true;

    void Update()
    {
        // Ataque aéreo (tecla J)
        if (Input.GetKeyDown(KeyCode.J) && !enSuelo && !ataqueUsado)
        {
            StartCoroutine(GolpeConSalto());
        }
    }

    IEnumerator GolpeConSalto()
    {
        ataqueUsado = true;

        // Impulso hacia adelante
        rb.AddForce(Vector2.right * impulsoHorizontal, ForceMode2D.Impulse);

        // Activar animación de ataque aquí
        // animator.SetTrigger("Ataque");

        yield return new WaitForSeconds(0.3f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            enSuelo = true;
            ataqueUsado = false; // Reinicia el ataque al aterrizar
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            enSuelo = false;
        }
    }
}
