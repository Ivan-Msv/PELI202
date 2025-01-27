using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform targetPortal;
    private void OnTriggerStay2D(Collider2D collision)
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

        // So that the gravity doesn't change during this
        playerMovement.IsUsingPortal = true;

        // Reset gravity so it doesn't affect the new velocity too early
        collision.attachedRigidbody.gravityScale = 0;

        // Get current velocity
        var lastVelocity = collision.attachedRigidbody.linearVelocity;

        // Calculate the rotation difference between this portal and the next one
        var angleDiff = Vector2.SignedAngle(transform.up, targetPortal.transform.up);

        // This was the hardest part to find info on, since the player gravity changes based on the velocity
        var newSpeed = lastVelocity.magnitude / Mathf.Sqrt(2);
        // Get new direction using the angle difference between two portals
        var newDirection = Quaternion.Euler(0, 0, angleDiff) * -lastVelocity.normalized;

        // Round the speed magnitude so it doesn't sway around too much and decrease/increase velocity
        var newVelocity = newDirection * Mathf.Round(newSpeed);

        //Vector2 offset = new(0, Mathf.Abs(collision.transform.position.y - transform.position.y));
        //var targetPosition = (Vector2)targetPortal.transform.position + offset;

        Debug.Log(playerMovement.currentPlayerState);

        // Teleport the player
        collision.transform.position = targetPortal.transform.position;
        LevelManager.instance.TeleportCamera();

        // Setting horizontal as external force due to playermovement script resetting it otherwise
        collision.attachedRigidbody.linearVelocityY = newVelocity.y;
        playerMovement.AddExternalForce(new(newVelocity.x, 0));
        playerMovement.SetPortalCooldown();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var playerMovement = collision.GetComponent<PlayerMovement2D>();
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
        collision.attachedRigidbody.gravityScale = playerMovement.DefaultGravity;
    }
}
