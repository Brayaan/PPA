using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    // Instancia global accesible desde cualquier script
    public static CombatManager Instance { get; private set; }

    [Header("Jugadores")]
    public HealthSystem player1Health;
    public HealthSystem player2Health;

    [Header("Nombres en pantalla")]
    public string player1Name = "Jugador 1";
    public string player2Name = "Jugador 2";

    [Header("Componentes a desactivar al terminar/iniciar round")]
    public PlayerMovement player1Movement;
    public PlayerMovement player2Movement;
    public PlayerAttack player1Attack;
    public PlayerAttack player2Attack;
    public PlayerSpecialAttack player1Special;
    public PlayerSpecialAttack player2Special;

    [Header("UI de resultado final")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("Sistema de Rondas (NUEVO)")]
    public TMP_Text scoreText; // Asignar el texto en el inspector para el marcador
    public int roundsToWin = 2; // Mejor de 3 rounds
    public float timeBetweenRounds = 2f;

    private int player1Wins = 0;
    private int player2Wins = 0;
    private bool roundActive = false;
    private bool isFirstRound = true;

    private bool combatEnded = false;
    public bool isCombatEnded => combatEnded;
    public bool isRoundActive => roundActive;

    void Awake()
    {
        // Singleton: solo existe una instancia a la vez
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    IEnumerator Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
            
        // Esperar 1 frame para que HealthSystem y EnergySystem ejecuten sus Start()
        yield return null;

        StartMatch();
    }

    public void StartMatch()
    {
        player1Wins = 0;
        player2Wins = 0;
        UpdateScoreUI();
        StartCoroutine(StartNewRoundCoroutine());
    }

    private IEnumerator StartNewRoundCoroutine()
    {
        roundActive = false;
        combatEnded = false;

        // Ocultar mensaje de victoria al iniciar un nuevo round
        if (resultPanel != null) resultPanel.SetActive(false);

        // Tarea: Bloquear inputs antes de iniciar
        DisableAllControls();

        // CA-02: Reiniciar posiciones y vida (100%)
        if (player1Health != null) player1Health.ResetPlayer();
        if (player2Health != null) player2Health.ResetPlayer();

        // Desactivar controles nuevamente por seguridad (si ResetPlayer activó movement)
        DisableAllControls();

        // Calcular el tiempo que deben estar bloqueados dependiendo del audio
        float currentWaitTime = timeBetweenRounds;
        if (isFirstRound && AudioManager.Instance != null && AudioManager.Instance.matchStartJingle != null)
        {
            // En el primer round, esperar a que termine el jingle completo
            currentWaitTime = Mathf.Max(timeBetweenRounds, AudioManager.Instance.matchStartJingle.length);
            isFirstRound = false;
        }
        else if (AudioManager.Instance != null && AudioManager.Instance.roundStartSound != null)
        {
            // En los siguientes rounds, esperar a que termine el sonido de "Fight!"
            currentWaitTime = Mathf.Max(timeBetweenRounds, AudioManager.Instance.roundStartSound.length);
        }

        // Esperar antes de iniciar el combate bloqueando todo
        yield return new WaitForSeconds(currentWaitTime);

        // Desbloquear inputs y comenzar
        EnableAllControls();
        roundActive = true;
    }

    // Llamado cuando un jugador muere
    public void NotifyPlayerDeath(HealthSystem deadPlayer)
    {
        if (combatEnded) return; // Evitar que se ejecute ms de una vez
        
        // Reproducir sonido de muerte
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeathSound();

        // CA-04: Esperamos al final del frame por si ambos mueren simultáneamente
        StartCoroutine(HandleRoundEndCoroutine());
    }

    private IEnumerator HandleRoundEndCoroutine()
    {
        roundActive = false;
        combatEnded = true; 
        
        // Tarea: Bloquear inputs al terminar cada round
        DisableAllControls();

        // Esperar al final del frame para confirmar empates
        yield return new WaitForEndOfFrame();

        if (player1Health == null || player2Health == null)
        {
            Debug.LogError("ERROR: player1Health o player2Health no están asignados en el CombatManager.");
            yield break; // Detiene la corrutina para evitar el error
        }

        bool p1Dead = player1Health.currentHealth <= 0;
        bool p2Dead = player2Health.currentHealth <= 0;

        // CA-04: Empate simultáneo
        if (p1Dead && p2Dead)
        {
            player1Wins++;
            player2Wins++;
            ShowRoundResult("¡Empate Simultáneo!");
        }
        // CA-01: Gana P2
        else if (p1Dead)
        {
            player2Wins++;
            ShowRoundResult($"¡{player2Name} Gana el Round!");
        }
        // CA-01: Gana P1
        else if (p2Dead)
        {
            player1Wins++;
            ShowRoundResult($"¡{player1Name} Gana el Round!");
        }

        UpdateScoreUI();

        // Pausa para que se reproduzca la animación de muerte
        yield return new WaitForSeconds(2f);

        // CA-03: Evaluar si alguien alcanzó 2 victorias
        if (player1Wins >= roundsToWin || player2Wins >= roundsToWin)
        {
            EndMatch();
        }
        else
        {
            // Iniciar siguiente round
            StartCoroutine(StartNewRoundCoroutine());
        }
    }

    private void ShowRoundResult(string text)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = text;
    }

    private void EndMatch()
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        // Mostrar pantalla de resultado final
        if (player1Wins >= roundsToWin && player2Wins >= roundsToWin)
        {
            if (resultText != null) resultText.text = "¡EMPATE ÉPICO!";
        }
        else if (player1Wins >= roundsToWin)
        {
            if (resultText != null) resultText.text = $"¡{player1Name} GANA LA PARTIDA!";
        }
        else if (player2Wins >= roundsToWin)
        {
            if (resultText != null) resultText.text = $"¡{player2Name} GANA LA PARTIDA!";
        }
    }

    public void RestartMatch()
    {
        // Reiniciar contadores y UI
        player1Wins = 0;
        player2Wins = 0;
        isFirstRound = true; // Reiniciar estado del primer round
        UpdateScoreUI();
        
        if (resultPanel != null) resultPanel.SetActive(false);
        if (resultText != null) resultText.text = "";
        
        combatEnded = false;
        
        // CA-03: Reiniciar combate desde el round 1 con vida completa
        if (player1Health != null) player1Health.ResetPlayer();
        if (player2Health != null) player2Health.ResetPlayer();
        
        StartCoroutine(StartNewRoundCoroutine());
    }

    // Por compatibilidad con código antiguo
    public void EndCombat(string loserName)
    {
        // ...
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            // Formato profesional tipo e-Sports/Arcade:
            // Nombres en color, números gigantes en dorado, y un pequeño 'VS' en el centro.
            scoreText.text = $"<color=#FF4444>{player1Name.ToUpper()}</color>  <size=150%><color=#FFD700>{player1Wins}</color></size>  <color=#FFFFFF><size=60%>V S</size></color>  <size=150%><color=#FFD700>{player2Wins}</color></size>  <color=#4444FF>{player2Name.ToUpper()}</color>";
        }
    }

    void DisableAllControls()
    {
        if (player1Movement != null) player1Movement.enabled = false;
        if (player2Movement != null) player2Movement.enabled = false;
        if (player1Attack   != null) player1Attack.enabled   = false;
        if (player2Attack   != null) player2Attack.enabled   = false;
        if (player1Special  != null) player1Special.enabled  = false;
        if (player2Special  != null) player2Special.enabled  = false;
    }

    void EnableAllControls()
    {
        if (player1Movement != null) player1Movement.enabled = true;
        if (player2Movement != null) player2Movement.enabled = true;
        if (player1Attack   != null) player1Attack.enabled   = true;
        if (player2Attack   != null) player2Attack.enabled   = true;
        if (player1Special  != null) player1Special.enabled  = true;
        if (player2Special  != null) player2Special.enabled  = true;
    }
}