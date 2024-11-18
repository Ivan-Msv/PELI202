using Unity.Netcode;
using UnityEngine;

public class BoardPlayerInfo : NetworkBehaviour
{
    public int currentPosition;
    public bool movingForward;

    private void Start()
    {
        if (NetworkObject.IsOwner)
        {
            transform.position = GameManager.instance.currentPath.tiles[currentPosition].transform.position;
            GameManager.instance.LoadPlayerServerRpc();
        }
    }
}
