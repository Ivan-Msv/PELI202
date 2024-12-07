using UnityEngine;

[CreateAssetMenu(menuName = "Dice/DefaultDice")]
public class DefaultDice : BoardDice
{
    public override void SpecialFunction()
    {
        Debug.Log("No Special Function");
    }
}
