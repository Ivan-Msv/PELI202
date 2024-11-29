using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class MainPlayerInfo : NetworkBehaviour
{
    public PlayerSetup playerSetup;

    public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> coinAmount = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> crownAmount = new(writePerm: NetworkVariableWritePermission.Owner);

    public BoardDice test;
    private void Start()
    {
        if (!NetworkObject.IsOwner)
        {
            return;
        }
        playerSetup = GetComponent<PlayerSetup>();
        StartCoroutine(LateInit());
    }

    private IEnumerator LateInit()
    {
        yield return new WaitForEndOfFrame();
        playerName.Value = playerSetup.SavedData["PlayerName"].Value;
    }
}
