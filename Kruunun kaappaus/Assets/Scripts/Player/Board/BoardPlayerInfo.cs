using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Splines;

public class BoardPlayerInfo : NetworkBehaviour
{
    public MainPlayerInfo playerInfo;
    public bool movingForward;

    [HideInInspector] public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> playerSpriteIndex = new(writePerm: NetworkVariableWritePermission.Owner, value: -1);
    public NetworkVariable<int> playerColor = new(writePerm: NetworkVariableWritePermission.Owner, value: -1);

    [SerializeField] private SpriteRenderer spriteComponent;

    private void Start()
    {
        if (NetworkObject.IsOwner)
        {
            playerInfo = GetComponentInParent<MainPlayerInfo>();
            transform.position = BoardPath.instance.tiles[playerInfo.currentBoardPosition.Value].transform.position;

            GameManager.instance.LoadPlayerServerRpc();
        }
    }

    public override void OnNetworkSpawn()
    {
        playerSpriteIndex.OnValueChanged += UpdatePlayerSprite;
        playerColor.OnValueChanged += UpdatePlayerColor;
        base.OnNetworkSpawn();
    }

    private void OnTransformParentChanged()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        var playerData = GetComponentInParent<PlayerSetup>().SavedData;

        playerSpriteIndex.Value = int.Parse(playerData["PlayerIconIndex"].Value);
        playerColor.Value = int.Parse(playerData["PlayerColor"].Value);
    }

    [Rpc(SendTo.Owner)]
    public void UpdatePlayerPositionClientRpc(Vector2 newPos)
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }

        transform.position = newPos;
    }

    [Rpc(SendTo.Everyone)]
    public void FlipSpriteRpc(bool currentX)
    {
        spriteComponent.flipX = currentX;
    }

    private void UpdatePlayerSprite(int oldIndex, int newIndex)
    {
        spriteComponent.sprite = MainMenuUI.instance.PlayerIcons[newIndex];
        GetComponent<Animator>().runtimeAnimatorController = MainMenuUI.instance.PlayerAnimators[newIndex];
    }
    private void UpdatePlayerColor(int oldColor, int newColor)
    {
        if (newColor == (int)Colors.None)
        {
            spriteComponent.material.SetFloat("_Thickness", 0);
        }
        spriteComponent.material.color = MainMenuUI.GetColor(newColor);
    }
}
