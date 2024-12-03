using Unity.Netcode;
using UnityEngine;

public class BoardPlayerInfo : NetworkBehaviour
{
    public MainPlayerInfo playerInfo;
    public bool movingForward;

    private void Start()
    {
        if (NetworkObject.IsOwner)
        {
            playerInfo = GetComponentInParent<MainPlayerInfo>();
            transform.position = BoardPath.instance.tiles[playerInfo.currentBoardPosition.Value].transform.position;
            GameManager.instance.LoadPlayerServerRpc();
        }
    }
}
