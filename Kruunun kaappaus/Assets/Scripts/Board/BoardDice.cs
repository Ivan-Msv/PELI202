using System.Collections.Generic;
using UnityEngine;

public abstract class BoardDice : MonoBehaviour
{
    public int[] availableNumbers;
    public abstract int RollDiceNumber();
    public abstract void DiceAnimation();
}
