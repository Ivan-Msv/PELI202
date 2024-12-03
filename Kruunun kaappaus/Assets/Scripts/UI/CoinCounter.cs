using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private int allCoinAmount;
    [SerializeField] private int collectedCoins;
    private void Awake()
    {
        LevelManager.instance.OnPlayerValueChange += GetAllPlayerEvents;
    }
    void Start()
    {
        foreach (var coin in GameObject.FindGameObjectsWithTag("Coin"))
        {
            allCoinAmount++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        coinText.text = $"Collected Coins: {collectedCoins}/{allCoinAmount}";
    }

    private void GetAllPlayerEvents()
    {
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            player.GetComponentInParent<MainPlayerInfo>().coinAmount.OnValueChanged += AddCollectedCoin;
        }
    }

    private void AddCollectedCoin(int oldValue, int newValue)
    {
        collectedCoins++;
    }
}
