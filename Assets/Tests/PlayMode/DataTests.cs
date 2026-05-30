using NUnit.Framework;
using UnityEngine;

public class DataTests
{
    [Test]
    public void CharacterData_CanBeCreated_AndHoldsValues()
    {
        // Al ser ScriptableObject, usamos CreateInstance
        CharacterData characterData = ScriptableObject.CreateInstance<CharacterData>();
        
        characterData.characterName = "Fighter1";
        characterData.description = "Test Description";
        characterData.animationSpeed = 0.5f;

        Assert.IsNotNull(characterData);
        Assert.AreEqual("Fighter1", characterData.characterName);
        Assert.AreEqual("Test Description", characterData.description);
        Assert.AreEqual(0.5f, characterData.animationSpeed);

        // Limpiar memoria
        Object.DestroyImmediate(characterData);
    }

    [Test]
    public void GameData_CanStoreCharactersStatically()
    {
        CharacterData p1 = ScriptableObject.CreateInstance<CharacterData>();
        p1.characterName = "Player One";

        CharacterData p2 = ScriptableObject.CreateInstance<CharacterData>();
        p2.characterName = "Player Two";

        GameData.player1Character = p1;
        GameData.player2Character = p2;

        Assert.AreEqual("Player One", GameData.player1Character.characterName);
        Assert.AreEqual("Player Two", GameData.player2Character.characterName);

        // Limpieza estática y memoria
        GameData.player1Character = null;
        GameData.player2Character = null;
        Object.DestroyImmediate(p1);
        Object.DestroyImmediate(p2);
    }
}
