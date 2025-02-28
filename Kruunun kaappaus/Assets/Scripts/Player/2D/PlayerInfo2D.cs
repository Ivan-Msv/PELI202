using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public NetworkVariable<int> localCoinAmount = new(writePerm: NetworkVariableWritePermission.Owner);

    private Animator animatorComponent;
    private ClientNetworkAnimator netAnimator;
    private SpriteRenderer spriteComponent;
    void Awake()
    {
        animatorComponent = GetComponent<Animator>();
        netAnimator = GetComponent<ClientNetworkAnimator>();
        spriteComponent = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        // Adding here to prevent multiple audiolisteners across 1 scene
        transform.AddComponent<AudioListener>();

        playerIsGhost.Value = GetComponentInParent<MainPlayerInfo>().isGhost.Value;
        LevelManager.instance.SetCamera(transform);
        LevelManager.instance.LoadPlayerServerRpc();
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
        if (!NetworkObject.IsOwner || transform.parent == null)
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
        animatorComponent.runtimeAnimatorController = newValue ? MainMenuUI.instance.GhostAnimator : MainMenuUI.instance.PlayerAnimators[playerSpriteIndex.Value];
        gameObject.layer = newValue ? LayerMask.NameToLayer("Ghost Player") : LayerMask.NameToLayer("Player");
        netAnimator.Animator = animatorComponent;
    }
    private void SwapSpriteAxis()
    {
        float lastAxis = Input.GetAxisRaw("Horizontal");

        switch (lastAxis)
        {
            case 1:
                FlipRpc(false);
                break;
            case -1:
                FlipRpc(true);
                break;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void FlipRpc(bool currentX)
    {
        spriteComponent.flipX = currentX;
    }
}
