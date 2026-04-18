using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator anim;

    public PlayerMovement playerMovement;

    public GameObject punchHitbox;
    public GameObject kickHitbox;

    public KeyCode punchKey = KeyCode.J;
    public KeyCode kickKey = KeyCode.K;

    private bool kickReady = true;
    public float kickCooldown = 0.5f;

    private bool punchReady = true;
    public float punchCooldown = 0.3f;

    private bool isAttacking = false;
    // Tiempo que el hitbox permanece activo por ataque
    private float attackDuration = 0.3f;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Asegurar que los hitboxes están desactivados al iniciar
        if (punchHitbox != null)
            punchHitbox.SetActive(false);

        if (kickHitbox != null)
            kickHitbox.SetActive(false);
    }

    void Update()
    {
        // Puñetazo: verificar input, cooldown y estado de ataque
        if (Input.GetKeyDown(punchKey) && !isAttacking && punchReady)
        {
            StartAttack();
            anim.SetTrigger("punch");
            ActivarHitbox();
            Invoke(nameof(DesactivarHitbox), attackDuration);

            punchReady = false;
            Invoke(nameof(ResetPunch), punchCooldown);
        }

        // Patada: verificar cooldown, input y estado de ataque
        if (Input.GetKeyDown(kickKey) && kickReady && !isAttacking)
        {
            StartAttack();
            anim.SetTrigger("kick");
            ActivarKickHitbox();
            Invoke(nameof(DesactivarKickHitbox), attackDuration);

            kickReady = false;
            Invoke(nameof(ResetKick), kickCooldown);
        }
    }

    void StartAttack()
    {
        isAttacking = true;

        // Sincronizar flag de ataque con PlayerMovement
        if (playerMovement != null)
        {
            playerMovement.isAttacking = true;
        }

        Invoke(nameof(EndAttack), attackDuration);
    }

    void EndAttack()
    {
        isAttacking = false;

        // Limpiar flag de ataque en PlayerMovement también
        if (playerMovement != null)
        {
            playerMovement.isAttacking = false;
        }
    }

    // Exponer estado para que Hitbox pueda consultarlo
    public bool IsAttacking()
    {
        return isAttacking;
    }

    public void ActivarHitbox()
    {
        if (punchHitbox != null)
            punchHitbox.SetActive(true);
    }

    public void DesactivarHitbox()
    {
        if (punchHitbox != null)
            punchHitbox.SetActive(false);
    }

    public void ActivarKickHitbox()
    {
        if (kickHitbox != null)
            kickHitbox.SetActive(true);
    }

    public void DesactivarKickHitbox()
    {
        if (kickHitbox != null)
            kickHitbox.SetActive(false);
    }

    void ResetKick()
    {
        kickReady = true;
    }

    void ResetPunch()
    {
        punchReady = true;
    }
}