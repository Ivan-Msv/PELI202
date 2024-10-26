using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerInfo2D : NetworkBehaviour
{
    public Dictionary<string, PlayerDataObject> playerData;
    [SerializeField] private TextMeshProUGUI playerNameVisual;

    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> playerSpriteIndex = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        // jos index on 0 niin se huomaa että index vaihtuu (muuten 0 = 0)
        playerSpriteIndex.Value = -1;
        playerName.OnValueChanged += UpdatePlayerName;
        playerSpriteIndex.OnValueChanged += UpdatePlayerSprite;
        base.OnNetworkSpawn();
    }

    private void OnTransformParentChanged()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        playerData = GetComponentInParent<PlayerSetup>().SavedData;

        playerName.Value = playerData["PlayerName"].Value;
        playerSpriteIndex.Value = int.Parse(playerData["PlayerIconIndex"].Value);
    }

    private void UpdatePlayerName(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        // jotta ei tarvisi nähä oman nimen
        if (NetworkObject.IsOwner)
        {
            return;
        }

        playerNameVisual.text = newValue.ToString();
    }

    private void UpdatePlayerSprite(int oldIndex, int newIndex)
    {
        GetComponent<SpriteRenderer>().sprite = MainMenuUI.instance.PlayerIcons[newIndex];
    }
}
