using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Juego Pelea/Personaje")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    
    [Header("Animación (Estilo GIF)")]
    public Sprite[] idleAnimation; // Varios PNGs para hacer la animación
    public float animationSpeed = 0.1f; // Velocidad (ej: 0.1 segundos por frame)
    
    [TextArea(3, 5)]
    public string description = "Descripción del personaje aquí...";
    
    // Prefab del jugador para instanciar en combate (opcional para el futuro)
    public GameObject characterPrefab;
}
