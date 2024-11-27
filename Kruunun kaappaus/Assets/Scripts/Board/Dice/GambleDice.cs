using UnityEngine;

public class GambleDice : BoardDice
{
    public override int RollDiceNumber()
    {
        Debug.Log("Gamble Dice Rolled");
        Debug.Log(availableNumbers[Random.Range(0, availableNumbers.Length)]);
        return 0;
    }

    public override void DiceAnimation()
    {
        throw new System.NotImplementedException();
    }
}
