using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerSprite2D : NetworkBehaviour
{
    private SpriteRenderer spriteComponent;
    void Start()
    {
        spriteComponent = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (NetworkObject.IsOwner)
        {
            SwapSpriteAxis();
        }
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
