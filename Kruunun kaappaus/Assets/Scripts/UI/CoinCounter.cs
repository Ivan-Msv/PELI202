using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private int allCoinAmount;
    [SerializeField] private int collectedCoins;
    private List<MainPlayerInfo> players = new();
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
        LevelManager.instance.availableCoins = allCoinAmount;
    }

    private void OnDisable()
    {
        foreach (var player in players)
        {
            player.coinAmount.OnValueChanged -= AddCollectedCoin;
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
            var component = player.GetComponentInParent<MainPlayerInfo>();
            component.coinAmount.OnValueChanged += AddCollectedCoin;
            players.Add(component);
        }
    }

    private void AddCollectedCoin(int oldValue, int newValue)
    {
        Debug.Log("Collected coin.");
        collectedCoins++;
        LevelManager.instance.availableCoins--;
    }
}
