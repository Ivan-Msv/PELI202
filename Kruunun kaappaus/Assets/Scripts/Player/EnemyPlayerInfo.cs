using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyPlayerInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;
    [SerializeField] private TextMeshProUGUI playerCoins;
    [SerializeField] private TextMeshProUGUI playerCrowns;
    [SerializeField] private Image specialDiceImage;

    public void UpdatePlayerInfo(string name, int coins, int crowns, Sprite specialDiceSprite)
    {
        playerName.text = name;
        playerCoins.text = coins.ToString();
        playerCrowns.text = crowns.ToString();
        specialDiceImage.sprite = specialDiceSprite;
    }
}
