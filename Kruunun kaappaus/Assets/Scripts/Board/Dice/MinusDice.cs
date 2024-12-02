using UnityEngine;

public class MinusDice : BoardDice
{
    public override int RollDiceNumber()
    {
        return availableNumbers[Random.Range(0, availableNumbers.Length)];
    }

    public override string DiceAnimationString(int rolledNumber)
    {
        // Change later as well
        return $"default_dice_1";
    }
}
