using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : ScriptableObject
{
    public int MoneyAmount { get; private set; }
    public int CrownAmount { get; private set; }
    public GameObject[] EquippedDice { get; private set; }

    public void AddMoney(int amount) // - jos haluu poistaa rahat
    {
        MoneyAmount += amount;
    }

    public void AddCrown(int amount) // sama täs
    {
        CrownAmount += amount;
    }
}
