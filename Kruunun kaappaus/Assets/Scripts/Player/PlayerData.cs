using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    // Pelaajan data
    [field: SerializeField] public NetworkVariable<int> MoneyAmount { get; private set; }
    [field: SerializeField] public NetworkVariable<int> CrownAmount { get; private set; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsOwner)
        {
            ChangeMoneyServerRpc(1);
        }
    }
    // Kertoo hostille / serverille että haluu jotain ja EI tarvi ownershippiä
    [ServerRpc(RequireOwnership = false)]
    public void ChangeMoneyServerRpc(int amount)
    {
        AddMoneyClientRpc(amount);
    }
    // Vaihtaa jotain kaikille pelaajille
    [ClientRpc]
    private void AddMoneyClientRpc(int amount)
    {
        MoneyAmount.Value += amount;
    }
}
