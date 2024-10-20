using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyInfo : MonoBehaviour
{
    public Dictionary<string, PlayerDataObject> playerData;
    public bool IsHost { get; private set; }
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image iconImage;
    public TextMeshProUGUI hostText;
    void Start()
    {
        playerNameText.text = playerData["PlayerName"].Value;
        iconImage.sprite = MainMenuUI.instance.PlayerIcons[int.Parse(playerData["PlayerIconIndex"].Value)];
    }

    public void SetPlayerAsHost(bool apply)
    {
        switch (apply)
        {
            case true:
                IsHost = true;
                hostText.gameObject.SetActive(true);
                break;
            case false:
                IsHost = false;
                hostText.gameObject.SetActive(false);
                break;
        }
    }
}
