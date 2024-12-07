using System.Collections;
using UnityEngine;

public abstract class BoardDice : ScriptableObject
{
    [Tooltip("Dice has animations numbers, remove the number (EX: default_dice_")]
    public string animationStringPrefix;
    public int networkIndex;
    public Sprite image;
    public int[] availableNumbers;
    public int RollDiceNumber()
    {
        return availableNumbers[Random.Range(0, availableNumbers.Length)];
    }

    public string DiceAnimationString(int rolledNumber)
    {
        return $"{animationStringPrefix}{Mathf.Abs(rolledNumber)}";
    }
    public abstract void SpecialAbility();
}
