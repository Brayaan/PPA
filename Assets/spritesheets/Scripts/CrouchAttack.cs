using UnityEngine;
using System.Collections;

public class CrouchAttack : MonoBehaviour
{
    public Animator animator;

    private bool estaAgachado;
    private bool atacando = false;

    void Update()
    {
        // Detectar si está agachado
        estaAgachado = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // Actualizar Animator
        animator.SetBool("isCrouching", estaAgachado);

        // Ataque agachado con Q
        if (estaAgachado && Input.GetKeyDown(KeyCode.Q) && !atacando)
        {
            StartCoroutine(RealizarAtaque());
        }
    }

    IEnumerator RealizarAtaque()
    {
        atacando = true;

        animator.SetTrigger("CrouchAttack");

        // Duración de la animación (ajústala)
        yield return new WaitForSeconds(0.5f);

        atacando = false;
    }
}