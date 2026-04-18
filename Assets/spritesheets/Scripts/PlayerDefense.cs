using UnityEngine;

public class PlayerDefense : MonoBehaviour
{
    private Animator anim;

    public GameObject blockHitbox;

    private bool isBlocking = false;

    public KeyCode blockKey = KeyCode.L;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Desactivar hitbox de bloqueo al iniciar la escena
        if (blockHitbox != null)
            blockHitbox.SetActive(false);
    }

    void Update()
    {
        bool input = Input.GetKey(blockKey);

        // Solo ejecutar lógica cuando el estado de bloqueo cambia
        if (input != isBlocking)
        {
            isBlocking = input;

            // Otorgar energía únicamente al inicio del bloqueo
            if (isBlocking)
            {
                EnergySystem energy = GetComponent<EnergySystem>();
                if (energy != null)
                    energy.GainEnergyFromBlock();
            }

            // Sincronizar animación con el estado actual de bloqueo
            if (anim != null)
                anim.SetBool("isBlocking", isBlocking);

            // Activar o desactivar el hitbox de bloqueo según estado
            if (blockHitbox != null)
                blockHitbox.SetActive(isBlocking);
        }
    }

    // Exponer estado para que Hitbox pueda consultarlo
    public bool IsBlocking()
    {
        return isBlocking;
    }
}