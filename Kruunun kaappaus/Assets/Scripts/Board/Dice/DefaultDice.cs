using UnityEngine;

public class DefaultDice : BoardDice
{
    public override int RollDiceNumber()
    {
        Debug.Log("Default Dice Rolled");
        Debug.Log(availableNumbers[Random.Range(0, availableNumbers.Length)]);
        // play animation here
        return 0;
    }

    public override void DiceAnimation()
    {
        throw new System.NotImplementedException();
    }
}
