using TMPro;
using UnityEngine;

public class EnemyPlayerInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI playerCoins;
    [SerializeField] private TextMeshProUGUI playerCrowns;

    public void UpdatePlayerInfo(string name, int coins, int crowns)
    {
        playerName.text = name;
        playerCoins.text = coins.ToString();
        playerCrowns.text = crowns.ToString();
    }
}
