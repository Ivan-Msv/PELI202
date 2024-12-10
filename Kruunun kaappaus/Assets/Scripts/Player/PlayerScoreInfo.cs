using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScoreInfo : MonoBehaviour
{
    [SerializeField] private Image playerSprite;
    [SerializeField] private TextMeshProUGUI playerCoinText;

    public void UpdatePlayerImage(Sprite newImage)
    {
        playerSprite.sprite = newImage;
    }

    public void UpdatePlayerScore(int newScore)
    {
        playerCoinText.text = newScore.ToString();
    }
}
