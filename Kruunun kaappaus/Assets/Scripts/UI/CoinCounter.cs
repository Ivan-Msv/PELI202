using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class CoinCounter : NetworkBehaviour
{
    [SerializeField] private PlayerScoreInfo playerScoreInfoPrefab;
    [SerializeField] private GameObject playerScorePanel;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private NetworkVariable<int> allCoinAmount;
    [SerializeField] private NetworkVariable<int> collectedCoins;
    [SerializeField] private Dictionary<PlayerInfo2D, PlayerScoreInfo> playerScores = new();

    void Start()
    {
        if (!IsServer || LevelManager.instance.currentLevelType != LevelType.Minigame)
        {
            return;
        }

        for (int i = 0; i < GameObject.FindGameObjectsWithTag("Coin").Length; i++)
        {
            allCoinAmount.Value++;
        }

        LevelManager.instance.availableCoins.Value = allCoinAmount.Value;
    }

    void Update()
    {
        coinText.text = $"Collected Coins: {collectedCoins.Value}/{allCoinAmount.Value}";
    }

    private void OnDisable()
    {
        foreach (var score in playerScores)
        {
            score.Key.localCoinAmount.OnValueChanged -= UpdatePlayerData;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void GetAllPlayerDataRpc()
    {
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            var component = player.GetComponent<PlayerInfo2D>();

            component.localCoinAmount.OnValueChanged += UpdatePlayerData;

            if (component.playerIsGhost.Value)
            {
                continue;
            }

            var newScoreInfo = Instantiate(playerScoreInfoPrefab, playerScorePanel.transform);
            playerScores.Add(component, newScoreInfo);
            newScoreInfo.UpdatePlayerImage(MainMenuUI.instance.PlayerIcons[component.playerSpriteIndex.Value]);
        }
    }

    private void UpdatePlayerData(int oldValue, int newValue)
    {
        foreach (var score in playerScores)
        {
            score.Value.UpdatePlayerScore(score.Key.localCoinAmount.Value);
        }
    }

    [Rpc(SendTo.Server)]
    public void AddCollectedCoinServerRpc()
    {
        collectedCoins.Value++;
        LevelManager.instance.availableCoins.Value--;
    }
}
