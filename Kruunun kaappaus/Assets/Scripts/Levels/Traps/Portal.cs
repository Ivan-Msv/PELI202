using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform nextPortal;
    private List<Collider2D> playerObjects = new();

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

        playerObjects.Add(collision);
    }


    private void FixedUpdate()
    {
        if (playerObjects.Count > 0)
        {
            for (int i = 0; i < playerObjects.Count; i++)
            {
                var collision = playerObjects[i];
                var playerMovement = collision.GetComponent<PlayerMovement2D>();

                var lastVelocity = collision.attachedRigidbody.linearVelocity;

                Vector2 offset = collision.transform.position - transform.position;

                collision.attachedRigidbody.MovePosition((Vector2)nextPortal.transform.position + offset);

                float angleDiff = Vector2.SignedAngle(transform.up, nextPortal.transform.up);
                var newVelocity = Quaternion.Euler(0, 0, angleDiff) * -lastVelocity;

                Debug.Log(newVelocity);

                collision.attachedRigidbody.linearVelocity = newVelocity;
                playerMovement.SetPortalCooldown();

                playerObjects.Remove(playerObjects[i]);
            }
        }
    }
}
