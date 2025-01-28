using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Collider2D currentPortal;
    [SerializeField] private Collider2D targetPortal;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var playerMovement = collision.collider.GetComponent<PlayerMovement2D>();
        if (playerMovement.isGhost)
        {
            return;
        }

        if (!playerMovement.CanUsePortal)
        {
            return;
        }

        // So that the gravity doesn't change during this
        playerMovement.IsUsingPortal = true;

        // Reset gravity so it doesn't affect the new velocity too early
        collision.rigidbody.gravityScale = 0;

        var lastVelocity = collision.relativeVelocity;

        // This was the hardest part to find info on, since the player gravity changes based on the velocity
        var newSpeed = lastVelocity.magnitude / Mathf.Sqrt(2);

        // Round the speed magnitude so it doesn't sway around too much and decrease/increase velocity
        var newVelocity = targetPortal.transform.up * Mathf.Round(newSpeed);

        Vector3 relativePos = transform.InverseTransformPoint(new(transform.position.x, collision.transform.position.y));

        // Rotate the relative position to align with the target portal's rotation


        // Transform the rotated relative position into the target portal's world space
        collision.transform.position = targetPortal.transform.TransformPoint(relativePos);

        LevelManager.instance.TeleportCamera();

        // Setting horizontal as external force due to playermovement script resetting it otherwise
        collision.rigidbody.linearVelocityY = newVelocity.y;
        playerMovement.AddExternalForce(new(newVelocity.x, 0));
        playerMovement.SetPortalCooldown();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        var playerMovement = collision.collider.GetComponent<PlayerMovement2D>();
        if (playerMovement.isGhost)
        {
            return;
        }

        if (!playerMovement.IsUsingPortal)
        {
            return;
        }

        // Reset everything, and put the portal on cooldown (otherwise it would spam teleport)

        playerMovement.IsUsingPortal = false;
        collision.rigidbody.gravityScale = playerMovement.DefaultGravity;
    }
}
