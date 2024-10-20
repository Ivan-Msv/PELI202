using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerInfo2D : NetworkBehaviour
{
    public Dictionary<string, PlayerDataObject> playerData;
    [SerializeField] private TextMeshProUGUI playerNameVisual;

    private void Start()
    {
        playerData = FindObjectsOfType<PlayerLobbyInfo>().FirstOrDefault(player => player.name == transform.name).playerData;

        playerNameVisual.text = playerData["PlayerName"].Value;
        GetComponent<SpriteRenderer>().sprite = MainMenuUI.instance.PlayerIcons[int.Parse(playerData["PlayerIconIndex"].Value)];
    }
}
