using UnityEngine;
using UnityEngine.UI;

public class EnergySystem : MonoBehaviour
{
    public int maxEnergy = 100;
    public int currentEnergy;

    public Image energyImage;
    private Sprite[] energySprites;

    void Start()
    {
        // Cargar spritesheet de energía desde la carpeta Resources
        energySprites = Resources.LoadAll<Sprite>("EnergyBar/emerald_counter-Sheet");

        currentEnergy = 0;
        UpdateEnergyUI();
    }

    // Ganancia de energía al recibir un golpe del oponente
    public void GainEnergyFromDamage()
    {
        currentEnergy += 2;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
        UpdateEnergyUI();
    }

    // Ganancia de energía según el tipo de ataque conectado
    public void GainEnergyFromAttack(string attackName)
    {
        int gain = 0;
        if (attackName == "Puñetazo") gain = 5;
        else if (attackName == "Patada") gain = 10;

        currentEnergy += gain;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        UpdateEnergyUI();
    }

    // Ganancia de energía al activar el bloqueo
    public void GainEnergyFromBlock()
    {
        currentEnergy += 3;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        UpdateEnergyUI();
    }

    void UpdateEnergyUI()
    {
        // Guard: evitar división por cero si maxEnergy es inválido
        if (maxEnergy <= 0)
        {
            Debug.LogError("maxEnergy debe ser mayor que cero", this);
            return;
        }

        if (energyImage == null)
        {
            Debug.LogError("energyImage no está asignada en el Inspector", this);
            return;
        }

        if (energySprites == null || energySprites.Length == 0)
        {
            Debug.LogError("No se cargaron los sprites de energía", this);
            return;
        }

        // Calcular índice e invertir: sprite 0 = lleno
        int index = Mathf.RoundToInt(((float)currentEnergy / maxEnergy) * (energySprites.Length - 1));
        index = Mathf.Clamp(index, 0, energySprites.Length - 1);
        index = (energySprites.Length - 1) - index;
        energyImage.sprite = energySprites[index];
    }

    public bool IsFull()
    {
        return currentEnergy >= maxEnergy;
    }

    public void ConsumeEnergy(int amount)
    {
        currentEnergy -= amount;
        if (currentEnergy < 0) currentEnergy = 0;
        UpdateEnergyUI();
    }
}