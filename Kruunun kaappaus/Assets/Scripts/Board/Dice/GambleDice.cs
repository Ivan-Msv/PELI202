using UnityEngine;

public class GambleDice : BoardDice
{
    public override int RollDiceNumber()
    {
        return availableNumbers[Random.Range(0, availableNumbers.Length)];
    }

    public override string DiceAnimationString(int rolledNumber)
    {
        // Change to different animation later
        return $"default_dice_1";
    }
}
