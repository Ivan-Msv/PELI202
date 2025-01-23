using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform nextPortal;

    private float lastGravity;

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

        lastGravity = collision.attachedRigidbody.gravityScale;
        collision.attachedRigidbody.gravityScale = 0;

        var lastVelocity = collision.attachedRigidbody.linearVelocity;

        var localPosition = transform.InverseTransformPoint(collision.transform.position);

        collision.transform.position = nextPortal.transform.TransformPoint(localPosition);

        float angleDiff = Vector2.SignedAngle(transform.up, nextPortal.transform.up);
        var newVelocity = Quaternion.Euler(0, 0, angleDiff) * -lastVelocity;

        collision.attachedRigidbody.linearVelocity = newVelocity;
        Debug.Log(collision.attachedRigidbody.linearVelocity);
    }

    private void OnTriggerExit2D(Collider2D collision)
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

        collision.attachedRigidbody.gravityScale = lastGravity;
        playerMovement.SetPortalCooldown();
    }
}
