using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScoreInfo : MonoBehaviour
{
    [SerializeField] private Image playerSprite;
    [SerializeField] private TextMeshProUGUI playerCoinText;

    public void UpdatePlayerImage(Sprite newSprite)
    {
        playerSprite.sprite = newSprite;
    }

    public void UpdatePlayerScore(int newScore)
    {
        playerCoinText.text = newScore.ToString();
    }
}
