using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform nextPortal;
    private static Quaternion halfTurn = Quaternion.Euler(0, 0, 180);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var playerMovement = collision.GetComponent<PlayerMovement2D>();
        if (playerMovement.isGhost)
        {
            return;
        }

        if (!playerMovement.CanUsePortal)
        {
            return;
        }

        var relativeVelocity = halfTurn * playerMovement.LastVelocity;

        Debug.Log(relativeVelocity);

        collision.attachedRigidbody.linearVelocity = Vector2.zero;
        collision.transform.position = nextPortal.transform.position;
        collision.attachedRigidbody.linearVelocity = relativeVelocity;
        playerMovement.SetPortalCooldown();
    }
}
