using System.Collections;
using UnityEngine;

public abstract class BoardDice : MonoBehaviour
{
    public int[] availableNumbers;
    public abstract int RollDiceNumber();
    public abstract string DiceAnimationString(int rolledNumber);
}
