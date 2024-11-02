using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;

public enum Colors
{
    None = 1, White = 2, Red = 3, Green = 4, Magenta = 5
}

public class PlayerInfo2D : NetworkBehaviour
{
    public Dictionary<string, PlayerDataObject> playerData;
    [SerializeField] private TextMeshProUGUI playerNameVisual;

    [HideInInspector] public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> playerSpriteIndex = new(writePerm: NetworkVariableWritePermission.Owner, value: -1);
    public NetworkVariable<int> playerColor = new(writePerm: NetworkVariableWritePermission.Owner, value: -1); 
    public NetworkVariable<bool> playerIsGhost = new(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> coinAmount = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> crownAmount = new(writePerm: NetworkVariableWritePermission.Owner);
    private Animator animatorComponent;
    private SpriteRenderer spriteComponent;
    void Awake()
    {
        animatorComponent = GetComponent<Animator>();
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

        transform.parent.TryGetComponent(out PlayerSetup playerInformation);

        if (playerInformation != null)
        {
            playerData = playerInformation.SavedData;

            playerName.Value = playerData["PlayerName"].Value;
            playerSpriteIndex.Value = int.Parse(playerData["PlayerIconIndex"].Value);
            playerColor.Value = int.Parse(playerData["PlayerColor"].Value);
        }
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
        animatorComponent.runtimeAnimatorController = MainMenuUI.instance.PlayerAnimators[newIndex];
    }
    private void UpdatePlayerColor(int oldColor, int newColor)
    {
        if (newColor == (int)Colors.None)
        {
            spriteComponent.material.SetFloat("_Thickness", 0);
        }
        spriteComponent.material.color = MainMenuUI.GetColor(newColor);
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
