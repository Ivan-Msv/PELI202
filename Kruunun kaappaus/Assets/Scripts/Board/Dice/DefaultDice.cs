using UnityEngine;

public class DefaultDice : BoardDice
{
    public override int RollDiceNumber()
    {
        Debug.Log("Default Dice Rolled");
        return availableNumbers[Random.Range(0, availableNumbers.Length)];
    }

    public override string DiceAnimationString(int rolledNumber)
    {
        return $"default_dice_{rolledNumber}";
    }
}
