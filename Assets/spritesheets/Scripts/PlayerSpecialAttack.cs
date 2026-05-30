using System.Collections;
using UnityEngine;

[System.Serializable]
public class SpecialAttackData
{
    public string attackName;
    public KeyCode key;
    public int energyCost;
    public int damage;
    public float duration;
    public string animTrigger;
    public GameObject hitbox;
    
    [Tooltip("Tiempo que tarda en activarse el golpe después de presionar la tecla (para sincronizar con la animación)")]
    public float hitDelay = 0.15f;

    [Space]
    public bool isHealing;
    [Range(0f, 1f)]
    public float healPercent;
}

public class PlayerSpecialAttack : MonoBehaviour
{
    [Header("Referencias")]
    public EnergySystem energySystem;
    public PlayerAttack playerAttack;
    public PlayerMovement playerMovement;
    public HealthSystem playerHealth;
    private Animator anim;

    [Header("UI — Barra de Poderes")]
    public GameObject powerBarUI;

    [Header("Poderes Especiales (configurar los 4 en el Inspector)")]
    public SpecialAttackData[] specialAttacks = new SpecialAttackData[4];

    private bool isUsingSpecial = false;
    private bool wasEnergyFull  = false;
    private SpecialAttackData pendingSpecial = null;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (powerBarUI != null)
            powerBarUI.SetActive(false);

        foreach (SpecialAttackData sa in specialAttacks)
            if (sa.hitbox != null)
                sa.hitbox.SetActive(false);
    }

    void Update()
    {
        bool energyFull = energySystem != null && energySystem.IsFull();

        if (energyFull != wasEnergyFull)
        {
            wasEnergyFull = energyFull;
            if (powerBarUI != null)
                powerBarUI.SetActive(energyFull);
        }

        if (isUsingSpecial) return;

        for (int i = 0; i < specialAttacks.Length; i++)
        {
            SpecialAttackData sa = specialAttacks[i];

            // Ignorar ataques no configurados (sin nombre o sin tecla asignada)
            if (string.IsNullOrEmpty(sa.attackName) || sa.key == KeyCode.None)
                continue;

            if (Input.GetKeyDown(sa.key))
            {
                if (energySystem == null || energySystem.currentEnergy < sa.energyCost)
                {
                    Debug.Log($"[Especial] Energía insuficiente para {sa.attackName}. Necesitás {sa.energyCost}.");
                    break;
                }

                pendingSpecial = sa;
                break;
            }
        }

        if (pendingSpecial != null && (playerAttack == null || !playerAttack.IsAttacking()))
        {
            ExecuteSpecialAttack(pendingSpecial);
            pendingSpecial = null;
        }
    }

    void ExecuteSpecialAttack(SpecialAttackData sa)
    {
        isUsingSpecial = true;
        Debug.Log($"[Especial] Ejecutando: {sa.attackName}");

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (anim != null && !string.IsNullOrEmpty(sa.animTrigger))
            anim.SetTrigger(sa.animTrigger);

        if (sa.isHealing)
        {
            if (playerHealth != null)
            {
                int amount = Mathf.RoundToInt(playerHealth.maxHealth * sa.healPercent);
                StartCoroutine(DelayedHeal(amount, sa.hitDelay));
            }
            else
            {
                Debug.LogWarning("[Healing] playerHealth no está asignado en el Inspector.");
            }
        }
        else if (sa.hitbox != null)
        {
            SpecialHitbox sh = sa.hitbox.GetComponent<SpecialHitbox>();
            if (sh == null)
            {
                sh = sa.hitbox.AddComponent<SpecialHitbox>();
            }
            
            sh.damage = sa.damage;

            StartCoroutine(DelayedHitbox(sa.hitbox, sa.hitDelay));
        }

        // Retrasar el consumo de energía para sincronizarlo con la animación y el golpe
        StartCoroutine(DelayedConsumeEnergy(sa.energyCost, sa.hitDelay));

        StartCoroutine(EndSpecialAfterDuration(sa));
    }

    IEnumerator DelayedConsumeEnergy(int amount, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (energySystem != null)
            energySystem.ConsumeEnergy(amount);
    }

    IEnumerator DelayedHitbox(GameObject hitbox, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hitbox == null)
        {
            Debug.LogError("¡El hitbox es nulo! Revisa el Inspector.");
            yield break;
        }

        Debug.Log("Activando hitbox: " + hitbox.name + " en " + gameObject.name);

        Collider2D col = hitbox.GetComponent<Collider2D>();
        if (col == null)
            Debug.LogWarning($"⚠️ El hitbox {hitbox.name} NO tiene un Collider2D. ¡No podrá detectar golpes!");
        else
        {
            if (!col.isTrigger) Debug.LogWarning($"⚠️ El Collider de {hitbox.name} no tiene marcado 'Is Trigger'. Deberías marcarlo.");
            col.enabled = true; // <-- Asegurar que el collider esté encendido
        }

        SpecialHitbox sh = hitbox.GetComponent<SpecialHitbox>();
        if (sh == null)
        {
            Debug.LogWarning($"El objeto {hitbox.name} no tenía el script SpecialHitbox. Se lo agregué automáticamente.");
            sh = hitbox.AddComponent<SpecialHitbox>();
        }
        sh.enabled = true; // <-- Asegurar que el script esté encendido
        
        sh.owner = transform.root.gameObject; 

        hitbox.SetActive(true);
    }

    IEnumerator DelayedHeal(int amount, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerHealth != null)
            playerHealth.Heal(amount);
    }

    IEnumerator EndSpecialAfterDuration(SpecialAttackData sa)
    {
        // Si la duración es menor que el retraso del golpe (0.15f), el hitbox se apagará antes de encenderse o instantáneamente.
        // Forzamos un mínimo de tiempo para que la física tenga tiempo de reaccionar.
        float actualDuration = Mathf.Max(sa.duration, 0.25f); 

        yield return new WaitForSeconds(actualDuration);

        if (sa.hitbox != null)
        {
            Debug.Log($"Apagando hitbox: {sa.hitbox.name} en {gameObject.name} después de {actualDuration} segundos.");
            sa.hitbox.SetActive(false);
        }

        if (playerMovement != null)
            playerMovement.enabled = true;

        isUsingSpecial = false;
    }

    public bool IsUsingSpecial() => isUsingSpecial;
}