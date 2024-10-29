using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.U2D.Animation;

public enum Colors
{
    White = 0, Red = 1, Green = 2, Blue = 3, Magenta = 4
}

public class PlayerInfo2D : NetworkBehaviour
{
    public Dictionary<string, PlayerDataObject> playerData;
    [SerializeField] private TextMeshProUGUI playerNameVisual;

    [HideInInspector] public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> playerSpriteIndex = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<Colors> playerColor = new(writePerm: NetworkVariableWritePermission.Owner); 
    public NetworkVariable<bool> playerIsGhost = new(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> coinAmount = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> crownAmount = new(writePerm: NetworkVariableWritePermission.Owner);
    private SpriteLibrary spriteLibraryComponent;
    private SpriteRenderer spriteComponent;
    void Awake()
    {
        spriteLibraryComponent = GetComponent<SpriteLibrary>();
        spriteComponent = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        SwapSpriteAxis();
    }

    public override void OnNetworkSpawn()
    {
        playerName.OnValueChanged += UpdatePlayerName;
        playerSpriteIndex.OnValueChanged += UpdatePlayerSprite;
        playerColor.OnValueChanged += UpdatePlayerColor;
        playerIsGhost.OnValueChanged += UpdatePlayerBoolean;
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
        playerColor.Value = (Colors)int.Parse(playerData["PlayerColor"].Value);
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
        spriteComponent.sprite = MainMenuUI.instance.PlayerIcons[newIndex];
        spriteLibraryComponent.spriteLibraryAsset = MainMenuUI.instance.PlayerSprites[newIndex];
    }
    private void UpdatePlayerColor(Colors oldColor, Colors newColor)
    {
        spriteComponent.material.color = MainMenuUI.GetColor((int)newColor);
    }
    private void UpdatePlayerBoolean(bool oldValue, bool newValue)
    {
        GetComponent<PlayerMovement2D>().isGhost = newValue;
        var newColor = spriteComponent.color;
        newColor.a = newValue ? 0.4f : 1;
        spriteComponent.color = newColor;
    }
    private void SwapSpriteAxis()
    {
        float lastAxis = Input.GetAxisRaw("Horizontal");

        switch (lastAxis)
        {
            case 1:
                FlipServerRpc(false);
                break;
            case -1:
                FlipServerRpc(true);
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FlipServerRpc(bool currentX)
    {
        FlipClientRpc(currentX);
    }

    [ClientRpc]
    private void FlipClientRpc(bool currentX)
    {
        spriteComponent.flipX = currentX;
    }
}
